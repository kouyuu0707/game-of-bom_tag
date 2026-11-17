using System;

namespace BombTag.Online
{
    // Unity processes pickup attempts on its main thread. Reserve before granting stock,
    // so subsequent (including re-entrant) attempts cannot grant the same item twice.
    public sealed class PickupClaim
    {
        public bool IsClaimed { get; private set; }

        public bool TryClaim(Func<bool> grantItem)
        {
            if (IsClaimed)
                return false;
            IsClaimed = true;
            bool granted = false;
            try
            {
                granted = grantItem();
                return granted;
            }
            finally
            {
                if (!granted)
                    IsClaimed = false;
            }
        }
    }
}
