using System.Collections;
using System.Reflection;

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
    #region Constants
    // ReSharper disable InconsistentNaming
    public const int JACK = 11;
    public const int QUEEN = 12;
    public const int KING = 13;
    public const int ACE = 1;
    // ReSharper restore InconsistentNaming
    #endregion
    
    #region Static fields
    public static readonly Card NullCard = new Card(0, Suit.None);

    public static readonly string[] ImageRanks =
    [
        "illegal",
        "ace",
        "2",
        "3",
        "4",
        "5",
        "6",
        "7n",
        "8",
        "9",
        "10",
        "jack",
        "queen",
        "king"
    ];
    #endregion

    #region Properties
    public int Index => (Rank - 1) + (int)Suit * 13;
    #endregion

    #region Naming
    private static char SuitAbbrev(Suit suitParm)
    {
        return suitParm switch
        {
            Suit.Club => 'C',
            Suit.Diamond => 'D',
            Suit.Heart => 'H',
            Suit.Spade => 'S',
            _ => 'X'
        };
    }

    private static char RankAbbrev(int rankParm)
    {
        return rankParm switch
        {
            < 1 or > 13 => throw new ArgumentOutOfRangeException(nameof(rankParm)),
            > 1 and < 10 => (char)('0' + rankParm),
            _ => rankParm switch
            {
                1 => 'A',
                10 => 'T',
                11 => 'J',
                12 => 'Q',
                13 => 'K',
                // ReSharper disable once UnreachableSwitchArmDueToIntegerAnalysis
                _ => '\0'
            }
        };
    }

    public static Card CardFromString(string cardString)
    {
        byte rank;
        var rankChar = cardString[0];
        if (rankChar is >= '1' and <= '9')
        {
            rank = (byte)(rankChar - '0');
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
        return new(rank, suit);
    }

    private string ImageRank()
    {
        return ImageRanks[Rank];
    }

    private string ImageSuit()
    {
        return Suit switch
        {
            Suit.Club => "clubs",
            Suit.Diamond => "diamonds",
            Suit.Heart => "hearts",
            Suit.Spade => "spades",
            _ => throw new ArgumentException($"Invalid suit {Suit}")
        };
    }

    private string ImageFile()
    {
        var variant = Rank > 10 ? "2" : "";
        return $"{ImageRank()}_of_{ImageSuit()}{variant}.png";
    }

    public Stream ImageStream()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var names = assembly.GetManifestResourceNames();
        var resourcePath = $"Cards.Resources.Playing_Cards.{ImageFile()}";
        return assembly.GetManifestResourceStream(resourcePath)!;
    }
    #endregion
    
    #region Queries
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

    public IEnumerator<Card> GetEnumerator()
    {
        throw new NotImplementedException();
    }
    #endregion
}