using Avalonia.Media.Imaging;
using Cards;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace SolitaireUI.ViewModels;

public partial class CardBackSelectViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly int _originalSelectedBackIndex;

    [ObservableProperty]
    private int _selectedCardBackIndex;

    public ObservableCollection<CardBackOption> AvailableCardBacks { get; }

    public CardBackSelectViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _originalSelectedBackIndex = Card.GetSelectedCardBackIndex();
        AvailableCardBacks = new ObservableCollection<CardBackOption>();
        _selectedCardBackIndex = _originalSelectedBackIndex;

        // Load available card backs
        LoadAvailableCardBacks();
    }

    private void LoadAvailableCardBacks()
    {
        AvailableCardBacks.Clear();
        var availableIndices = Card.GetAvailableCardBackIndices();

        foreach (var index in availableIndices)
        {
            try
            {
                using (var stream = Card.GetCardBackImageByIndex(index))
                {
                    var bitmap = new Bitmap(stream);
                    AvailableCardBacks.Add(new CardBackOption
                    {
                        Index = index,
                        Image = bitmap
                    });
                }
            }
            catch
            {
                // Skip any card backs that fail to load
            }
        }
    }

    [RelayCommand]
    public void SelectCardBack(int index)
    {
        SelectedCardBackIndex = index;
    }

    [RelayCommand]
    public void Okay()
    {
        Card.SetSelectedCardBackIndex(SelectedCardBackIndex);
        _mainWindowViewModel.RefreshCardBackImage();
        _mainWindowViewModel.NavigateBack();
    }

    [RelayCommand]
    public void Cancel()
    {
        // Restore original selection
        Card.SetSelectedCardBackIndex(_originalSelectedBackIndex);
        _mainWindowViewModel.NavigateBack();
    }
}

public class CardBackOption
{
    public int Index { get; set; }
    public Bitmap? Image { get; set; }
}

