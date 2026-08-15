using Gamma931.Core.Data;

namespace Gamma931.App.ViewModels;

public sealed class MainViewModel
{
    public CardBrowserViewModel CardBrowser { get; }
    public GameViewModel Game { get; }

    public MainViewModel(CardDatabase db)
    {
        CardBrowser = new CardBrowserViewModel(db);
        Game = new GameViewModel(db);
    }
}
