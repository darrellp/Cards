using Cards;

namespace CardsTests;

public class StackTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void TestToString()
    {
        var stack = new Stack([
            new Card(10, Suit.Club),
            new Card(5, Suit.Diamond)
        ]);
        Assert.That(stack.ToString(), Is.EqualTo("TC 5D"));
    }

    [Test]
    public void TestSplit()
    {
        var card1 = Card.CardFromString("TC"); ;
        var card2 = Card.CardFromString("5S"); ;
        var card3 = Card.CardFromString("AH"); ;
        var card4 = Card.CardFromString("2C");
        List<Card> lstCards = [card1, card2, card3, card4];
        var stack = new Stack(lstCards);

        var topCard = stack.Split(2);
        Assert.That(topCard.Count, Is.EqualTo(2));
        Assert.That(topCard[0], Is.EqualTo(card3));
        Assert.That(topCard[1], Is.EqualTo(card4));
        Assert.That(stack.Count, Is.EqualTo(2));
        Assert.That(stack[0], Is.EqualTo(card1));
        Assert.That(stack[1], Is.EqualTo(card2));
        stack.Merge(topCard);
        Assert.That(stack.Count, Is.EqualTo(4));
        Assert.That(topCard.Count, Is.EqualTo(0));
        for (var iCard = 0; iCard < stack.Count; iCard++)
        {
            Assert.That(stack[iCard], Is.EqualTo(lstCards[iCard]));
        }
        topCard = stack.Split(2);
        stack.Merge(topCard, 1);
        Assert.That(topCard.Count, Is.EqualTo(1));
        Assert.That(topCard[0], Is.EqualTo(card3));
        Assert.That(stack[0], Is.EqualTo(card1));
        Assert.That(stack[1], Is.EqualTo(card2));
        Assert.That(stack[2], Is.EqualTo(card4));
    }

    [Test]
    public void TestStackFromString()
    {
        var stack = Stack.SortedDeck();
        var stackString = stack.ToString();
        var newStack = Stack.Parse(stackString);
        Assert.That(newStack.Count, Is.EqualTo(stack.Count));
        for (var i = 0; i < stack.Count; i++)
        {
            Assert.That(newStack[i], Is.EqualTo(stack[i]));
        }
    }

    [Test]
    public void TestMixedStackFromString()
    {
        var stack = MixedStack.ParseMixed("|");
        Assert.That(stack.Count == 0);
        Assert.That(stack.CardsUp == 0);

        stack = MixedStack.ParseMixed("| TC");
        Assert.That(stack.Count == 1);
        Assert.That(stack[0] == new Card(10, Suit.Club));
        Assert.That(stack.CardsUp == 1);

        stack = MixedStack.ParseMixed("|TC");
        Assert.That(stack.Count == 1);
        Assert.That(stack[0] == new Card(10, Suit.Club));
        Assert.That(stack.CardsUp == 1);

        stack = MixedStack.ParseMixed("TC |");
        Assert.That(stack.Count == 1);
        Assert.That(stack[0] == new Card(10, Suit.Club));
        Assert.That(stack.CardsUp == 0);

        stack = MixedStack.ParseMixed("TC|");
        Assert.That(stack.Count == 1);
        Assert.That(stack.TopCard == new Card(10, Suit.Club));
        Assert.That(stack.CardsUp == 0);

        stack = MixedStack.ParseMixed("|TC AS 3C");
        Assert.That(stack.Count == 3);
        Assert.That(stack.TopCard == new Card(3, Suit.Club));
        Assert.That(stack.CardsUp == 3);

        stack = MixedStack.ParseMixed("TC | AS 3C");
        Assert.That(stack.Count == 3);
        Assert.That(stack.TopCard == new Card(3, Suit.Club));
        Assert.That(stack.CardsUp == 2);

        stack = MixedStack.ParseMixed("TC AS | 3C");
        Assert.That(stack.Count == 3);
        Assert.That(stack.TopCard == new Card(3, Suit.Club));
        Assert.That(stack.CardsUp == 1);

        stack = MixedStack.ParseMixed("TC AS 3C |");
        Assert.That(stack.Count == 3);
        Assert.That(stack.TopCard == new Card(3, Suit.Club));
        Assert.That(stack.CardsUp == 0);
    }

    [Test]
    public void TestMixedToString()
    {
        Assert.That(MixedStack.ParseMixed("|").ToString(), Is.EqualTo("|"));
        Assert.That(MixedStack.ParseMixed("|AC 2C").ToString(), Is.EqualTo("|AC 2C"));
        Assert.That(MixedStack.ParseMixed("AC | 2C").ToString(), Is.EqualTo("AC | 2C"));
        Assert.That(MixedStack.ParseMixed("AC 2C|").ToString(), Is.EqualTo("AC 2C|"));
    }
}