using Unity.Netcode;
using UnityEngine;

namespace BombTag.Online
{
    [RequireComponent(typeof(NetworkObject))]
    public class OnlineBombPickup : NetworkBehaviour
    {
        [SerializeField, Min(0.1f)] private float pickupRadius = 0.9f;
        private readonly PickupClaim claim = new PickupClaim();
        public bool IsCollected => claim.IsClaimed;

        public override void OnNetworkSpawn()
        {
            var properties = new MaterialPropertyBlock();
            Color color = new Color(0.2f, 1f, 0.3f);
            properties.SetColor("_BaseColor", color);
            properties.SetColor("_Color", color);
            GetComponent<Renderer>().SetPropertyBlock(properties);
        }

        private void FixedUpdate()
        {
            if (!IsSpawned || !IsServer || IsCollected || OnlineRound.Instance == null || !OnlineRound.Instance.IsPlaying)
                return;

            // Query the host's player colliders, not client-submitted pickup requests.
            // Recheck while nearby so a full player can place a bomb and then collect.
            Physics.SyncTransforms();
            Collider[] nearby = Physics.OverlapSphere(transform.position, pickupRadius,
                Physics.AllLayers, QueryTriggerInteraction.Ignore);
            foreach (Collider collider in nearby)
            {
                OnlineBombInventory inventory = collider.GetComponentInParent<OnlineBombInventory>();
                if (inventory == null || !claim.TryClaim(inventory.TryCollectOnServer))
                    continue;
                NetworkObject.Despawn();
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
