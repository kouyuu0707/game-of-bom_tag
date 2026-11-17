using BombTag.Online;
using NUnit.Framework;

public class BombStockTests
{
    [Test]
    public void EachPlayerStartsWithTwoIndependentBombs()
    {
        var first = new BombStock();
        var second = new BombStock();
        Assert.That(first.Count, Is.EqualTo(2));
        Assert.That(first.TrySpend(), Is.True);
        Assert.That(first.Count, Is.EqualTo(1));
        Assert.That(second.Count, Is.EqualTo(2));
    }

    [Test]
    public void EmptyStockCannotBeSpentOrBecomeNegative()
    {
        var stock = new BombStock();
        Assert.That(stock.TrySpend(), Is.True);
        Assert.That(stock.TrySpend(), Is.True);
        Assert.That(stock.TrySpend(), Is.False);
        Assert.That(stock.Count, Is.Zero);
    }

    [Test]
    public void CollectingStopsAtFive()
    {
        var stock = new BombStock();
        Assert.That(stock.TryCollect(), Is.True);
        Assert.That(stock.TryCollect(), Is.True);
        Assert.That(stock.TryCollect(), Is.True);
        Assert.That(stock.Count, Is.EqualTo(5));
        Assert.That(stock.TryCollect(), Is.False);
        Assert.That(stock.Count, Is.EqualTo(5));
    }

    [Test]
    public void SpendingFromFullStockMakesRoomForOnePickup()
    {
        var stock = new BombStock();
        while (stock.TryCollect()) { }
        Assert.That(stock.TrySpend(), Is.True);
        Assert.That(stock.Count, Is.EqualTo(4));
        Assert.That(stock.TryCollect(), Is.True);
        Assert.That(stock.Count, Is.EqualTo(5));
        Assert.That(stock.TryCollect(), Is.False);
    }
}
