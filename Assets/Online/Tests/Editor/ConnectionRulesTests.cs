using BombTag.Online;
using NUnit.Framework;
using UnityEngine;

public class ConnectionRulesTests
{
    [Test]
    public void TwoPlayersGetSeparateSpawnsAndThirdIsRejected()
    {
        var slots = new ConnectionSlots();
        Assert.That(slots.TryReserve(0, out Vector3 first), Is.True);
        Assert.That(slots.TryReserve(1, out Vector3 second), Is.True);
        Assert.That(first, Is.Not.EqualTo(second));
        Assert.That(slots.TryReserve(2, out _), Is.False);
    }

    [Test]
    public void RejoiningReusesVacantSlotWithoutOverlappingRemainingPlayer()
    {
        var slots = new ConnectionSlots();
        slots.TryReserve(0, out _);
        slots.TryReserve(1, out Vector3 remaining);
        slots.Release(0);
        Assert.That(slots.TryReserve(2, out Vector3 replacement), Is.True);
        Assert.That(replacement, Is.Not.EqualTo(remaining));
    }

    [Test]
    public void DuplicateApprovalDoesNotConsumeAnotherSlot()
    {
        var slots = new ConnectionSlots();
        slots.TryReserve(5, out Vector3 original);
        Assert.That(slots.TryReserve(5, out Vector3 again), Is.True);
        Assert.That(again, Is.EqualTo(original));
        Assert.That(slots.TryReserve(6, out _), Is.True);
    }

    [Test]
    public void ClearingRoomAllowsFreshHostAndGuest()
    {
        var slots = new ConnectionSlots();
        slots.TryReserve(0, out _);
        slots.TryReserve(1, out _);
        slots.Clear();
        Assert.That(slots.TryReserve(10, out _), Is.True);
        Assert.That(slots.TryReserve(11, out _), Is.True);
    }

    [Test]
    public void DiagonalAndOversizedInputsCannotIncreaseSpeed()
    {
        Assert.That(ConnectionRules.SanitizeMovement(new Vector2(1, 1)).magnitude,
            Is.EqualTo(1f).Within(0.0001f));
        Assert.That(ConnectionRules.SanitizeMovement(new Vector2(100, 100)).magnitude,
            Is.EqualTo(1f).Within(0.0001f));
        Assert.That(ConnectionRules.SanitizeMovement(new Vector2(0.3f, 0)), Is.EqualTo(new Vector2(0.3f, 0)));
    }

    [Test]
    public void NonFiniteInputsAreIgnored()
    {
        Assert.That(ConnectionRules.SanitizeMovement(new Vector2(float.NaN, 1)), Is.EqualTo(Vector2.zero));
        Assert.That(ConnectionRules.SanitizeMovement(new Vector2(1, float.PositiveInfinity)), Is.EqualTo(Vector2.zero));
        Assert.That(ConnectionRules.SanitizeMovement(new Vector2(float.NegativeInfinity, 0)), Is.EqualTo(Vector2.zero));
    }
}
