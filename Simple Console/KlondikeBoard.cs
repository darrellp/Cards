using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using Cards;
using Spectre.Console;
using Klondike;
namespace Simple_Console;

public class KlondikeBoard
{
    #region Public variables
    internal bool Quit { get; private set; }
    internal bool NewGame { get; private set; }
    #endregion
    
    #region Private variables
    private const int BoardWidth = Klondike.Game.TabCount * 3 - 1;
    internal bool GameOver { get; private set; }
    #endregion
    
    #region constructor/UI loop
    public KlondikeBoard(int seed = 0)
    {
        var game = new Game(seed);
        var klayout = new Layout("Root")
            .SplitRows(
                new Layout("TopStacks").Size(1),
                new Layout("TopIndicators").Size(1),
                new Layout("BottomIndicators").Size(1),
                new Layout("Tableaux").Size(13),
                new Layout("Help").Size(1),
                new Layout("GameInfo").Size(1));
        
        var topStacksLayout = new Layout("TopStacks")
            .SplitColumns(
                new Layout("Foundations").Size(15),
                new Layout("Discard").Size(3),
                new Layout("Feed").Size(2));
        
        klayout["TopStacks"].Update(topStacksLayout);
        klayout["Help"].Update(new Markup("[yellow]Q[/] - Quit [yellow]N[/] - New Game [yellow]Space[/] - Next Move"));
        klayout["GameInfo"].Update(new Markup("[yellow]Game[/]"));
        
        var ai = new AI(game);

        AnsiConsole.Live(klayout)
            .Start(async ctx =>
            {
                // Gameplay loop
                while (true)
                {
                    char inChar;
                    var wonLost = "";
                    if (game.Won)
                    {
                        GameOver = true;
                        wonLost = "!!!  YOU WON  !!!";
                    }
                    else if (game.Lost)
                    {
                        GameOver = true;
                        wonLost = "Sorry - you lost!";
                    }
                    var gameInfo = $"[blue]Game: {game.Seed}  Move: {game.Moves}[/] [red]{wonLost}[/]";
                    klayout["GameInfo"].Update(new Markup(gameInfo));
                    var nextMove = new Move(StackId.None, StackId.None);
                    if (!GameOver)
                    {
                        nextMove = ai.GetNextMove();
                    }

                    var discardCard = game.Discards().TopCard;
                    var discardString = discardCard == Card.NullCard ? "[green]--[/]" : CardString(discardCard);
                    topStacksLayout["Discard"].Update(new Markup(discardString));
                    var feedString = game.Feed().Count == 0 ? "[green]--[/]" : "ST";
                    topStacksLayout["Feed"].Update(new Markup(feedString));

                    UpdateTableaux(klayout["Tableaux"], game);
                    UpdateFoundations(topStacksLayout["Foundations"], game);

                    var (topIndicator, botIndicator) = GetIndicatorStrings(nextMove);
                    var topLength = topIndicator.RemoveMarkup().Length;
                    var botLength = botIndicator.RemoveMarkup().Length;
                    Debug.Assert(topLength <= BoardWidth && botLength <= BoardWidth);
                    klayout["TopIndicators"].Update(new Markup(topIndicator));
                    klayout["BottomIndicators"].Update(new Markup(botIndicator));

                    ctx.Refresh();
                    inChar = Char.ToUpper(Console.ReadKey(true).KeyChar);
                    if (inChar == 'N')
                    {
                        NewGame = true;
                        break;
                    }

                    if (inChar == 'Q')
                    {
                        Quit = true;
                        break;
                    }

                    if (!GameOver && inChar == ' ')
                    {
                        game.MakeMove(nextMove);
                    }
                }
            });
    }
    #endregion

    #region Indicator strings
    static readonly string EmptyLine = new String(' ', BoardWidth - 1);
        
    private (string top, string bottom) GetIndicatorStrings(Move move)
    {
        var (indexSrc, isTopSrc) = GetIndicatorInfo(move.IdSrc);
        var (indexDst, isTopDst) = GetIndicatorInfo(move.IdDst);
        string top;
        string bottom;

        if (isTopSrc && isTopDst)
        {
            var indexMin = Math.Min(indexSrc, indexDst);
            var indexMax = Math.Max(indexSrc, indexDst);
            top = GetIndicatorString(indexMin, indexMax, indexSrc == indexMin);
            bottom = EmptyLine;
        }
        else if (!isTopSrc && !isTopDst)
        {
            var indexMin = Math.Min(indexSrc, indexDst);
            var indexMax = Math.Max(indexSrc, indexDst);
            bottom = GetIndicatorString(indexMin, indexMax, indexSrc == indexMin);
            top = EmptyLine;
        }
        else if (isTopSrc)
        {
            top = GetIndicatorString(indexSrc, -1, true);
            bottom = GetIndicatorString(indexDst, -1, false);
        }
        else
        {
            bottom = GetIndicatorString(indexSrc, -1, true);
            top = GetIndicatorString(indexDst, -1, false);
        }

        return (top, bottom);
    }

    private string GetIndicatorString(int iSmall, int iLarge, bool smallIsSrc)
    {
        if (iSmall < 0)
        {
            return EmptyLine;
        }
        StringBuilder sb = new();
        sb.Append(new string(' ', iSmall));
        string color = smallIsSrc ? "[green]" : "[red]";
        sb.Append($"{color}$[/]");
        if (iLarge > 0)
        {
            sb.Append(new string(' ', iLarge - iSmall - 1));
            color = smallIsSrc ? "[red]" : "[green]";
            sb.Append($"{color}$[/]");
        }

        return sb.ToString();
    }

    private (int index, bool isTop) GetIndicatorInfo(StackId id)
    {
        int index = -1;
        bool isTop = false;

        switch (id)
        {
            case >= StackId.Fnd1 and <= StackId.Fnd4:
                index = 3 * (id - StackId.Fnd1);
                isTop = true;
                break;
            
            case StackId.Waste:
                index = BoardWidth - 5;
                isTop = true;
                break;
            
            case StackId.Stock:
                index = BoardWidth - 2;
                isTop = true;
                break;
            
            case >= StackId.Tab1 and <= StackId.Tab7:
                index = 3 * (id - StackId.Tab1);
                break;
            
            default:
                break;
        }

        return (index, isTop);
    }
    #endregion
    
    #region Stack I/O
    private void UpdateFoundations(Layout layout, Game game)
    {
        var fndString = new StringBuilder();

        for (var i = 0; i < Klondike.Game.FndCount; i++)
        {
            var stack = game.Foundation(i);
            if (stack.Count == 0)
            {
                fndString.Append("[green]-- [/]");
            }
            else
            {
                fndString.Append(CardString(stack.TopCard) + " ");
            }
        }
        layout.Update(new Markup(fndString.ToString()));
    }

    private void UpdateTableaux(Layout tabLayout, Game game)
    {
        var tabString = new StringBuilder();

        for (var iLevel = 1; iLevel <= 13; iLevel++)
        {
            var nextLine = GetTabString(game, iLevel);
            if (nextLine == "")
            {
                break;
            }

            tabString.AppendLine(nextLine);
        }
        tabLayout.Update(new Markup(tabString.ToString()));
    }

    private string GetTabString(Game game, int iLevel)
    {
        // iLevel is one based
        var tabString = new StringBuilder();
        var foundCrd = false;
        for (var iTab = 0; iTab < Klondike.Game.TabCount; iTab++)
        {
            var stack = game.Tableau(iTab);
            if (iLevel == 1 && stack.Count == 0)
            {
                tabString.Append("[green]-- [/]");
            }
            else if (stack.CardsUp < iLevel)
            {
                tabString.Append("   ");
            }
            else
            {
                tabString.Append(CardString(stack[^(stack.CardsUp - iLevel + 1)]));
                tabString.Append(" ");
                foundCrd = true;
            }
        }
        return foundCrd ? tabString.ToString() : "";
    }

    private string CardString(Card card)
    {
        var colorString = card.IsBlack ? "Gray" : "Red";
        return $"[{colorString}]{card}[/]";
    }
    #endregion
}