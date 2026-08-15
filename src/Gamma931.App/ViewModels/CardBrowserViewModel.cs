using System.ComponentModel;
using System.Windows.Data;
using Gamma931.Core.Data;

namespace Gamma931.App.ViewModels;

/// <summary>Read-only browser over everything loaded from CSV, with a shared text search across all card types.</summary>
public sealed class CardBrowserViewModel : ObservableObject
{
    private string _searchText = string.Empty;

    public ICollectionView Characters { get; }
    public ICollectionView Bosses { get; }
    public ICollectionView Minions { get; }
    public ICollectionView CrabActions { get; }
    public ICollectionView DamageCards { get; }
    public ICollectionView Equipment { get; }
    public ICollectionView Locations { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (Set(ref _searchText, value))
            {
                RefreshAll();
            }
        }
    }

    public CardBrowserViewModel(CardDatabase db)
    {
        Characters = MakeFilteredView(db.Characters);
        Bosses = MakeFilteredView(db.Bosses);
        Minions = MakeFilteredView(db.Minions);
        CrabActions = MakeFilteredView(db.CrabActions);
        DamageCards = MakeFilteredView(db.DamageCards);
        Equipment = MakeFilteredView(db.Equipment);
        Locations = MakeFilteredView(db.Locations);
    }

    private ICollectionView MakeFilteredView(System.Collections.IEnumerable source)
    {
        var view = CollectionViewSource.GetDefaultView(source);
        view.Filter = Matches;
        return view;
    }

    private bool Matches(object card) =>
        string.IsNullOrWhiteSpace(SearchText) ||
        (card.ToString()?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false);

    private void RefreshAll()
    {
        Characters.Refresh();
        Bosses.Refresh();
        Minions.Refresh();
        CrabActions.Refresh();
        DamageCards.Refresh();
        Equipment.Refresh();
        Locations.Refresh();
    }
}
