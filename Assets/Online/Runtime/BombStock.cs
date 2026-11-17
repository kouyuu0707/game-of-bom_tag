namespace BombTag.Online
{
    // Pure inventory rules, separate from RPCs and scene objects.
    public sealed class BombStock
    {
        public const int StartingCount = 2;
        public const int Capacity = 5;
        public int Count { get; private set; } = StartingCount;

        public bool TrySpend()
        {
            if (Count == 0)
                return false;
            Count--;
            return true;
        }

        public bool TryCollect()
        {
            if (Count == Capacity)
                return false;
            Count++;
            return true;
        }
    }
}
