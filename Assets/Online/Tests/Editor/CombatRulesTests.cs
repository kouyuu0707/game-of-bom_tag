using BombTag.Online;
using NUnit.Framework;

public class CombatRulesTests
{
    [Test]
    public void PlayerStartsWithThreeHealth()
    {
        Assert.That(new HealthState().Current, Is.EqualTo(3));
    }

    [Test]
    public void OneHitRemovesOneHealth()
    {
        var health = new HealthState();
        Assert.That(health.TryDamage(1, 10), Is.True);
        Assert.That(health.Current, Is.EqualTo(2));
    }

    [Test]
    public void MultipleHitsInsideOneSecondDoNotStack()
    {
        var health = new HealthState();
        health.TryDamage(1, 10);
        Assert.That(health.TryDamage(1, 10), Is.False);
        Assert.That(health.TryDamage(1, 10.999), Is.False);
        Assert.That(health.Current, Is.EqualTo(2));
        Assert.That(health.TryDamage(1, 11), Is.True);
        Assert.That(health.Current, Is.EqualTo(1));
    }

    [Test]
    public void HealthCannotBecomeNegativeOrReviveFromDamage()
    {
        var health = new HealthState();
        health.TryDamage(100, 10);
        Assert.That(health.Current, Is.Zero);
        Assert.That(health.TryDamage(1, 100), Is.False);
        Assert.That(health.Current, Is.Zero);
    }

    [Test]
    public void ResetRestoresHealthAndClearsOldInvulnerability()
    {
        var health = new HealthState();
        health.TryDamage(3, 10);
        health.Reset();
        Assert.That(health.Current, Is.EqualTo(3));
        Assert.That(health.TryDamage(1, 10.1), Is.True);
        Assert.That(health.Current, Is.EqualTo(2));
    }

    [Test]
    public void NonPositiveDamageDoesNotHealOrStartInvulnerability()
    {
        var health = new HealthState();
        Assert.That(health.TryDamage(0, 10), Is.False);
        Assert.That(health.TryDamage(-1, 10), Is.False);
        Assert.That(health.Current, Is.EqualTo(3));
        Assert.That(health.TryDamage(1, 10), Is.True);
    }

    [Test]
    public void InvalidTimeDoesNotPoisonHealthState()
    {
        var health = new HealthState();
        Assert.That(health.TryDamage(1, double.NaN), Is.False);
        Assert.That(health.TryDamage(1, double.PositiveInfinity), Is.False);
        Assert.That(health.TryDamage(1, double.NegativeInfinity), Is.False);
        Assert.That(health.TryDamage(1, 10), Is.True);
    }

    [Test]
    public void SurvivingTeammateKeepsTheRoundPlaying()
    {
        Assert.That(CombatRules.EvaluateRound(true, 1), Is.EqualTo(OnlineRoundPhase.Playing));
    }

    [Test]
    public void AllPlayersDownEndsTheRound()
    {
        Assert.That(CombatRules.EvaluateRound(false, 2), Is.EqualTo(OnlineRoundPhase.Defeat));
    }

    [Test]
    public void LastEnemyDefeatedWithSurvivorIsVictory()
    {
        Assert.That(CombatRules.EvaluateRound(true, 0), Is.EqualTo(OnlineRoundPhase.Victory));
    }

    [Test]
    public void SimultaneousTeamWipeAndLastEnemyIsDefeat()
    {
        Assert.That(CombatRules.EvaluateRound(false, 0), Is.EqualTo(OnlineRoundPhase.Defeat));
    }
}
