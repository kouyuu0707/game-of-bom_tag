using Unity.Netcode;
using UnityEngine;

namespace BombTag.Online
{
    [RequireComponent(typeof(NetworkObject))]
    public class OnlineBombPickup : NetworkBehaviour
    {
        [SerializeField, Min(0.1f)] private float pickupRadius = 0.9f;
        [SerializeField, Min(0.1f)] private float respawnSeconds = 15f;
        private readonly PickupRespawnState state = new PickupRespawnState();
        private readonly NetworkVariable<bool> available = new NetworkVariable<bool>(true);
        private Renderer body;
        public bool IsCollected => !available.Value;

        public override void OnNetworkSpawn()
        {
            body = GetComponent<Renderer>();
            var properties = new MaterialPropertyBlock();
            Color color = new Color(0.2f, 1f, 0.3f);
            properties.SetColor("_BaseColor", color);
            properties.SetColor("_Color", color);
            body.SetPropertyBlock(properties);
            available.OnValueChanged += SetVisibility;
            SetVisibility(true, available.Value);
        }

        public override void OnNetworkDespawn() => available.OnValueChanged -= SetVisibility;

        private void SetVisibility(bool previous, bool current)
        {
            if (body != null) body.enabled = current;
        }

        private void FixedUpdate()
        {
            if (!IsSpawned || !IsServer || OnlineRound.Instance == null || !OnlineRound.Instance.IsPlaying) return;
            double now = NetworkManager.ServerTime.Time;
            if (state.Advance(now)) available.Value = true;
            if (!state.IsAvailable) return;

            // Keep the same network object and position during cooldown. Only the host
            // grants stock and schedules reappearance; all peers receive visibility changes.
            Physics.SyncTransforms();
            Collider[] nearby = Physics.OverlapSphere(transform.position, pickupRadius,
                Physics.AllLayers, QueryTriggerInteraction.Ignore);
            foreach (Collider collider in nearby)
            {
                OnlineBombInventory inventory = collider.GetComponentInParent<OnlineBombInventory>();
                if (inventory == null || !ArenaVisibility.IsClear(transform.position, inventory.transform.position) ||
                    !state.TryCollect(inventory.TryCollectOnServer, now, Mathf.Max(0.1f, respawnSeconds))) continue;
                available.Value = false;
                break;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, pickupRadius);
        }
    }
}
