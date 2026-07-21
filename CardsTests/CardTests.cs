using Cards;

namespace CardsTests;

public class CardTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void TestColors()
    {
        var card1 = Card.CardFromString("AD");
        var card2 = Card.CardFromString("AC");

        Assert.That(card1.IsRed);
        Assert.That(card2.IsBlack);
    }

    [Test]
    public void TestColorCompare()
    {
        var card1 = Card.CardFromString("AH");
        var card2 = Card.CardFromString("TH");
        var card3 = Card.CardFromString("3S"); ;
        Assert.That(card1.IsSameColor(card2));
        Assert.That(!card1.IsSameColor(card3));
    }

    [Test]
    public void TestKBelow()
    {
        var cardBelow = Card.CardFromString("5D");
        var legalCardAbove = Card.CardFromString("6S"); ;
        var illegalCardAbove1 = Card.CardFromString("7S"); ;
        var illegalCardAbove2 = Card.CardFromString("6H"); ;
        var illegalCardAbove3 = Card.CardFromString("7H"); ;
        Assert.That(cardBelow.IsKBelow(legalCardAbove));
        Assert.That(!cardBelow.IsKBelow(illegalCardAbove1));
        Assert.That(!cardBelow.IsKBelow(illegalCardAbove2));
        Assert.That(!cardBelow.IsKBelow(illegalCardAbove3));
    }

    [Test]
    public void TestCardFromString()
    {
        var deck = Stack.SortedDeck();

        for (int iCard = 0; iCard < deck.Count; iCard++)
        {
            var card = deck[iCard];
            var testCard = Card.CardFromString(card.ToString());
            Assert.That(testCard.Suit == card.Suit);
            Assert.That(testCard.Rank == card.Rank);
        }
    }
}