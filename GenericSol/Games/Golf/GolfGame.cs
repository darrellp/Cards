using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Cards;
using System.Diagnostics;
using System.Text.Json;

namespace GenericSol.Games.Golf;
public partial class GolfGame : GenericGame
{
    #region Constants
    public const int TabCount = 7;
    public const int TabSize = 5;
    #endregion

    GolfAi _ai;

    #region Stacks
    // These are only internal for unit testing purposes
    // ReSharper disable InconsistentNaming
    internal MixedStack[] _tableau { get; } = new MixedStack[TabCount];
    internal MixedStack _foundation;
    internal Stack _stock;
    // ReSharper restore InconsistentNaming

    internal IEnumerable<MixedStack> Tableaus()
    {
        for (var iStack = 0; iStack < TabCount; iStack++)
        {
            yield return _tableau[iStack];
        }
    }
    #endregion

    public override IAi Ai => (IAi)_ai;

    public override IList<IMove> GetMoves()
    {
        return new List<IMove> { new GenericMove("From", "To") };
    }

    #region Stack name helpers
    internal static string TabNameFromIndex(int index)
    {
        if (index < 0 || index >= TabCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Tableau index must be between 0 and 6.");
        }
        return $"tab{index + 1}";
    }

    public override Stack StackFromName(string name)
    {
        if (name.StartsWith("tab"))
        {
            var index = int.Parse(name.Substring(3));
            if (index > TabCount || index < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(name), "Tableau index must be between 0 and 6.");
            }
            return _tableau[index - 1];
        }
        else if (name == "foundation")
        {
            return _foundation;
        }
        else if (name == "stock")
        {
            return _stock;
        }
        else
        {
            throw new ArgumentException($"Invalid stack name: {name}");
        }
    }
    #endregion

    public GolfGame(int seed = -1) : base(seed)
    {
        _ai = new GolfAi();
        _ai.Game = this;
        var deck = Stack.ShuffledDeck();

        for (int i = 0; i < TabCount; i++)
        {
            _tableau[i] = MixedStack.FromStack(deck.Split(TabSize), TabSize);
            _tableau[i].Name = $"tab{i + 1}";
        }
        _foundation = new MixedStack(new List<Card>(), 0)
        {
            Name = "foundation"
        };

        _stock = deck;
        _stock.Name = "stock";
    }

    #region Mouse Interaction
    public override void OnLeftClick(Stack stack)
    {
        if (stack.Count == 0)
        {
            return;
        }
        GenericMove finalMove = GenericMove.NoMove;
        if (stack.Name == "stock")
        {
            finalMove = new GenericMove("stock", "foundation");
        }
        else if (stack.Name.StartsWith("tab"))
        {
            if (_foundation.Count == 0)
            {
                finalMove = new GenericMove(stack.Name, "foundation");
            }
            else
            {
                finalMove = CheckStack(stack.Name);
            }
        }

        if (finalMove != GenericMove.NoMove)
        {
            ApplyMove(finalMove);
        }
    }

    bool CheckLoss()
    {
        return _stock.Count == 0 && Enumerable.Range(1, 7).Select(i => $"tab{i}").All(n => CheckStack(n) == GenericMove.NoMove);
    }

    private GenericMove CheckStack(string stackName)
    {
        var tabStack = StackFromName(stackName);
        if (tabStack.Count == 0)
        {
            return GenericMove.NoMove;
        }
        var ret = GenericMove.NoMove;

        var playable = CheckPlayability(tabStack.TopCard, _foundation.TopCard);
        if (playable)
        {
            ret = new GenericMove(stackName, "foundation");
        }

        return ret;
    }

    public bool CheckPlayability(Card cardSrc, Card cardDst)
    {
        if (cardSrc == Card.NullCard)
        {
            return false;
        }
        var zbRankSrc = cardSrc.Rank - 1;
        var zbRankDest = cardDst.Rank - 1;
        if (OptionsCur.AceKingWrap)
        {
            if ((zbRankSrc + 1) % 13 == zbRankDest || (zbRankSrc + 12) % 13 == zbRankDest)
            {
                return true;
            }
        }
        else
        {
            if (Math.Abs(zbRankSrc - zbRankDest) == 1)
            {
                return true;
            }
        }

        return false;
    }

    public override bool IsMoveValid(Stack stkSrc, string srcName, Stack stkDst, int cardCount)
    {
        // We never drag in Golf - just left click
        return false;
    }
    #endregion

    #region Move overrides
    public override void ApplyAbstractPostMove(IMove move)
    {
        if (Tableaus().Select(s => s.Count).Sum() == 0)
        {
            GameState.EventOccurred("Won");
        }
        else if (CheckLoss())
        {
            GameState.EventOccurred("Lost");
        }

    }
    #endregion

    #region Info
    Options OptionsCur = new Options();

    override public void SetupInfo(Grid options, out string markdown)
    {
        markdown = """
            # Golf Solitaire
            Golf Solitaire is a fast-paced card game where the goal is to clear
            a layout of 35 cards (seven columns of five overlapping face-up cards) 
            by moving cards to a single waste pile. 
            You can transfer any exposed bottom tableau card to the waste pile if 
            it is strictly one rank higher or lower than the current top waste card, 
            regardless of suit.
            #### **Game Setup**
            - Shuffle a standard 52-card deck.
            - Deal 35 cards face-up into seven columns (or "tableau" piles) with five cards overlapping in each column.
            - Place the remaining 17 cards face-down next to the layout to form the stock pile.
            - Flip the top card of the stock pile face-up to start the waste (discard) foundation pile or, optionally, select a starting card from the tableau to begin the waste pile.
            #### **Rules of Play**
            - **Matching:** Only the bottommost, exposed card of each of the seven columns is available to play.
            - **Sequencing:** Move a card from the column to the top of the waste pile if its rank is one higher or one lower than the current waste card. Suits do not matter.
            - **Ranking:** Aces are low (1) and Kings are high (13). In standard rules, sequences do not "wrap around"—you cannot place an Ace on a King or a King on an Ace (though a popular "easy mode" variant allows wrapping).
            - **Drawing from Stock:** When no more moves are available from the seven columns, flip the top card of the stock pile onto the waste pile and continue matching from the columns. There is no redeal from the stock.
            #### **Winning and Scoring**
            - **Win Condition:** Clear all 35 cards from the columns into the waste pile before running out of cards in the stock.
            """;

        string xaml = @"
<StackPanel xmlns='https://github.com/avaloniaui'>
    <CheckBox Name='AceKingWrap' Content='Allow Ace to King wrapping' />
    <CheckBox Name='SelectStartCard' Content='Play starting card from tableaux' />
</StackPanel>";

        StackPanel panel = AvaloniaRuntimeXamlLoader.Parse<StackPanel>(xaml);
        var cbAceKingWrap = panel.FindControl<CheckBox>("AceKingWrap");
        if (cbAceKingWrap != null)
        {
            cbAceKingWrap.IsChecked = OptionsCur.AceKingWrap;
        }
        var cbSelectStartCard = panel.FindControl<CheckBox>("SelectStartCard");
        if (cbSelectStartCard != null)
        {
            cbSelectStartCard.IsChecked = OptionsCur.SelectStartCard;
        }
        options.Children.Add(panel);
    }

    public override void SetOptionsFromUI(Grid options)
    {
        var cbAceKingWrap = options.GetVisualDescendants().OfType<CheckBox>()
            .FirstOrDefault(cb => cb.Name == "AceKingWrap");
        Debug.Assert(cbAceKingWrap != null);
        OptionsCur.AceKingWrap = cbAceKingWrap.IsChecked == true;

        var cbSelectStartCard = options.GetVisualDescendants().OfType<CheckBox>()
            .FirstOrDefault(cb => cb.Name == "SelectStartCard");
        Debug.Assert(cbSelectStartCard != null);
        OptionsCur.SelectStartCard = cbSelectStartCard.IsChecked == true;
    }

    public override void SetOptions(IJsonSerializable options)
    {
        Debug.Assert(options != null);
        OptionsCur = options as Options;

        // We'd like to do this in the game constructor but the options are created after the constructor runs.
        // Note: we mutate the existing _foundation/_stock stacks in place (rather than reassigning _foundation
        // to a new MixedStack) because external references to these stack objects (e.g. bound view-model
        // properties) may already have been captured before SetOptions runs, particularly at app startup.
        if (!OptionsCur.SelectStartCard && _foundation.Count == 0)
        {
            _foundation.Merge(_stock.Split(1), 1);
        }

    }

    public override IJsonSerializable GetOptions()
    {
        return OptionsCur;
    }

    public override IJsonSerializable DeserializeOptions(string json)
    {
        return Options.FromJson(json);
    }

    public class Options : IJsonSerializable<Options>
    {
        public bool AceKingWrap { get; set; } = true;
        public bool SelectStartCard { get; set; } = true;

        public static Options FromJson(string json)
        {
            return JsonSerializer.Deserialize(json, GolfOptionsJsonContext.Default.GolfOptions)!;
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, GolfOptionsJsonContext.Default.GolfOptions);
        }
    }
    #endregion

}

[System.Text.Json.Serialization.JsonSerializable(typeof(GolfGame.Options), TypeInfoPropertyName = "GolfOptions")]
partial class GolfOptionsJsonContext : System.Text.Json.Serialization.JsonSerializerContext
{
}
