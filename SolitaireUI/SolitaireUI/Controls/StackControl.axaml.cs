using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Cards;
using GenericSol;
using SolitaireUI.ViewModels;
using System;
using System.Diagnostics;

namespace SolitaireUI.Controls;

public enum StackOrientation
{
    Up,
    Down,
    Left,
    Right
}

public class StackControl : Control
{
    public static readonly StyledProperty<Stack?> StackProperty =
        AvaloniaProperty.Register<StackControl, Stack?>(nameof(Stack));

    public static readonly StyledProperty<bool> FaceUpProperty =
        AvaloniaProperty.Register<StackControl, bool>(nameof(FaceUp), defaultValue: true);

    public static readonly StyledProperty<StackOrientation> OrientationProperty =
        AvaloniaProperty.Register<StackControl, StackOrientation>(nameof(Orientation), defaultValue: StackOrientation.Down);

    /// <summary>
    /// When true (the default), a left click on the stack starts a drag. When false, a left
    /// click instead invokes <c>OnLeftClick</c> on the game. Applies to all stacks, mixed or not.
    /// </summary>
    public static readonly StyledProperty<bool> DraggableProperty =
        AvaloniaProperty.Register<StackControl, bool>(nameof(Draggable), defaultValue: true);

    public static readonly StyledProperty<double> CardWidthProperty =
        AvaloniaProperty.Register<StackControl, double>(nameof(CardWidth), defaultValue: 71.0);

    public static readonly StyledProperty<double> CardHeightProperty =
        AvaloniaProperty.Register<StackControl, double>(nameof(CardHeight), defaultValue: 96.0);

    public static readonly StyledProperty<double> OverlapDistanceProperty =
        AvaloniaProperty.Register<StackControl, double>(nameof(OverlapDistance), defaultValue: 20.0);

    public static readonly StyledProperty<double> FaceDownPeekHeightProperty =
        AvaloniaProperty.Register<StackControl, double>(nameof(FaceDownPeekHeight), defaultValue: 5.0);

    /// <summary>
    /// When true (the default), an X is drawn on the card area when the stack is empty. When
    /// false, nothing is drawn for an empty stack.
    /// </summary>
    public static readonly StyledProperty<bool> EmptyXProperty =
        AvaloniaProperty.Register<StackControl, bool>(nameof(EmptyX), defaultValue: true);

    private Stack? _previousStack;

    private static double s_maxMixedStackOverlapDistance = 0.0;

    private bool IsHoveredDuringDrag
    {
        get
        {
            if (DataContext is IDragDropViewModel viewModel && Stack != null)
            {
                return viewModel.CurrentHoverStack == Stack;
            }
            return false;
        }
    }

    static StackControl()
    {
        AffectsRender<StackControl>(StackProperty, FaceUpProperty, CardWidthProperty,
            CardHeightProperty, OverlapDistanceProperty, FaceDownPeekHeightProperty, OrientationProperty, EmptyXProperty);
        AffectsMeasure<StackControl>(StackProperty, CardWidthProperty, CardHeightProperty,
            OverlapDistanceProperty, FaceDownPeekHeightProperty, OrientationProperty);

        StackProperty.Changed.AddClassHandler<StackControl>((control, args) =>
            control.OnStackChanged(args));
    }

    public StackControl()
    {
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (Stack != null && DataContext is IDragDropViewModel viewModel)
        {
            viewModel.SetMouseHoverStack(Stack);
            viewModel.SetMouseHoverCard(GetFaceUpCardAt(e.GetPosition(this)));
        }
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (DataContext is IDragDropViewModel viewModel)
        {
            if (viewModel.MouseHoverStack == Stack)
            {
                viewModel.SetMouseHoverStack(null);
            }
            viewModel.SetMouseHoverCard(null);
        }
    }

    /// <summary>
    /// Returns the face-up card (if any) located at the given position within this control,
    /// whether the stack is a plain face-up <see cref="Stack"/> (only the top card counts) or a
    /// <see cref="MixedStack"/> (any of its face-up cards, using the same overlap geometry as
    /// rendering/hit-testing for drags).
    /// </summary>
    private Card? GetFaceUpCardAt(Point position)
    {
        if (Stack == null || Stack.Count == 0)
        {
            return null;
        }

        if (Stack is MixedStack mixedStack)
        {
            var vertical = IsVerticalOrientation;
            var crossSize = vertical ? CardWidth : CardHeight;
            var crossPos = vertical ? position.X : position.Y;

            if (crossPos < 0 || crossPos > crossSize)
            {
                return null;
            }

            var axisPos = vertical ? position.Y : position.X;
            var axisCardSize = vertical ? CardHeight : CardWidth;
            var totalAxisSize = vertical ? Bounds.Height : Bounds.Width;

            var faceDownCount = mixedStack.Count - mixedStack.CardsUp;
            var currentDistance = faceDownCount * FaceDownPeekHeight;

            if (axisPos >= currentDistance && mixedStack.CardsUp > 0)
            {
                var overlapDistance = CalculateOverlapDistance(mixedStack.CardsUp, totalAxisSize, axisCardSize);
                var firstFaceUpIndex = mixedStack.Count - mixedStack.CardsUp;

                for (int i = 0; i < mixedStack.CardsUp; i++)
                {
                    var sliceStart = currentDistance;
                    var sliceEnd = currentDistance + (i == mixedStack.CardsUp - 1 ? axisCardSize : overlapDistance);

                    if (axisPos >= sliceStart && axisPos < sliceEnd)
                    {
                        return mixedStack[firstFaceUpIndex + i];
                    }

                    currentDistance += overlapDistance;
                }
            }

            return null;
        }

        if (position.X < 0 || position.X > CardWidth)
        {
            return null;
        }

        if (FaceUp && position.Y >= 0 && position.Y < CardHeight)
        {
            return Stack[^1];
        }

        return null;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);

        if (point.Properties.IsRightButtonPressed && Stack != null)
        {
            if (DataContext is IDragDropViewModel viewModel)
            {
                viewModel.HandleStackRightClick(Stack);
            }
            e.Handled = true;
            return;
        }

        if (point.Properties.IsLeftButtonPressed && Stack != null)
        {
            if (DataContext is IDragDropViewModel viewModel)
            {
                // Safety net: if a previous drag was somehow left stuck in progress (e.g. an
                // exception interrupted it), clear it out now so this new press isn't silently
                // ignored and mouse dragging doesn't stay broken for the rest of the session.
                if (viewModel.IsDragging)
                {
                    viewModel.CancelDrag();
                    InvalidateAllStackControls();
                }

                // Check if Draggable
                if (!Draggable)
                {
                    if (viewModel.Game is GenericGame game)
                    {
                        game.OnLeftClick(Stack);
                    }
                    e.Handled = true;
                    return;
                }

                // Draggable is true - start drag operation
                var vertical = IsVerticalOrientation;
                var clickAxisPos = vertical ? point.Position.Y : point.Position.X;
                int cardCount = 1;
                int clickedCardIndex = -1;
                var clickedCardAxisPos = 0.0;

                if (Stack is MixedStack mixedStack)
                {
                    // Calculate which card was clicked in a mixed stack
                    var axisCardSize = vertical ? CardHeight : CardWidth;
                    var totalAxisSize = vertical ? Bounds.Height : Bounds.Width;
                    var faceDownCount = mixedStack.Count - mixedStack.CardsUp;
                    var currentDistance = 0.0;

                    // Skip past face-down cards
                    currentDistance += faceDownCount * FaceDownPeekHeight;

                    // Check face-up cards
                    if (clickAxisPos >= currentDistance && mixedStack.CardsUp > 0)
                    {
                        var overlapDistance = CalculateOverlapDistance(mixedStack.CardsUp, totalAxisSize, axisCardSize);
                        var firstFaceUpIndex = mixedStack.Count - mixedStack.CardsUp;

                        for (int i = 0; i < mixedStack.CardsUp; i++)
                        {
                            var sliceStart = currentDistance;
                            var sliceEnd = currentDistance + (i == mixedStack.CardsUp - 1 ? axisCardSize : overlapDistance);

                            if (clickAxisPos >= sliceStart && clickAxisPos < sliceEnd)
                            {
                                clickedCardIndex = firstFaceUpIndex + i;
                                cardCount = mixedStack.Count - clickedCardIndex;
                                clickedCardAxisPos = sliceStart;
                                break;
                            }

                            currentDistance += overlapDistance;
                        }
                    }
                }
                else
                {
                    // Non-mixed stack - just take the top card
                    if (Stack.Count > 0)
                    {
                        clickedCardIndex = Stack.Count - 1;
                        cardCount = 1;
                        clickedCardAxisPos = 0.0;
                    }
                }

                if (clickedCardIndex >= 0)
                {
                    var topLevel = TopLevel.GetTopLevel(this);
                    var topLevelPoint = topLevel != null ? e.GetPosition(topLevel) : point.Position;
                    var clickOffsetX = vertical ? point.Position.X : point.Position.X - clickedCardAxisPos;
                    var clickOffsetY = vertical ? point.Position.Y - clickedCardAxisPos : point.Position.Y;

                    try
                    {
                        var started = viewModel.StartDrag(Stack, cardCount, topLevelPoint.X, topLevelPoint.Y, clickOffsetX, clickOffsetY);
                        if (started)
                        {
                            e.Pointer.Capture(this);
                            // Immediately invalidate all controls to update visuals after split
                            InvalidateAllStackControls();
                            e.Handled = true;
                        }
                    }
                    catch
                    {
                        // If starting the drag failed partway through, make sure we never leave
                        // the pointer captured or the view model stuck in a "dragging" state -
                        // otherwise every subsequent mouse press/drag on the board would be
                        // swallowed by this control.
                        e.Pointer.Capture(null);
                        viewModel.CancelDrag();
                        InvalidateAllStackControls();
                        throw;
                    }
                }
            }
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (DataContext is not IDragDropViewModel viewModel)
        {
            return;
        }

        if (viewModel.IsDragging)
        {
            // Get the position relative to the window/top-level
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                var screenPoint = e.GetPosition(topLevel);

                // Hit test to find which stack control is under the pointer
                var hitStack = FindStackUnderPoint(topLevel, screenPoint);

                viewModel.UpdateDragHover(hitStack, screenPoint.X, screenPoint.Y);

                // Invalidate all stack controls to update visuals
                InvalidateAllStackControls();
            }
        }
        else
        {
            // Not dragging: track which face-up card (if any) the mouse is over so the status
            // bar can display it.
            viewModel.SetMouseHoverCard(GetFaceUpCardAt(e.GetPosition(this)));
        }
    }

    private Stack? FindStackUnderPoint(Visual topLevel, Point point)
    {
        // Use InputHitTest to find the visual at the point
        if (topLevel is IInputElement inputElement)
        {
            var hitResult = inputElement.InputHitTest(point);

            // Walk up the visual tree to find a StackControl
            var visual = hitResult as Visual;
            while (visual != null)
            {
                if (visual is StackControl stackControl && stackControl.Stack != null)
                {
                    return stackControl.Stack;
                }
                visual = visual.GetVisualParent();
            }
        }

        return null;
    }

    private void InvalidateAllStackControls()
    {
        if (DataContext is IDragDropViewModel)
        {
            // Find the top level and invalidate all StackControls
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                InvalidateStackControlsInVisual(topLevel);
            }
        }
    }

    private void InvalidateStackControlsInVisual(Visual visual)
    {
        if (visual is StackControl stackControl)
        {
            stackControl.InvalidateVisual();
        }

        // Use GetVisualChildren extension method
        var children = visual.GetVisualChildren();
        foreach (var child in children)
        {
            InvalidateStackControlsInVisual(child);
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (DataContext is IDragDropViewModel viewModel && viewModel.IsDragging)
        {
            e.Pointer.Capture(null); // Release pointer capture
            viewModel.CompleteDrag();

            // Invalidate all stack controls to update visuals
            InvalidateAllStackControls();
            e.Handled = true;
        }
    }

    public Stack? Stack
    {
        get => GetValue(StackProperty);
        set => SetValue(StackProperty, value);
    }

    public bool FaceUp
    {
        get => GetValue(FaceUpProperty);
        set => SetValue(FaceUpProperty, value);
    }

    /// <summary>
    /// When true (the default), an X is drawn on the card area when the stack is empty. When
    /// false, nothing is drawn for an empty stack.
    /// </summary>
    public bool EmptyX
    {
        get => GetValue(EmptyXProperty);
        set => SetValue(EmptyXProperty, value);
    }

    /// <summary>
    /// When true (the default), a left click on the stack starts a drag. When false, a left
    /// click instead invokes <c>OnLeftClick</c> on the game. Applies to all stacks, mixed or not.
    /// </summary>
    public bool Draggable
    {
        get => GetValue(DraggableProperty);
        set => SetValue(DraggableProperty, value);
    }

    public double CardWidth
    {
        get => GetValue(CardWidthProperty);
        set => SetValue(CardWidthProperty, value);
    }

    public double CardHeight
    {
        get => GetValue(CardHeightProperty);
        set => SetValue(CardHeightProperty, value);
    }

    public double OverlapDistance
    {
        get => GetValue(OverlapDistanceProperty);
        set => SetValue(OverlapDistanceProperty, value);
    }

    public double FaceDownPeekHeight
    {
        get => GetValue(FaceDownPeekHeightProperty);
        set => SetValue(FaceDownPeekHeightProperty, value);
    }

    public StackOrientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    private void OnStackChanged(AvaloniaPropertyChangedEventArgs args)
    {
        // Unsubscribe from previous stack
        if (_previousStack != null)
        {
            _previousStack.StackModified -= OnStackModified;
        }

        // Subscribe to new stack
        _previousStack = args.NewValue as Stack;
        if (_previousStack != null)
        {
            _previousStack.StackModified += OnStackModified;
        }
    }

    private void OnStackModified(object? sender, EventArgs e)
    {
        // Force complete visual update
        InvalidateVisual();
        InvalidateMeasure();
        InvalidateArrange();

        // Also invalidate parent's layout to ensure it re-arranges
        var parent = this.GetVisualParent();
        if (parent is Layoutable layoutableParent)
        {
            layoutableParent.InvalidateVisual();
            layoutableParent.InvalidateMeasure();
            layoutableParent.InvalidateArrange();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Stack == null || Stack.Count == 0)
        {
            return new Size(CardWidth, CardHeight);
        }

        if (Stack is MixedStack mixedStack)
        {
            var vertical = IsVerticalOrientation;
            var axisCardSize = vertical ? CardHeight : CardWidth;
            var crossCardSize = vertical ? CardWidth : CardHeight;
            var availableAxisSize = vertical ? availableSize.Height : availableSize.Width;

            var faceDownCount = mixedStack.Count - mixedStack.CardsUp;
            var faceUpCount = mixedStack.CardsUp;

            double faceDownAxisSize;
            if (faceUpCount > 0)
            {
                // All face-down cards render as small peeks; the face-up cards below take over
                // showing full-size cards.
                faceDownAxisSize = faceDownCount > 0 ? faceDownCount * FaceDownPeekHeight : 0;
            }
            else
            {
                // No face-up cards (e.g. mid-drag): all but the topmost face-down card render as
                // peeks, and the topmost renders full-size so the stack still shows a card.
                faceDownAxisSize = faceDownCount > 0
                    ? (faceDownCount - 1) * FaceDownPeekHeight + axisCardSize
                    : 0;
            }

            var overlapDistance = CalculateOverlapDistance(faceUpCount, availableAxisSize, axisCardSize);
            var faceUpAxisSize = faceUpCount > 0
                ? axisCardSize + (faceUpCount - 1) * overlapDistance
                : 0;

            var totalAxisSize = faceDownAxisSize + faceUpAxisSize;
            return vertical ? new Size(crossCardSize, totalAxisSize) : new Size(totalAxisSize, crossCardSize);
        }

        // Fixed (non-mixed) stack: Orientation has no effect here; always render a single card.
        return new Size(CardWidth, CardHeight);
    }

    /// <summary>
    /// True when <see cref="Orientation"/> grows the stack vertically (Down/Up) rather than
    /// horizontally (Left/Right). Only meaningful for <see cref="MixedStack"/>.
    /// </summary>
    private bool IsVerticalOrientation => Orientation is StackOrientation.Down or StackOrientation.Up;

    /// <summary>
    /// True when <see cref="Orientation"/> anchors the stack at the far edge (Up/Left), i.e. the
    /// stack grows toward the near (0,0) edge instead of away from it (Down/Right).
    /// </summary>
    private bool IsReversedOrientation => Orientation is StackOrientation.Up or StackOrientation.Left;

    /// <summary>
    /// Computes the rect for a card/peek slice positioned <paramref name="distanceAlongAxis"/> from
    /// the stack's anchor edge, sized <paramref name="sizeAlongAxis"/> along the growth axis, given
    /// the control's total size along that axis.
    /// </summary>
    private Rect GetSliceRect(double distanceAlongAxis, double sizeAlongAxis, double totalAxisSize)
    {
        var vertical = IsVerticalOrientation;
        var crossSize = vertical ? CardWidth : CardHeight;
        var axisPos = IsReversedOrientation
            ? totalAxisSize - distanceAlongAxis - sizeAlongAxis
            : distanceAlongAxis;

        return vertical
            ? new Rect(0, axisPos, crossSize, sizeAlongAxis)
            : new Rect(axisPos, 0, sizeAlongAxis, crossSize);
    }

    private double CalculateOverlapDistance(int cardCount, double availableSizeAlongAxis, double cardSizeAlongAxis)
    {
        if (cardCount <= 1 || double.IsInfinity(availableSizeAlongAxis))
        {
            return OverlapDistance;
        }

        var totalSizeNeeded = cardSizeAlongAxis + (cardCount - 1) * OverlapDistance;
        if (totalSizeNeeded <= availableSizeAlongAxis)
        {
            return OverlapDistance;
        }

        // Reduce overlap distance to fit within available size
        var maxOverlap = (availableSizeAlongAxis - cardSizeAlongAxis) / (cardCount - 1);
        return Math.Max(5.0, maxOverlap); // Minimum 5 pixels overlap
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (Stack == null || Stack.Count == 0)
        {
            DrawEmptyStackIndicator(context);

            // Add hover tint for empty stacks too
            if (IsHoveredDuringDrag)
            {
                DrawHoverTint(context);
            }
            return;
        }

        if (Stack is MixedStack mixedStack)
        {
            RenderMixedStack(context, mixedStack);
        }
        else
        {
            RenderNormalStack(context);
        }

        // Draw hover tint if this stack is being hovered during a drag
        if (IsHoveredDuringDrag)
        {
            DrawHoverTint(context);
        }
    }

    private void DrawHoverTint(DrawingContext context)
    {
        var tintBrush = new SolidColorBrush(Color.FromArgb(80, 0, 120, 255)); // Semi-transparent blue
        var rect = new Rect(0, 0, Bounds.Width, DesiredSize.Height);
        context.DrawRectangle(tintBrush, null, rect);
    }

    private void DrawEmptyStackIndicator(DrawingContext context)
    {
        if (!EmptyX)
        {
            return;
        }

        var rect = new Rect(0, 0, CardWidth, CardHeight);

        // Draw a transparent rectangle over the full card area so that the
        // entire card size participates in hit testing, not just the X strokes.
        context.DrawRectangle(Brushes.Transparent, null, rect);

        var brush = new SolidColorBrush(Color.FromArgb(255, 0, 200, 0));
        var pen = new Pen(brush, 12.0);

        // Draw red X
        context.DrawLine(pen, rect.TopLeft, rect.BottomRight);
        context.DrawLine(pen, rect.TopRight, rect.BottomLeft);
    }

    private void RenderNormalStack(DrawingContext context)
    {
        if (FaceUp && Stack!.Count > 0)
        {
            var bottomCard = Stack[^1];
            var bitmap = MainWindowViewModel.ImageFromCard(bottomCard);
            var rect = new Rect(0, 0, CardWidth, CardHeight);
            context.DrawImage(bitmap, rect);
            DrawTintIfHighlighted(context, Stack!, Stack!.Count - 1, rect);
        }
        else
        {
            // Draw face-down card (card back image)
            var rect = new Rect(0, 0, CardWidth, CardHeight);
            context.DrawImage(MainWindowViewModel.GetCardBackImage(), rect);
        }
    }

    private void RenderMixedStack(DrawingContext context, MixedStack mixedStack)
    {
        var vertical = IsVerticalOrientation;
        var axisCardSize = vertical ? CardHeight : CardWidth;
        var totalAxisSize = vertical ? Bounds.Height : Bounds.Width;
        var cardBackImage = MainWindowViewModel.GetCardBackImage();
        var faceDownCount = mixedStack.Count - mixedStack.CardsUp;
        var currentDistance = 0.0;

        if (vertical)
        {
            // Draw each peek slightly taller than the spacing between peeks (~10% extra) so it
            // overdraws onto the peek below it, covering that card's transparent rounded corners
            // instead of letting the playing surface show through. Round to a whole pixel so the
            // extra slice height doesn't introduce a fractional-pixel scale (which would
            // blur/anti-alias it). This crop-based peek rendering only applies to the vertical
            // (Down/Up) orientations, since the source-rect crops are height-based.
            var peekRectHeight = Math.Max(FaceDownPeekHeight + 1, Math.Round(FaceDownPeekHeight * 1.1));
            if (peekRectHeight > s_maxMixedStackOverlapDistance)
            {
                s_maxMixedStackOverlapDistance = peekRectHeight;
            }

            // Draw the peek slices directly from the card back artwork straight to the real
            // DrawingContext (which already accounts for the screen's actual DPI/render scaling)
            // instead of pre-rendering to an intermediate off-screen bitmap - that indirection
            // previously risked introducing its own width/height scaling mismatches.
            var faceDownOverlapSourceRect = MainWindowViewModel.GetFaceDownOverlapSourceRect(
                s_maxMixedStackOverlapDistance, CardWidth, CardHeight);
            var faceDownBackingSourceRect = MainWindowViewModel.GetFaceDownBackingSourceRect(
                s_maxMixedStackOverlapDistance, CardWidth, CardHeight);

            if (mixedStack.CardsUp > 0)
            {
                // Draw face-down cards as small peeks; face-up cards below will be full-size.
                if (faceDownCount > 0)
                {
                    for (int i = 0; i < faceDownCount; i++)
                    {
                        var rect = GetSliceRect(currentDistance, peekRectHeight, totalAxisSize);
                        // Every slice except the topmost one needs an opaque backing behind it so
                        // its transparent rounded corners reveal matching card-back artwork
                        // instead of the playing surface beneath. The topmost slice (i == 0) has
                        // nothing above it, so it should legitimately show the playing surface
                        // through its corners.
                        if (i > 0)
                        {
                            context.DrawImage(cardBackImage, faceDownBackingSourceRect, rect);
                        }
                        context.DrawImage(cardBackImage, faceDownOverlapSourceRect, rect);
                        currentDistance += FaceDownPeekHeight;
                    }
                }
            }
            else if (faceDownCount > 0)
            {
                // No face-up cards (e.g. mid-drag): draw all but the last face-down card as
                // peeks, then draw the topmost face-down card full-size so the stack still shows
                // a card.
                for (int i = 0; i < faceDownCount - 1; i++)
                {
                    var rect = GetSliceRect(currentDistance, peekRectHeight, totalAxisSize);
                    if (i > 0)
                    {
                        context.DrawImage(cardBackImage, faceDownBackingSourceRect, rect);
                    }
                    context.DrawImage(cardBackImage, faceDownOverlapSourceRect, rect);
                    currentDistance += FaceDownPeekHeight;
                }

                var topRect = GetSliceRect(currentDistance, axisCardSize, totalAxisSize);
                context.DrawImage(cardBackImage, topRect);
            }
        }
        else
        {
            // Horizontal (Left/Right) orientation: no width-based crop artwork is available, so
            // draw full card-back images offset along the growth axis instead. Later slices are
            // drawn after (and therefore visually on top of) earlier ones, which still produces a
            // correct thin-peek overlap illusion.
            if (mixedStack.CardsUp > 0)
            {
                if (faceDownCount > 0)
                {
                    for (int i = 0; i < faceDownCount; i++)
                    {
                        var rect = GetSliceRect(currentDistance, axisCardSize, totalAxisSize);
                        context.DrawImage(cardBackImage, rect);
                        currentDistance += FaceDownPeekHeight;
                    }
                }
            }
            else if (faceDownCount > 0)
            {
                for (int i = 0; i < faceDownCount; i++)
                {
                    var rect = GetSliceRect(currentDistance, axisCardSize, totalAxisSize);
                    context.DrawImage(cardBackImage, rect);
                    currentDistance += FaceDownPeekHeight;
                }
            }
        }

        // Draw face-up cards overlapping
        if (mixedStack.CardsUp > 0)
        {
            var overlapDistance = CalculateOverlapDistance(mixedStack.CardsUp, totalAxisSize, axisCardSize);
            var firstFaceUpIndex = mixedStack.Count - mixedStack.CardsUp;

            for (int i = 0; i < mixedStack.CardsUp; i++)
            {
                var index = firstFaceUpIndex + i;
                var card = mixedStack[index];
                var bitmap = MainWindowViewModel.ImageFromCard(card);
                var rect = GetSliceRect(currentDistance, axisCardSize, totalAxisSize);
                context.DrawImage(bitmap, rect);
                DrawTintIfHighlighted(context, mixedStack, index, rect);
                currentDistance += overlapDistance;
            }
        }
    }

    /// <summary>
    /// If <see cref="Stack.IsHilit"/> is set on <paramref name="stack"/> and <paramref name="cardIndex"/>
    /// falls within its tinted range (<see cref="Stack.StartTintIndex"/>/<see cref="Stack.TintCount"/>),
    /// paints <paramref name="rect"/> with <see cref="Stack.TintColor"/>. Highlighting only ever
    /// applies to face-up cards, which the caller is expected to guarantee.
    /// </summary>
    private static void DrawTintIfHighlighted(DrawingContext context, Stack stack, int cardIndex, Rect rect)
    {
        if (!stack.IsHilit)
        {
            return;
        }

        if (cardIndex < stack.StartTintIndex || cardIndex >= stack.StartTintIndex + stack.TintCount)
        {
            return;
        }

        Debug.Assert(stack is not MixedStack mixedStack || cardIndex >= mixedStack.Count - mixedStack.CardsUp,
            "Only face-up cards may be tinted.");

        // Overlay the tint translucently on top of the already-drawn card image rather than
        // replacing it, so the card artwork remains visible underneath the color wash.
        var tintColor = stack.TintColor;
        const byte overlayAlpha = 100;
        var brush = new SolidColorBrush(Color.FromArgb(overlayAlpha, tintColor.R, tintColor.G, tintColor.B));
        context.DrawRectangle(brush, null, rect);
    }
}