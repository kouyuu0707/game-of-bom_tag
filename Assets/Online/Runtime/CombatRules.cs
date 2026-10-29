using System;

namespace BombTag.Online
{
    public enum OnlineRoundPhase : byte { Waiting, Playing, Victory, Defeat }

    public static class CombatRules
    {
        public const int PlayerHealth = 3;
        public const float BlastRadius = 3f;
        public const double InvulnerabilitySeconds = 1;

        public static OnlineRoundPhase EvaluateRound(bool anyoneAlive, int enemiesRemaining)
        {
            // A final explosion that also wipes out the team is a defeat.
            if (!anyoneAlive)
                return OnlineRoundPhase.Defeat;
            return enemiesRemaining == 0 ? OnlineRoundPhase.Victory : OnlineRoundPhase.Playing;
        }
    }

    public sealed class HealthState
    {
        public int Current { get; private set; } = CombatRules.PlayerHealth;
        public double InvulnerableUntil { get; private set; } = double.NegativeInfinity;

        public bool TryDamage(int amount, double now)
        {
            if (amount <= 0 || Current == 0 || double.IsNaN(now) || double.IsInfinity(now) || now < InvulnerableUntil)
                return false;
            Current = Math.Max(0, Current - amount);
            InvulnerableUntil = now + CombatRules.InvulnerabilitySeconds;
            return true;
        }

        public void Reset()
        {
            Current = CombatRules.PlayerHealth;
            InvulnerableUntil = double.NegativeInfinity;
        }
    }
}
