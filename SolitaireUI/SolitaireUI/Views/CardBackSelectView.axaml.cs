using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using SolitaireUI.ViewModels;
using Avalonia.VisualTree;

namespace SolitaireUI.Views;

public partial class CardBackSelectView : UserControl
{
    private ItemsControl? _cardBacksItemsControl;

    public CardBackSelectView()
    {
        InitializeComponent();

        this.Loaded += (s, e) =>
        {
            _cardBacksItemsControl = this.FindControl<ItemsControl>("CardBacksItemsControl");

            if (this.DataContext is CardBackSelectViewModel viewModel)
            {
                viewModel.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(CardBackSelectViewModel.SelectedCardBackIndex))
                    {
                        UpdateCardBackSelection();
                    }
                };

                // Initial selection
                UpdateCardBackSelection();
            }
        };
    }

    private void UpdateCardBackSelection()
    {
        if (_cardBacksItemsControl?.Presenter is not null && 
            this.DataContext is CardBackSelectViewModel viewModel)
        {
            // Iterate through the visual tree and update the borders
            UpdateBordersInContainer(_cardBacksItemsControl, viewModel.SelectedCardBackIndex);
        }
    }

    private void UpdateBordersInContainer(Control container, int selectedIndex)
    {
        foreach (var child in container.GetVisualDescendants())
        {
            if (child is Border border && border.Name == "SelectionBorder")
            {
                // Find the parent button to get the data context
                if (FindParentOfType<Button>(border) is Button button && 
                    button.DataContext is CardBackOption option)
                {
                    if (option.Index == selectedIndex)
                    {
                        border.BorderBrush = new SolidColorBrush(Color.Parse("#FFD700"));
                        border.BorderThickness = new Thickness(4);
                    }
                    else
                    {
                        border.BorderBrush = new SolidColorBrush(Colors.Transparent);
                        border.BorderThickness = new Thickness(4);
                    }
                }
            }
        }
    }

    private static T? FindParentOfType<T>(Control child) where T : Control
    {
        var parent = child.Parent;
        while (parent != null)
        {
            if (parent is T control)
                return control;
            parent = (parent as StyledElement)?.Parent;
        }
        return null;
    }
}




