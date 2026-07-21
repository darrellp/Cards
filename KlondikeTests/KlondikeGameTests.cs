using Cards;

namespace KlondikeTests;
using Klondike;


public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void ConstructorTest()
    {
        var test = new Game(Stack.SortedDeck());
        for (var i = 0; i < 7; i++)
        {
            Assert.That(test._tableau[i].Count, Is.EqualTo(i + 1));
        }

        for (var i = 0; i < 4; i++)
        {
            Assert.That(test._foundations[i].Count, Is.EqualTo(0));
        }
        Assert.That(test._waste.Count, Is.EqualTo(0));
        // 28 cards in the tableau (i.e., 7*8/2)
        Assert.That(test._stock.Count, Is.EqualTo(52 - 28));
    }

    private bool IsMove(Move move, StackId idSrc, StackId idDst, int cardCount)
    {
        return move.IdSrc == idSrc && move.IdDst == idDst && move.CardCount == cardCount;
    }

    [Test]
    public void SelectNextMoveTest()
    {
        var test = new Game(Stack.SortedDeck());
        test._tableau[0].Replace(0, "9S");
        test._foundations[0].Add(Card.CardFromString("8H"));
        // 8H should now go under the 9S we put on the first tableau enabling the 7S on the 4th tableau to make combo
        var ai = new AI(test);

        var select = ai.GetNextMove();
        // Fnd1 (8H) -> Tab1 (9S)
        Assert.That(select.IdSrc == StackId.Fnd1);
        Assert.That(select.IdDst == StackId.Tab1);
        Assert.That(select.CardCount == 1);
        test.MakeMove(select);
        Assert.That(test._foundations[0].Count == 0);
        Assert.That(test._tableau[0].Count == 2);
        Assert.That(test._tableau[0].CardsUp == 2);
        Assert.That(test._tableau[0].TopCard == Card.CardFromString("8H"));

        // This is the combo move for the previous move from foundations
        select = ai.GetNextMove();
        // Fnd4 (7S) -> Tab1 (8H)
        Assert.That(select.IdSrc == StackId.Tab4);
        Assert.That(select.IdDst == StackId.Tab1);
        Assert.That(select.CardCount == 1);
        test.MakeMove(select);
        Assert.That(test._tableau[3].Count == 3);
        Assert.That(test._tableau[3].CardsUp == 1);
        Assert.That(test._tableau[0].TopCard == Card.CardFromString("7S"));
        Assert.That(test._tableau[0].Count == 3);
        Assert.That(test._tableau[0].CardsUp == 3);

        select = ai.GetNextMove();
        Assert.That(select.IdSrc == StackId.Tab6);
        Assert.That(select.IdDst == StackId.Tab2);
        Assert.That(select.CardCount == 1);
        test.MakeMove(select);
        Assert.That(test._tableau[5].Count == 5);
        Assert.That(test._tableau[5].CardsUp == 1);
        Assert.That(test._tableau[5].Count == 5);
        Assert.That(test._tableau[1].Count == 3);
        Assert.That(test._tableau[1].TopCard == Card.CardFromString("JH"));
    }

    [Test]
    public void PlayGameTest()
    {
        var test = new Game(Stack.SortedDeck());
        Assert.That(test.PlayGame());           // We win from a sorted deck
        test = new Game(Stack.SortedDeck());
        test._stock._cards.RemoveAt(0);     // Remove an ace - can't win with one of the aces gone!
        Assert.That(!test.PlayGame());
    }

    [Test]
    public void PlaySpecificGames()
    {
        var test = new Game(850976309);
        test.PlayGameTo(76);
        //Assert.That(test.PlayGame());
    }
}