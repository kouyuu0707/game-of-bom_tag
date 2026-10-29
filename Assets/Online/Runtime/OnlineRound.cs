using System.Collections.Generic;
using Unity.AI.Navigation;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace BombTag.Online
{
    [RequireComponent(typeof(NetworkObject))]
    public class OnlineRound : NetworkBehaviour
    {
        [SerializeField] private OnlineEnemy enemyPrefab;
        [SerializeField] private OnlineBombPickup pickupPrefab;
        [SerializeField] private Vector3[] pickupPositions =
        {
            new Vector3(-5, 0.5f, -4), new Vector3(-5, 0.5f, 0), new Vector3(-5, 0.5f, 4),
            new Vector3(5, 0.5f, -4), new Vector3(5, 0.5f, 0), new Vector3(5, 0.5f, 4),
            new Vector3(0, 0.5f, -5), new Vector3(0, 0.5f, 5)
        };
        [SerializeField] private Vector3[] enemyPositions = { new Vector3(8, 1, 8), new Vector3(-8, 1, 8) };
        private readonly NetworkVariable<OnlineRoundPhase> phase = new NetworkVariable<OnlineRoundPhase>(OnlineRoundPhase.Waiting);
        private readonly NetworkVariable<int> remaining = new NetworkVariable<int>(0);
        private readonly List<OnlineEnemy> enemies = new List<OnlineEnemy>();
        private readonly List<OnlineBombPickup> pickups = new List<OnlineBombPickup>();
        private readonly NetworkVariable<int> pickupsRemaining = new NetworkVariable<int>(0);
        public int PickupsRemaining => pickupsRemaining.Value;
        private readonly NetworkVariable<GameMode> mode = new NetworkVariable<GameMode>(GameMode.Cooperative);
        public GameMode Mode => mode.Value;
        public bool CanStartWithPlayers(int players) => GameModeRules.CanStartRound(Mode, players);
        public static OnlineRound Instance { get; private set; }
        public OnlineRoundPhase Phase => phase.Value;
        public bool IsPlaying => Phase == OnlineRoundPhase.Playing;
        public int EnemiesRemaining => remaining.Value;
        public string StartError { get; private set; }

        public override void OnNetworkSpawn() => Instance = this;
        public override void OnNetworkDespawn()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetModeOnServer(GameMode selectedMode)
        {
            if (IsSpawned && IsServer && !IsPlaying)
                mode.Value = selectedMode;
        }

        public void StartRoundOnServer()
        {
            if (!IsSpawned || !IsServer || IsPlaying || !CanStartWithPlayers(NetworkManager.ConnectedClientsIds.Count))
                return;
            StartError = null;
            foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
            {
                if (client.PlayerObject == null || client.PlayerObject.GetComponent<OnlineHealth>() == null ||
                    client.PlayerObject.GetComponent<OnlineBombInventory>() == null || client.PlayerObject.GetComponent<OnlinePlayer>() == null)
                {
                    StartError = "Wait for all players to finish loading, then try again.";
                    return;
                }
            }
            NavMeshSurface surface = FindFirstObjectByType<NavMeshSurface>();
            if (surface == null || enemyPrefab == null || pickupPrefab == null || enemyPositions == null || enemyPositions.Length == 0)
            {
                StartError = "Update OnlineTest using Tools / Bomb Tag / Create or Open Online Test.";
                return;
            }
            // The small flat arena is baked on the host; clients only receive enemy transforms.
            surface.BuildNavMesh();
            var spawnPoints = new List<Vector3>();
            foreach (Vector3 position in enemyPositions)
            {
                if (!NavMesh.SamplePosition(position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                {
                    StartError = "An enemy spawn is outside the navigation area. Check Enemy Positions.";
                    return;
                }
                spawnPoints.Add(hit.position + Vector3.up);
            }
            phase.Value = OnlineRoundPhase.Waiting;
            foreach (OnlineEnemy enemy in enemies)
            {
                if (enemy != null && enemy.IsSpawned)
                    enemy.NetworkObject.Despawn();
            }
            enemies.Clear();
            foreach (OnlineBombPickup pickup in pickups)
            {
                if (pickup != null && pickup.IsSpawned)
                    pickup.NetworkObject.Despawn();
            }
            pickups.Clear();
            int slot = 0;
            foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
            {
                if (client.PlayerObject == null)
                    continue;
                OnlineHealth health = client.PlayerObject.GetComponent<OnlineHealth>();
                OnlineBombInventory inventory = client.PlayerObject.GetComponent<OnlineBombInventory>();
                OnlinePlayer player = client.PlayerObject.GetComponent<OnlinePlayer>();
                health.ResetOnServer();
                inventory.ResetOnServer();
                player.ResetPositionOnServer(new Vector3(slot++ == 0 ? -2 : 2, 1, 0));
            }
            foreach (Vector3 position in spawnPoints)
            {
                OnlineEnemy enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
                enemy.NetworkObject.Spawn();
                enemies.Add(enemy);
            }
            foreach (Vector3 position in pickupPositions ?? System.Array.Empty<Vector3>())
            {
                OnlineBombPickup pickup = Instantiate(pickupPrefab, position, Quaternion.identity);
                pickup.NetworkObject.Spawn();
                pickups.Add(pickup);
            }
            pickupsRemaining.Value = pickups.Count;
            remaining.Value = enemies.Count;
            phase.Value = OnlineRoundPhase.Playing;
        }

        private void LateUpdate()
        {
            if (!IsSpawned || !IsServer || !IsPlaying)
                return;
            bool anyoneAlive = false;
            foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
            {
                if (client.PlayerObject != null && client.PlayerObject.TryGetComponent(out OnlineHealth health) && !health.IsDead)
                    anyoneAlive = true;
            }
            int aliveEnemies = 0;
            foreach (OnlineEnemy enemy in enemies)
            {
                if (enemy != null && enemy.IsSpawned && !enemy.IsDefeated)
                    aliveEnemies++;
            }
            int availablePickups = 0;
            foreach (OnlineBombPickup pickup in pickups)
            {
                if (pickup != null && pickup.IsSpawned && !pickup.IsCollected)
                    availablePickups++;
            }
            pickupsRemaining.Value = availablePickups;
            remaining.Value = aliveEnemies;
            phase.Value = CombatRules.EvaluateRound(anyoneAlive, aliveEnemies);
        }
    }
}
