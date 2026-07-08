using Cards;

namespace CardsTests;

public class ImageTests
{
    [Test]
    public void TestStreams()
    {
        Card[] cards =
        [
            Card.CardFromString("2H"),
            Card.CardFromString("KS")
        ];
        foreach (var card in cards)
        {
            try
            {
                using var stream = card.ImageStream();
                Assert.That(stream, Is.Not.Null);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }
    }
}
