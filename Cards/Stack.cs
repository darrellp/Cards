using System.Collections;

namespace Cards;

/// <summary>
/// Class to represent card stacks in a cards.dll client
/// </summary>
/// 
/// <remarks>
/// Stacks are defined as having the 0'th card at the "bottom" of the stack.
/// Bottom is not terribly well defined but generally if a stack would "sit" on
/// the physical playing surface then the bottom card would be the one closest to the surface
/// and the top card would be the one easiest to remove away from the playing surface.
/// If the stack doesn't actually sit on the playing surface then the bottom is defined
/// specifically for the stack.
/// </remarks>
/// 
/// <param name="cards">List of cards making up the stack from bottom to top</param>
public class Stack(List<Card> cards) : IEnumerable<Card>
{
    // Event to notify when the stack is modified
    public event EventHandler? StackModified;

    // Actual list of cards in this stack
    protected internal List<Card> _cards = cards;
    public int Count => _cards.Count;

    public Stack() : this([]) { }

    protected virtual void OnStackModified()
    {
        StackModified?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Returns a stack of the top n cards and leaves the bottom cards in the original stack
    /// </summary>
    ///
    /// <remarks>
    /// This method has the side effect of also truncating the original stack.
    /// </remarks>
    /// 
    /// <param name="n">Number of cards in the stack returned</param>
    /// 
    /// <returns>A stack of the top n cards</returns>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when asking to split more elements than are in the original stack or if n is negative
    /// </exception>
    public virtual Stack Split(int n)
    {
        if (n < 0)
        {
            throw new ArgumentOutOfRangeException($"{n} must not be negative");
        }
        if (Count < n)
        {
            throw new ArgumentOutOfRangeException(
                $"Trying to split {n} cards from a stack with only {Count} cards");
        }

        var bottomCount = Count - n;
        var newBottomList = _cards.Take(bottomCount).ToList();
        var newTopList = _cards.Skip(bottomCount).ToList();
        _cards = newBottomList;
        OnStackModified();
        return new Stack(newTopList);
    }

    /// <summary>
    /// Takes cardCount cards off the top of sourceStack and puts them on the top of this stack.
    /// </summary>
    /// 
    /// <param name="sourceStack">Stack to take cards from</param>
    /// <param name="cardCount">
    /// Count of cards to take.  If -1 (the default) take them all
    /// </param>
    public virtual void Merge(Stack sourceStack, int cardCount = -1)
    {
        if (cardCount == -1)
        {
            _cards.AddRange(sourceStack._cards);
            sourceStack._cards = [];
            OnStackModified();
            sourceStack.OnStackModified();
            return;
        }

        if (cardCount < 0 || cardCount > sourceStack.Count)
        {
            throw new ArgumentOutOfRangeException("Invalid count in Stack.Merge()");
        }

        int sourceSizePostOp = sourceStack.Count - cardCount;
        _cards = _cards.Concat(sourceStack._cards.Skip(sourceSizePostOp)).ToList();
        sourceStack._cards = sourceStack._cards.Take(sourceSizePostOp).ToList();
        OnStackModified();
        sourceStack.OnStackModified();
    }

    public virtual void Reverse()
    {
        _cards.Reverse();
        OnStackModified();
    }

    public void Shuffle(Random? rnd = null)
    {
        if (rnd == null)
        {
            rnd = new Random();
        }
        var cardsArray = _cards.ToArray();
        rnd.Shuffle(cardsArray);
        _cards = cardsArray.ToList();
        OnStackModified();
    }

    /// <summary>
    /// Sorted deck of 52 cards sorted first on suit and then on rank
    /// </summary>
    /// 
    /// <returns>Sorted deck of cards</returns>
    public static Stack SortedDeck()
    {
        var deck = new List<Card>();

        foreach (Suit suit in (Enum.GetValues(typeof(Suit))))
        {
            if (suit == Suit.None)
            {
                continue;
            }
            for (Byte rank = 1; rank <= 13; rank++)
            {
                deck.Add(new Card(rank, suit));
            }
        }

        return new Stack(deck);
    }

    /// <summary>
    /// Standard shuffled deck of 52 cards
    /// </summary>
    /// 
    /// <returns>Deck of cards</returns>
    public static Stack ShuffledDeck(Random? rnd = null)
    {
        var deck = SortedDeck();
        deck.Shuffle(rnd);
        return deck;
    }

    /// <summary>
    /// Creates a stack from it's string representation
    /// </summary>
    /// 
    /// <param name="stackString">String to convert</param>
    /// <returns>The stack from the string</returns>
    public static Stack Parse(string stackString)
    {
        var cardStrings = stackString.Split(' ');
        var list = cardStrings.Select(s => Card.CardFromString(s)).ToList();
        return new Stack(list);
    }

    public Card TopCard
    {
        get
        {
            return _cards.Count == 0 ? Card.NullCard : this[^1];
        }
    }

    /// <summary>
    /// Add a card to this stack
    /// </summary>
    /// 
    /// <param name="card">Card to be added</param>
    public virtual void Add(Card card)
    {
        _cards.Add(card);
        OnStackModified();
    }

    /// <summary>
    /// Add a card to this stack
    /// </summary>
    /// 
    /// <param name="cardString">Name of card to be added</param>
    public virtual void Add(string cardString)
    {
        var card = Card.CardFromString(cardString);
        _cards.Add(card);
        OnStackModified();
    }

    public Card this[int index]
    {
        get => _cards[index];
    }

    public override string ToString()
    {
        return string.Join(" ", _cards);
    }

    internal void Replace(int iCard, Card card)
    {
        _cards[iCard] = card;
        OnStackModified();
    }

    internal void Replace(int iCard, String cardName)
    {
        _cards[iCard] = Card.CardFromString(cardName);
        OnStackModified();
    }

    public IEnumerator<Card> GetEnumerator()
    {
        return _cards.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
