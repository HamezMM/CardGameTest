namespace Gamma931.Core.Game;

/// <summary>
/// A generic draw pile with a discard pile. RULES.md: on exhaustion, "reshuffle the discard
/// pile into a new deck and continue play uninterrupted" — <see cref="Draw"/> does this
/// automatically rather than throwing when the draw pile runs dry.
/// </summary>
public sealed class Deck<T>
{
    private readonly List<T> _drawPile;
    private readonly List<T> _discardPile = new();
    private readonly Random _random;

    public Deck(IEnumerable<T> cards, Random? random = null)
    {
        _random = random ?? Random.Shared;
        _drawPile = cards.ToList();
        Shuffle(_drawPile);
    }

    public int DrawPileCount => _drawPile.Count;
    public int DiscardPileCount => _discardPile.Count;

    /// <summary>Draws one card, reshuffling the discard pile in if the draw pile is empty.</summary>
    /// <exception cref="InvalidOperationException">Both piles are empty — nothing left to draw.</exception>
    public T Draw()
    {
        if (_drawPile.Count == 0)
        {
            ReshuffleDiscardIntoDrawPile();
        }

        if (_drawPile.Count == 0)
        {
            throw new InvalidOperationException("Deck is fully exhausted: no cards in draw or discard pile.");
        }

        var top = _drawPile[^1];
        _drawPile.RemoveAt(_drawPile.Count - 1);
        return top;
    }

    public List<T> Draw(int count)
    {
        var drawn = new List<T>(count);
        for (var i = 0; i < count; i++)
        {
            drawn.Add(Draw());
        }

        return drawn;
    }

    public void Discard(T card) => _discardPile.Add(card);

    public void Discard(IEnumerable<T> cards) => _discardPile.AddRange(cards);

    private void ReshuffleDiscardIntoDrawPile()
    {
        _drawPile.AddRange(_discardPile);
        _discardPile.Clear();
        Shuffle(_drawPile);
    }

    private void Shuffle(List<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
