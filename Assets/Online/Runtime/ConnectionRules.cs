using System.Collections.Generic;
using UnityEngine;

namespace BombTag.Online
{
    public static class ConnectionRules
    {
        public const int MaxPlayers = 2;
        public const ushort Port = 7777;
        // Increment when replicated state or procedural map rules change.
        public const ushort ProtocolVersion = 3;
        public const float ArenaLimit = 23f;

        public static Vector2 SanitizeMovement(Vector2 input)
        {
            if (float.IsNaN(input.x) || float.IsInfinity(input.x) ||
                float.IsNaN(input.y) || float.IsInfinity(input.y))
                return Vector2.zero;
            return Vector2.ClampMagnitude(input, 1f);
        }
    }

    // Reserve slots during approval, including clients whose connection is still pending.
    public sealed class ConnectionSlots
    {
        private readonly Dictionary<ulong, int> slots = new Dictionary<ulong, int>();

        public bool TryReserve(ulong clientId, out Vector3 position)
        {
            if (!slots.TryGetValue(clientId, out int slot))
            {
                slot = -1;
                for (int candidate = 0; candidate < ConnectionRules.MaxPlayers; candidate++)
                {
                    if (!slots.ContainsValue(candidate))
                    {
                        slot = candidate;
                        break;
                    }
                }
                if (slot < 0)
                {
                    position = Vector3.zero;
                    return false;
                }
                slots.Add(clientId, slot);
            }
            position = new Vector3(slot == 0 ? -2f : 2f, 1f, 0f);
            return true;
        }

        public void Release(ulong clientId) => slots.Remove(clientId);
        public void Clear() => slots.Clear();
    }
}
