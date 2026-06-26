namespace Cards;

/// <summary>
/// Intended to represent a stack where the top cards are faceup and the remainder facedown
/// </summary>
public sealed class MixedStack : Stack
{
    public int CardsUp { get; set; }

    private MixedStack() : base([])
    {
        CardsUp = 0;
    }
    
    public MixedStack(List<Card> cards, int cardsFaceUp) : base(cards)
    {
        CardsUp = cardsFaceUp;
    }

    public static MixedStack FromStack(Stack stack, int cFaceUp)
    {
        var ret = new MixedStack(stack._cards, cFaceUp);
        stack._cards = [];
        return ret;
    }

    public Card FirstFaceupCard
    {
        get
        {
            if (Count == 0 || CardsUp == 0)
            {
                return Card.NullCard;
            }

            return this[^CardsUp];
        }
    }

    public override void Reverse()
    {
        throw new InvalidOperationException("Can't reverse UpDown stacks");
    }

    public override Stack Split(int n)
    {
        if (n == -1)
        {
            // take all faceup cards
            n = CardsUp;
        }
        // We can only split the faceup cards because we don't know how to treat face up differently
        // than facedown.  This isn't a logical necessity but seems correct for the vast majority of
        // cases.  This is an easy enough decision to reverse later if it turns out to be in the way.
        if (n < 0 || n > CardsUp)
        {
            throw new ArgumentOutOfRangeException(nameof(n), "n must be less than " + nameof(CardsUp));
        }
        
        CardsUp = Math.Max(0, CardsUp - n);
        return base.Split(n);
    }

    /// <summary>
    /// Merge with a stack
    /// </summary>
    ///
    /// <remarks>
    /// We assume that all the incoming merged cards will be faceup.
    /// </remarks>
    /// 
    /// <param name="sourceStack">Stack to be merged into this one</param>
    /// <param name="cardCount">Count of cards to be merged</param>
    public override void Merge(Stack sourceStack, int cardCount = -1)
    {
        if (cardCount < 0)
        {
            cardCount = sourceStack.Count;
        }
        base.Merge(sourceStack, cardCount);
        CardsUp += cardCount;
    }

    /// <summary>
    /// Split a FaceUpDownStack
    /// </summary>
    ///
    /// <remarks>
    /// You can only split the face up cards in a FaceUpDownStack.  
    /// </remarks>
    /// 
    /// <param name="n">Number of cards face up in the stack or -1 to move all faceup cards</param>
    /// 
    /// <returns>A stack of all face up cards of size n</returns>
    /// 
    /// <exception cref="ArgumentOutOfRangeException">
    /// If n is negative or larger than the number of face up cards
    /// </exception>
    public MixedStack SplitUpDown(int n = -1)
    {
        if (n == -1)
        {
            // take all faceup cards
            n = CardsUp;
        }
        
        // We can only split the faceup cards
        if (n < 0 || n > CardsUp)
        {
            throw new ArgumentOutOfRangeException(nameof(n), "n must be less than " + nameof(CardsUp));
        }
        
        var tmp = base.Split(n);
        CardsUp -= n;
        return new MixedStack(tmp._cards, n);
    }
    
    public override string ToString()
    {
        if (Count == 0)
        {
            return "|";
        }
        
        var orig = base.ToString();

        if (CardsUp == 0)
        {
            return $"{orig}|";
        }

        if (CardsUp == Count)
        {
            return $"|{orig}";
        }

        var cFaceDown = Count - CardsUp;
        var prefixLength =3 * cFaceDown - 1;
        var suffixLength = orig.Length - prefixLength - 1;
        var suffixStart = prefixLength == 0 ? 0 : prefixLength + 1;
        var prefix = orig.AsSpan(0, Math.Max(0, prefixLength));
        var suffix = orig.AsSpan(suffixStart, suffixLength);
        return string.Concat(prefix, " | ", suffix);
    }

    /// <summary>
    /// Add a card to this stack
    /// </summary>
    /// 
    /// <remarks>
    /// Added card is assumed to be faceup
    /// </remarks>
    /// 
    /// <param name="card">Card to be added</param>
    public override void Add(Card card)
    {
        base.Add(card);
        CardsUp++;
    }

    /// <summary>
    /// Add a card to this stack
    /// </summary>
    ///
    /// <remarks>
    /// Card is assumed to be faceup
    /// </remarks>
    /// 
    /// <param name="card">Card to be added</param>
    public void Add(string cardString)
    {
        base.Add(cardString);
        CardsUp++;
    }

    public Card TopFaceupCard()
    {
        if (Count == 0 || CardsUp == 0)
        {
            return Card.NullCard;
        }
        return this[^CardsUp];
    }
    
    /// <summary>
    /// Creates a stack from it's string representation
    /// </summary>
    /// 
    /// <param name="stackString">String to convert</param>
    /// <returns>The stack from the string</returns>
    public static MixedStack ParseMixed(string stackString)
    {
       if (!stackString.Contains('|'))
        {
            throw new ArgumentException("Stack string must contain '|'");
        }

        if (stackString == "|")
        {
            return new MixedStack();
        }
        
        // Ensure that if a string begins or ends with '|' and no space we still work
        if (stackString[0] == '|' && stackString[1] != ' ')
        {
            stackString = "| " + stackString.Substring(1);
        }

        if (stackString[^1] == '|' && stackString[^2] != ' ')
        {
            stackString = stackString.Substring(0, stackString.Length - 1) + " |";
        }

        var cardStrings = stackString.Split(' ').ToList();
        var sepIndex = cardStrings.IndexOf("|");
        if (sepIndex == -1)
        {
            throw new ArgumentException("Illegal string in FaceUpDownStack.StackFromString");
        }
        var cCardsUp = cardStrings.Count - sepIndex - 1;
        cardStrings.RemoveAt(sepIndex);
        var list = cardStrings.Select(s => Card.CardFromString(s)).ToList();
        return new MixedStack(list, cCardsUp);
    }
}