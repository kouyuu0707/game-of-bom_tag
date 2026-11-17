using BombTag.Online;
using NUnit.Framework;

public class PickupRespawnStateTests
{
    [Test]
    public void StartsAvailable()
    {
        var item = new PickupRespawnState();
        Assert.That(item.IsAvailable, Is.True);
        Assert.That(item.Advance(100), Is.False);
    }

    [Test]
    public void ReappearsExactlyAfterTheConfiguredDelay()
    {
        var item = new PickupRespawnState();
        Assert.That(item.TryCollect(() => true, 100, 15), Is.True);
        Assert.That(item.IsAvailable, Is.False);
        Assert.That(item.Advance(114.999), Is.False);
        Assert.That(item.Advance(115), Is.True);
        Assert.That(item.IsAvailable, Is.True);
    }

    [Test]
    public void FullStockDoesNotHideTheItemOrStartCooldown()
    {
        var item = new PickupRespawnState();
        var stock = new BombStock();
        while (stock.TryCollect()) { }
        Assert.That(item.TryCollect(stock.TryCollect, 100, 15), Is.False);
        Assert.That(item.IsAvailable, Is.True);
        stock.TrySpend();
        Assert.That(item.TryCollect(stock.TryCollect, 101, 15), Is.True);
    }

    [Test]
    public void EachCycleOnlyGrantsOneBombAndDoesNotExtendTheTimer()
    {
        var item = new PickupRespawnState();
        var stock = new BombStock();
        Assert.That(item.TryCollect(stock.TryCollect, 0, 15), Is.True);
        Assert.That(item.TryCollect(stock.TryCollect, 10, 15), Is.False);
        Assert.That(stock.Count, Is.EqualTo(3));
        Assert.That(item.Advance(15), Is.True);
        Assert.That(item.TryCollect(stock.TryCollect, 15, 15), Is.True);
        Assert.That(stock.Count, Is.EqualTo(4));
        Assert.That(item.Advance(29.999), Is.False);
        Assert.That(item.Advance(30), Is.True);
    }

    [Test]
    public void RespawnedItemStaysAvailableIfThePlayerHasBecomeFull()
    {
        var item = new PickupRespawnState();
        var stock = new BombStock();
        item.TryCollect(stock.TryCollect, 0, 15);
        while (stock.TryCollect()) { }
        item.Advance(15);
        Assert.That(item.TryCollect(stock.TryCollect, 15, 15), Is.False);
        Assert.That(item.IsAvailable, Is.True);
    }

    [Test]
    public void NewRoundStateDoesNotInheritAnOldCooldown()
    {
        var previous = new PickupRespawnState();
        previous.TryCollect(() => true, 100, 15);
        var next = new PickupRespawnState();
        Assert.That(next.IsAvailable, Is.True);
        Assert.That(next.TryCollect(() => true, 101, 15), Is.True);
        Assert.That(previous.IsAvailable, Is.False);
    }

    [Test]
    public void InvalidClockOrDelayCannotGrantAnItem()
    {
        var item = new PickupRespawnState();
        int grants = 0;
        bool Grant() { grants++; return true; }
        Assert.That(item.TryCollect(Grant, double.NaN, 15), Is.False);
        Assert.That(item.TryCollect(Grant, 100, -1), Is.False);
        Assert.That(item.TryCollect(Grant, 100, double.PositiveInfinity), Is.False);
        Assert.That(grants, Is.Zero);
        Assert.That(item.IsAvailable, Is.True);
    }
}
