using BombTag.Online;
using NUnit.Framework;

public class GameModeRulesTests
{
    [Test]
    public void SinglePlayerStartsWithExactlyOnePlayer()
    {
        Assert.That(GameModeRules.CanStartRound(GameMode.SinglePlayer, 1), Is.True);
        Assert.That(GameModeRules.CanStartRound(GameMode.SinglePlayer, 0), Is.False);
        Assert.That(GameModeRules.CanStartRound(GameMode.SinglePlayer, 2), Is.False);
    }

    [Test]
    public void CooperativeStillRequiresTwoPlayers()
    {
        Assert.That(GameModeRules.CanStartRound(GameMode.Cooperative, 2), Is.True);
        Assert.That(GameModeRules.CanStartRound(GameMode.Cooperative, 1), Is.False);
        Assert.That(GameModeRules.CanStartRound(GameMode.Cooperative, 0), Is.False);
        Assert.That(GameModeRules.CanStartRound(GameMode.Cooperative, 3), Is.False);
    }

    [Test]
    public void SinglePlayerRejectsRemoteParticipants()
    {
        Assert.That(GameModeRules.CanJoin(GameMode.SinglePlayer, true), Is.True);
        Assert.That(GameModeRules.CanJoin(GameMode.SinglePlayer, false), Is.False);
    }

    [Test]
    public void CooperativeAllowsHostAndGuest()
    {
        Assert.That(GameModeRules.CanJoin(GameMode.Cooperative, true), Is.True);
        Assert.That(GameModeRules.CanJoin(GameMode.Cooperative, false), Is.True);
    }
}
