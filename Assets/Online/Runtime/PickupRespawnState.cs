using System;

namespace BombTag.Online
{
    public sealed class PickupRespawnState
    {
        private PickupClaim claim = new PickupClaim();
        private double readyAt = double.PositiveInfinity;
        public bool IsAvailable => !claim.IsClaimed;

        public bool TryCollect(Func<bool> grantItem, double now, double respawnSeconds)
        {
            if (double.IsNaN(now) || double.IsInfinity(now) || double.IsNaN(respawnSeconds) ||
                double.IsInfinity(respawnSeconds) || respawnSeconds < 0 || !claim.TryClaim(grantItem)) return false;
            readyAt = now + respawnSeconds;
            return true;
        }

        public bool Advance(double now)
        {
            if (IsAvailable || double.IsNaN(now) || double.IsInfinity(now) || now < readyAt) return false;
            claim = new PickupClaim();
            readyAt = double.PositiveInfinity;
            return true;
        }
    }
}
