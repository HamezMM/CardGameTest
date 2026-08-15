using Gamma931.Core.Game;
using Xunit;

namespace Gamma931.Core.Tests;

public class DeckTests
{
    [Fact]
    public void Draw_ReturnsEveryCardExactlyOnce()
    {
        var deck = new Deck<int>(Enumerable.Range(1, 10), new Random(42));

        var drawn = deck.Draw(10);

        Assert.Equal(Enumerable.Range(1, 10).ToHashSet(), drawn.ToHashSet());
        Assert.Equal(0, deck.DrawPileCount);
    }

    [Fact]
    public void Draw_ReshufflesDiscardPile_WhenDrawPileIsEmpty()
    {
        var deck = new Deck<int>(new[] { 1, 2, 3 }, new Random(1));
        var drawn = deck.Draw(3);
        deck.Discard(drawn);

        Assert.Equal(0, deck.DrawPileCount);
        Assert.Equal(3, deck.DiscardPileCount);

        var next = deck.Draw();

        Assert.Contains(next, new[] { 1, 2, 3 });
        Assert.Equal(0, deck.DiscardPileCount);
    }

    [Fact]
    public void Draw_Throws_WhenBothPilesAreEmpty()
    {
        var deck = new Deck<int>(Array.Empty<int>());

        Assert.Throws<InvalidOperationException>(() => deck.Draw());
    }
}
