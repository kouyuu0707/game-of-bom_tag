using System;
using BombTag.Online;
using NUnit.Framework;

public class PickupClaimTests
{
    [Test]
    public void OneItemCanOnlyIncreaseOnePlayersStock()
    {
        var pickup = new PickupClaim();
        var first = new BombStock();
        var second = new BombStock();
        Assert.That(pickup.TryClaim(first.TryCollect), Is.True);
        Assert.That(pickup.TryClaim(second.TryCollect), Is.False);
        Assert.That(first.Count, Is.EqualTo(3));
        Assert.That(second.Count, Is.EqualTo(2));
    }

    [Test]
    public void FullPlayerLeavesPickupAvailableAfterMakingRoom()
    {
        var pickup = new PickupClaim();
        var stock = new BombStock();
        while (stock.TryCollect()) { }
        Assert.That(pickup.TryClaim(stock.TryCollect), Is.False);
        Assert.That(pickup.IsClaimed, Is.False);
        stock.TrySpend();
        Assert.That(pickup.TryClaim(stock.TryCollect), Is.True);
        Assert.That(stock.Count, Is.EqualTo(5));
    }

    [Test]
    public void FullPlayersAttemptDoesNotBlockAnotherPlayer()
    {
        var pickup = new PickupClaim();
        var full = new BombStock();
        var other = new BombStock();
        while (full.TryCollect()) { }
        Assert.That(pickup.TryClaim(full.TryCollect), Is.False);
        Assert.That(pickup.TryClaim(other.TryCollect), Is.True);
        Assert.That(full.Count, Is.EqualTo(5));
        Assert.That(other.Count, Is.EqualTo(3));
    }

    [Test]
    public void TwoNearbyPickupsCannotOverfillOnePlayer()
    {
        var first = new PickupClaim();
        var second = new PickupClaim();
        var stock = new BombStock();
        stock.TryCollect();
        stock.TryCollect();
        Assert.That(first.TryClaim(stock.TryCollect), Is.True);
        Assert.That(second.TryClaim(stock.TryCollect), Is.False);
        Assert.That(stock.Count, Is.EqualTo(5));
        Assert.That(second.IsClaimed, Is.False);
    }

    [Test]
    public void ReentrantAttemptCannotGrantTwice()
    {
        var pickup = new PickupClaim();
        int granted = 0;
        Assert.That(pickup.TryClaim(() =>
        {
            Assert.That(pickup.TryClaim(() => { granted++; return true; }), Is.False);
            granted++;
            return true;
        }), Is.True);
        Assert.That(granted, Is.EqualTo(1));
    }

    [Test]
    public void FailedGrantReleasesReservation()
    {
        var pickup = new PickupClaim();
        Assert.Throws<InvalidOperationException>(() => pickup.TryClaim(() => throw new InvalidOperationException()));
        Assert.That(pickup.IsClaimed, Is.False);
        Assert.That(pickup.TryClaim(() => true), Is.True);
    }
}
