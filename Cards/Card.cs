namespace Cards;

public enum Suit
{
    Club,
    Diamond,
    Heart,
    Spade,
    None,
}

/// <summary>
/// Definition of a card
/// </summary>
///
/// <remarks>
/// The ranks start with 1 as the ace.  0 is an illegal rank.  13 is the kind.
/// </remarks>
/// 
/// <param name="Rank">Rank of the card</param>
/// <param name="Suit">Suit of the card</param>
public record Card(byte Rank, Suit Suit)
{
    public const int JACK = 11;
    public const int QUEEN = 12;
    public const int KING = 13;
    public const int ACE = 1;
    
    public static readonly Card NullCard = new Card(0, Suit.None);
    public static char SuitAbbrev(Suit suitParm)
    {
        return suitParm switch
        {
            Cards.Suit.Club => 'C',
            Cards.Suit.Diamond => 'D',
            Cards.Suit.Heart => 'H',
            Cards.Suit.Spade => 'S',
            _ => 'X'
        };
    }

    public static char RankAbbrev(int rankParm)
    {
        if (rankParm < 1 || rankParm > 13)
        {
            throw new ArgumentOutOfRangeException("Cards must be ranked from 1 to 13");
        }
        if (rankParm > 1 && rankParm < 10)
        {
            return (char)('0' + rankParm);
        }
        
        return rankParm switch
        {
            1 => 'A',
            10 => 'T',
            11 => 'J',
            12 => 'Q',
            13 => 'K',
            // ReSharper disable once UnreachableSwitchArmDueToIntegerAnalysis
            _ => '\0'
        };
    }

    public static Card CardFromString(string cardString)
    {
        Byte rank = 0;
        char rankChar = cardString[0];
        if (rankChar >= '1' && rankChar <= '9')
        {
            rank = (Byte)(rankChar - '0');
        }
        else
        {
            rank = cardString[0] switch
            {
                'A' => 1,
                'T' => 10,
                'J' => 11,
                'Q' => 12,
                'K' => 13,
                _ => throw new ArgumentException($"Invalid card string {cardString}")
            };
        }
        var suit = cardString[1] switch
        {
            'C' => Suit.Club,
            'D' => Suit.Diamond,
            'H' => Suit.Heart,
            'S' => Suit.Spade,
            _ => throw new ArgumentException($"Invalid card string {cardString}")
        };
        return new Card(rank, suit);
    }
    
    /// <summary>
    /// Checks if card is the same color as this card
    /// </summary>
    /// 
    /// <param name="card">The card to check against</param>
    /// 
    /// <returns>True if the cards are the same color, else false</returns>
    public bool IsSameColor(Card card)
    {
        return card != NullCard && !this.IsBlack ^ card.IsBlack;
    }
    
    /// <summary>
    /// Legally "below" as defined in Klondike tableau
    /// </summary>
    /// 
    ///<remarks>
    /// KBelow stands for "Klondike Below" and means that this card is one
    /// lower in rank and opposite color from the card passed in making it a
    /// legal move in Klondike.
    /// </remarks>
    /// 
    /// <param name="cardAbove">
    /// We test if this card can legally be placed below cardAbove in a Klondike Tableau
    /// </param>
    /// 
    /// <returns>True if this card can be placed below cardAblve, else false</returns>
     public bool IsKBelow(Card cardAbove)
    {
        if (this == Card.NullCard)
        {
            return false;
        }
        if (cardAbove == Card.NullCard)
        {
            return this.Rank == KING;
        }
        return !IsSameColor(cardAbove) && Rank == cardAbove.Rank - 1;
    }

    /// <summary>
    /// Returns true if this is a black card
    /// </summary>
    public bool IsBlack => Suit == Suit.Club || Suit == Suit.Spade;
    public static bool IsBlackSuit(Suit suit) => suit == Suit.Club || suit == Suit.Spade;
    
    /// <summary>
    /// Returns true if this is a red card
    /// </summary>
    public bool IsRed => !IsBlack;

    public override string ToString()
    {
        return $"{RankAbbrev(Rank)}{SuitAbbrev(Suit)}";
    }
}