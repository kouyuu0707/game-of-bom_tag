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
        [SerializeField] private Vector3[] enemyPositions = { new Vector3(8, 1, 8), new Vector3(-8, 1, 8) };
        private readonly NetworkVariable<OnlineRoundPhase> phase = new NetworkVariable<OnlineRoundPhase>(OnlineRoundPhase.Waiting);
        private readonly NetworkVariable<int> remaining = new NetworkVariable<int>(0);
        private readonly List<OnlineEnemy> enemies = new List<OnlineEnemy>();
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

        public void StartRoundOnServer()
        {
            if (!IsSpawned || !IsServer || IsPlaying || NetworkManager.ConnectedClientsIds.Count < ConnectionRules.MaxPlayers)
                return;
            StartError = null;
            foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
            {
                if (client.PlayerObject == null || client.PlayerObject.GetComponent<OnlineHealth>() == null ||
                    client.PlayerObject.GetComponent<OnlineBombInventory>() == null || client.PlayerObject.GetComponent<OnlinePlayer>() == null)
                {
                    StartError = "Wait for both players to finish loading, then try again.";
                    return;
                }
            }
            NavMeshSurface surface = FindFirstObjectByType<NavMeshSurface>();
            if (surface == null || enemyPrefab == null || enemyPositions == null || enemyPositions.Length == 0)
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
            remaining.Value = aliveEnemies;
            phase.Value = CombatRules.EvaluateRound(anyoneAlive, aliveEnemies);
        }
    }
}
