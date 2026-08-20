using Cards;
using System.Diagnostics;

namespace GenericSol.Games.Golf;

// Only aim for AI at the moment is to get longest run.  Two equally long runs are considered equal.
// Improving on this would look at the cards that would be left behind and try to maximize the largest potential run
// based on any possible remaining cards.

internal class GolfAi : IAi
{
    public IGame Game { get; set; } = null!;
    GolfGame Golf;

    public IMove GetNextMove()
    {
        Golf = (GolfGame)Game;
        var fnd = Game.StackFromName("foundation");
        var target = fnd.TopCard;
        var tableaux = Golf.Tableaus().ToArray();
        return BestMove(target, tableaux);
    }

    IMove BestMove(Card target, MixedStack[] tableaus)
    {
        var tableauTops = tableaus.Select(t => t.Count - 1).ToArray();
        var (iBest, _) = BestIndexRec(target, tableaus, tableauTops, 0);
        var src = iBest >= 0 ? $"tab{iBest + 1}" : "stock";
        return new GenericMove(src, "foundation");
    }

    private (int index, int count) BestIndexRec(Card target, MixedStack[] tableaus, int[] tableauTops, int level)
    {
        var iRet = -1;
        var available = tableaus.Select((t, i) => t[tableauTops[i]]).ToArray();
        var maxPlays = 0;

        bool[] playable;
        if (target == Card.NullCard)
        {
            // null card means nothing on foundation so all cards are playable
            playable = Enumerable.Repeat(true, available.Count()).ToArray();
        }
        else
        {
            // If there's an actual target then we only want to consider cards that are playable on the target
            playable = available.Select(c => Golf.CheckPlayability(c, target)).ToArray();
        }

        if (!playable.Any(b => b))
        {
            return (-1, level);
        }
        else if (playable.Count(b => b) == 1 && level == 0)
        {
            // If this is the top level and there's only one move possible then
            // no need for recursion - just return that move.
            var index = playable.IndexOf(true);
            return (index, 0);
        }

        foreach (var (index, card) in available.Index())
        {
            if (playable[index])
            {
                tableauTops[index]--;
                var (thisIndex, max) = BestIndexRec(card, tableaus, tableauTops, level + 1);
                if (max > maxPlays)
                {
                    maxPlays = max;
                    iRet = index;
                }
                tableauTops[index]++;
            }
        }
        return (iRet, maxPlays);
    }
}
