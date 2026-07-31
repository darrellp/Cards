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

namespace SolitaireUI.Controls;

public class StackControl : Control
{
    public static readonly StyledProperty<Stack?> StackProperty =
        AvaloniaProperty.Register<StackControl, Stack?>(nameof(Stack));

    public static readonly StyledProperty<bool> FaceUpProperty =
        AvaloniaProperty.Register<StackControl, bool>(nameof(FaceUp), defaultValue: true);

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
            CardHeightProperty, OverlapDistanceProperty, FaceDownPeekHeightProperty);
        AffectsMeasure<StackControl>(StackProperty, CardWidthProperty, CardHeightProperty,
            OverlapDistanceProperty, FaceDownPeekHeightProperty);

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

        if (position.X < 0 || position.X > CardWidth)
        {
            return null;
        }

        if (Stack is MixedStack mixedStack)
        {
            var faceDownCount = mixedStack.Count - mixedStack.CardsUp;
            var currentY = faceDownCount * FaceDownPeekHeight;

            if (position.Y >= currentY && mixedStack.CardsUp > 0)
            {
                var overlapDistance = CalculateOverlapDistance(mixedStack.CardsUp, Bounds.Height);
                var firstFaceUpIndex = mixedStack.Count - mixedStack.CardsUp;

                for (int i = 0; i < mixedStack.CardsUp; i++)
                {
                    var cardY = currentY;
                    var nextY = currentY + (i == mixedStack.CardsUp - 1 ? CardHeight : overlapDistance);

                    if (position.Y >= cardY && position.Y < nextY)
                    {
                        return mixedStack[firstFaceUpIndex + i];
                    }

                    currentY += overlapDistance;
                }
            }

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
                var clickY = point.Position.Y;
                int cardCount = 1;
                int clickedCardIndex = -1;
                var clickedCardTopY = 0.0;

                if (Stack is MixedStack mixedStack)
                {
                    // Calculate which card was clicked in a mixed stack
                    var faceDownCount = mixedStack.Count - mixedStack.CardsUp;
                    var currentY = 0.0;

                    // Skip past face-down cards
                    currentY += faceDownCount * FaceDownPeekHeight;

                    // Check face-up cards
                    if (clickY >= currentY && mixedStack.CardsUp > 0)
                    {
                        var overlapDistance = CalculateOverlapDistance(mixedStack.CardsUp, Bounds.Height);
                        var firstFaceUpIndex = mixedStack.Count - mixedStack.CardsUp;

                        for (int i = 0; i < mixedStack.CardsUp; i++)
                        {
                            var cardY = currentY;
                            var nextY = currentY + (i == mixedStack.CardsUp - 1 ? CardHeight : overlapDistance);

                            if (clickY >= cardY && clickY < nextY)
                            {
                                clickedCardIndex = firstFaceUpIndex + i;
                                cardCount = mixedStack.Count - clickedCardIndex;
                                clickedCardTopY = cardY;
                                break;
                            }

                            currentY += overlapDistance;
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
                        clickedCardTopY = 0.0;
                    }
                }

                if (clickedCardIndex >= 0)
                {
                    var topLevel = TopLevel.GetTopLevel(this);
                    var topLevelPoint = topLevel != null ? e.GetPosition(topLevel) : point.Position;
                    var clickOffsetX = point.Position.X;
                    var clickOffsetY = point.Position.Y - clickedCardTopY;

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
            var faceDownCount = mixedStack.Count - mixedStack.CardsUp;
            var faceUpCount = mixedStack.CardsUp;

            double faceDownHeight;
            if (faceUpCount > 0)
            {
                // All face-down cards render as small peeks; the face-up cards below take over
                // showing full-size cards.
                faceDownHeight = faceDownCount > 0 ? faceDownCount * FaceDownPeekHeight : 0;
            }
            else
            {
                // No face-up cards (e.g. mid-drag): all but the topmost face-down card render as
                // peeks, and the topmost renders full-size so the stack still shows a card.
                faceDownHeight = faceDownCount > 0
                    ? (faceDownCount - 1) * FaceDownPeekHeight + CardHeight
                    : 0;
            }

            var overlapDistance = CalculateOverlapDistance(faceUpCount, availableSize.Height);
            var faceUpHeight = faceUpCount > 0
                ? CardHeight + (faceUpCount - 1) * overlapDistance
                : 0;

            var totalHeight = faceDownHeight + faceUpHeight;
            return new Size(CardWidth, totalHeight);
        }

        return new Size(CardWidth, CardHeight);
    }

    private double CalculateOverlapDistance(int cardCount, double availableHeight)
    {
        if (cardCount <= 1 || double.IsInfinity(availableHeight))
        {
            return OverlapDistance;
        }

        var totalHeightNeeded = CardHeight + (cardCount - 1) * OverlapDistance;
        if (totalHeightNeeded <= availableHeight)
        {
            return OverlapDistance;
        }

        // Reduce overlap distance to fit within available height
        var maxOverlap = (availableHeight - CardHeight) / (cardCount - 1);
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
        var brush = new SolidColorBrush(Color.FromArgb(255, 0, 200, 0));
        var pen = new Pen(brush, 12.0);
        var rect = new Rect(0, 0, CardWidth, CardHeight);

        // Draw a transparent rectangle over the full card area so that the
        // entire card size participates in hit testing, not just the X strokes.
        context.DrawRectangle(Brushes.Transparent, null, rect);

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
        // Draw each peek slightly taller than the spacing between peeks (~10% extra) so it overdraws
        // down onto the top of the peek below it, covering that card's transparent rounded corners
        // instead of letting the playing surface show through. Round to a whole pixel so the extra
        // slice height doesn't introduce a fractional-pixel scale (which would blur/anti-alias it).
        var peekRectHeight = Math.Max(FaceDownPeekHeight + 1, Math.Round(FaceDownPeekHeight * 1.1));
        if (peekRectHeight > s_maxMixedStackOverlapDistance)
        {
            s_maxMixedStackOverlapDistance = peekRectHeight;
        }

        var renderScaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
        var faceDownOverlapBitmap = MainWindowViewModel.GetFaceDownOverlapBitmap(
            s_maxMixedStackOverlapDistance, CardWidth, CardHeight, renderScaling);
        var faceDownBackingBitmap = MainWindowViewModel.GetFaceDownBackingBitmap(
            s_maxMixedStackOverlapDistance, CardWidth, CardHeight, renderScaling);

        var faceDownCount = mixedStack.Count - mixedStack.CardsUp;
        var currentY = 0.0;

        if (mixedStack.CardsUp > 0)
        {
            // Draw face-down cards as small peeks; face-up cards below will be full-size.
            if (faceDownCount > 0)
            {
                for (int i = 0; i < faceDownCount; i++)
                {
                    var rect = new Rect(0, currentY, CardWidth, peekRectHeight);
                    // Every slice except the topmost one needs an opaque backing behind it so its
                    // transparent rounded corners reveal matching card-back artwork instead of the
                    // playing surface beneath. The topmost slice (i == 0) has nothing above it, so
                    // it should legitimately show the playing surface through its corners.
                    if (i > 0)
                    {
                        context.DrawImage(faceDownBackingBitmap, rect);
                    }
                    context.DrawImage(faceDownOverlapBitmap, rect);
                    currentY += FaceDownPeekHeight;
                }
            }
        }
        else if (faceDownCount > 0)
        {
            // No face-up cards (e.g. mid-drag): draw all but the last face-down card as peeks,
            // then draw the topmost face-down card full-size so the stack still shows a card.
            for (int i = 0; i < faceDownCount - 1; i++)
            {
                var rect = new Rect(0, currentY, CardWidth, peekRectHeight);
                if (i > 0)
                {
                    context.DrawImage(faceDownBackingBitmap, rect);
                }
                context.DrawImage(faceDownOverlapBitmap, rect);
                currentY += FaceDownPeekHeight;
            }

            var topRect = new Rect(0, currentY, CardWidth, CardHeight);
            context.DrawImage(MainWindowViewModel.GetCardBackImage(), topRect);
        }

        // Draw face-up cards overlapping
        if (mixedStack.CardsUp > 0)
        {
            var overlapDistance = CalculateOverlapDistance(mixedStack.CardsUp, Bounds.Height);
            var firstFaceUpIndex = mixedStack.Count - mixedStack.CardsUp;

            for (int i = 0; i < mixedStack.CardsUp; i++)
            {
                var card = mixedStack[firstFaceUpIndex + i];
                var bitmap = MainWindowViewModel.ImageFromCard(card);
                var rect = new Rect(0, currentY, CardWidth, CardHeight);
                context.DrawImage(bitmap, rect);
                currentY += overlapDistance;
            }
        }
    }
}