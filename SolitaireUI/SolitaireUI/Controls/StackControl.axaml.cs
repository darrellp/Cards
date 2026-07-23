using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Cards;
using SolitaireUI.ViewModels;
using System;

namespace SolitaireUI.Controls;

public class StackControl : Control
{
    public static readonly StyledProperty<Stack?> StackProperty =
        AvaloniaProperty.Register<StackControl, Stack?>(nameof(Stack));

    public static readonly StyledProperty<bool> FaceUpProperty =
        AvaloniaProperty.Register<StackControl, bool>(nameof(FaceUp), defaultValue: true);

    public static readonly StyledProperty<double> CardWidthProperty =
        AvaloniaProperty.Register<StackControl, double>(nameof(CardWidth), defaultValue: 71.0);

    public static readonly StyledProperty<double> CardHeightProperty =
        AvaloniaProperty.Register<StackControl, double>(nameof(CardHeight), defaultValue: 96.0);

    public static readonly StyledProperty<double> OverlapDistanceProperty =
        AvaloniaProperty.Register<StackControl, double>(nameof(OverlapDistance), defaultValue: 20.0);

    public static readonly StyledProperty<double> FaceDownPeekHeightProperty =
        AvaloniaProperty.Register<StackControl, double>(nameof(FaceDownPeekHeight), defaultValue: 5.0);

    private Stack? _previousStack;

    static StackControl()
    {
        AffectsRender<StackControl>(StackProperty, FaceUpProperty, CardWidthProperty,
            CardHeightProperty, OverlapDistanceProperty, FaceDownPeekHeightProperty);
        AffectsMeasure<StackControl>(StackProperty, CardWidthProperty, CardHeightProperty,
            OverlapDistanceProperty, FaceDownPeekHeightProperty);

        StackProperty.Changed.AddClassHandler<StackControl>((control, args) =>
            control.OnStackChanged(args));
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
        InvalidateVisual();
        InvalidateMeasure();
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

            var faceDownHeight = faceDownCount > 0 ? faceDownCount * FaceDownPeekHeight : 0;
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
    }

    private void DrawEmptyStackIndicator(DrawingContext context)
    {
        var brush = new SolidColorBrush(Color.FromArgb(255, 0, 200, 0));
        var pen = new Pen(brush, 12.0);
        var rect = new Rect(0, 0, CardWidth, CardHeight);

        // Draw red X
        context.DrawLine(pen, rect.TopLeft, rect.BottomRight);
        context.DrawLine(pen, rect.TopRight, rect.BottomLeft);
    }

    private void RenderNormalStack(DrawingContext context)
    {
        if (FaceUp && Stack!.Count > 0)
        {
            var bottomCard = Stack[^1];
            var bitmap = MainViewModel.ImageFromCard(bottomCard);
            var rect = new Rect(0, 0, CardWidth, CardHeight);
            context.DrawImage(bitmap, rect);
        }
        else
        {
            // Draw face-down card (blue rectangle)
            var rect = new Rect(0, 0, CardWidth, CardHeight);
            var cornerRadius = 5.0;
            context.DrawRectangle(Brushes.DodgerBlue, new Pen(Brushes.Black, 1.0), rect, cornerRadius);
        }
    }

    private void RenderMixedStack(DrawingContext context, MixedStack mixedStack)
    {
        var faceDownCount = mixedStack.Count - mixedStack.CardsUp;
        var currentY = 0.0;

        // Draw face-down cards as small peeks
        if (faceDownCount > 0)
        {
            for (int i = 0; i < faceDownCount; i++)
            {
                var rect = new Rect(0, currentY, CardWidth, FaceDownPeekHeight + 1);
                var cornerRadius = 3.0;
                context.DrawRectangle(Brushes.DodgerBlue, new Pen(Brushes.Black, 1.0), rect, cornerRadius);
                currentY += FaceDownPeekHeight;
            }
        }

        // Draw face-up cards overlapping
        if (mixedStack.CardsUp > 0)
        {
            var overlapDistance = CalculateOverlapDistance(mixedStack.CardsUp, Bounds.Height);
            var firstFaceUpIndex = mixedStack.Count - mixedStack.CardsUp;

            for (int i = 0; i < mixedStack.CardsUp; i++)
            {
                var card = mixedStack[firstFaceUpIndex + i];
                var bitmap = MainViewModel.ImageFromCard(card);
                var rect = new Rect(0, currentY, CardWidth, CardHeight);
                context.DrawImage(bitmap, rect);
                currentY += overlapDistance;
            }
        }
    }
}