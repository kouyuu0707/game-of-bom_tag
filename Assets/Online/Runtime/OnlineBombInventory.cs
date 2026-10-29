using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace BombTag.Online
{
    [RequireComponent(typeof(NetworkObject))]
    public class OnlineBombInventory : NetworkBehaviour
    {
        [SerializeField] private OnlineBomb bombPrefab;
        private readonly NetworkVariable<int> bombCount = new NetworkVariable<int>(BombStock.StartingCount);
        private readonly NetworkVariable<int> placedCount = new NetworkVariable<int>(0);
        private readonly List<OnlineBomb> placedBombs = new List<OnlineBomb>();
        private BombStock stock = new BombStock();
        public int Count => bombCount.Value;
        public int PlacedCount => placedCount.Value;

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
                return;
            stock = new BombStock();
            placedBombs.Clear();
            bombCount.Value = stock.Count;
            placedCount.Value = 0;
        }

        public void RequestPlace()
        {
            if (IsSpawned && IsOwner)
                PlaceServerRpc();
        }

        public void RequestDetonate()
        {
            if (IsSpawned && IsOwner)
                DetonateServerRpc();
        }

        [ServerRpc]
        private void PlaceServerRpc()
        {
            // NGO restricts this RPC to this player's owner. Never accept a position from the client.
            if (bombPrefab == null || !stock.TrySpend())
                return;

            Vector3 position = transform.position + new Vector3(0, -0.7f, 0);
            OnlineBomb bomb = Instantiate(bombPrefab, position, Quaternion.identity);
            // Default NGO ownership cleanup removes these bombs if the planter disconnects.
            bomb.NetworkObject.SpawnWithOwnership(OwnerClientId);
            placedBombs.RemoveAll(item => item == null || !item.IsSpawned);
            placedBombs.Add(bomb);
            bombCount.Value = stock.Count;
            placedCount.Value = placedBombs.Count;
        }

        [ServerRpc]
        private void DetonateServerRpc()
        {
            OnlineBomb[] bombs = placedBombs.ToArray();
            placedBombs.Clear();
            placedCount.Value = 0;
            foreach (OnlineBomb bomb in bombs)
            {
                if (bomb == null || !bomb.IsSpawned || bomb.OwnerClientId != OwnerClientId)
                    continue;

                // Send the effect from the persistent player object, not the bomb about to despawn.
                ShowDetonationClientRpc(bomb.transform.position);
                bomb.NetworkObject.Despawn();
            }
            // Detonation does not replenish stock. Pickup synchronization is a later step.
        }

        // For future host-authoritative pickups. Clients cannot refill their own stock.
        public bool TryCollectOnServer()
        {
            if (!IsSpawned || !IsServer || !stock.TryCollect())
                return false;
            bombCount.Value = stock.Count;
            return true;
        }

        [ClientRpc]
        private void ShowDetonationClientRpc(Vector3 position)
        {
            GameObject effect = new GameObject("Online Bomb Detonation");
            LineRenderer ring = effect.AddComponent<LineRenderer>();
            ring.sharedMaterial = bombPrefab.GetComponent<Renderer>().sharedMaterial;
            var properties = new MaterialPropertyBlock();
            properties.SetColor("_BaseColor", new Color(1f, 0.45f, 0.1f));
            properties.SetColor("_Color", new Color(1f, 0.45f, 0.1f));
            ring.SetPropertyBlock(properties);
            ring.loop = true;
            ring.useWorldSpace = true;
            ring.widthMultiplier = 0.12f;
            ring.positionCount = 64;
            for (int i = 0; i < ring.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / ring.positionCount;
                ring.SetPosition(i, position + new Vector3(Mathf.Cos(angle) * 3f, 0.1f, Mathf.Sin(angle) * 3f));
            }
            Destroy(effect, 0.35f);
        }
    }
}
