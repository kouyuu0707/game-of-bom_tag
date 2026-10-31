using UnityEngine;

namespace BombTag.Online
{
    public static class ArenaVisibility
    {
        // Ignore actors and the floor; only map walls obstruct attacks and pickup reach.
        public static bool IsClear(Vector3 from, Vector3 to) => FirstWallDistance(from, to) == float.PositiveInfinity;

        public static Vector3 ClipAtWall(Vector3 from, Vector3 to)
        {
            float distance = FirstWallDistance(from, to);
            return float.IsPositiveInfinity(distance) ? to :
                from + (to - from).normalized * Mathf.Max(0, distance - 0.02f);
        }

        private static float FirstWallDistance(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            if (direction.sqrMagnitude < 0.0001f) return float.PositiveInfinity;
            float nearest = float.PositiveInfinity;
            foreach (RaycastHit hit in Physics.RaycastAll(from, direction.normalized, direction.magnitude,
                Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<GeneratedWall>() != null)
                    nearest = Mathf.Min(nearest, hit.distance);
            }
            return nearest;
        }
    }
}
