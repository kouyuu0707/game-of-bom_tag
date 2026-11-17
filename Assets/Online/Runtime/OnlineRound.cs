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
        private readonly NetworkVariable<OnlineRoundPhase> phase = new NetworkVariable<OnlineRoundPhase>(OnlineRoundPhase.Waiting);
        private readonly NetworkVariable<int> remaining = new NetworkVariable<int>(0);
        private readonly List<OnlineEnemy> enemies = new List<OnlineEnemy>();
        private readonly List<OnlineBombPickup> pickups = new List<OnlineBombPickup>();
        private readonly NetworkVariable<int> pickupsRemaining = new NetworkVariable<int>(0);
        private readonly NetworkVariable<GameMode> mode = new NetworkVariable<GameMode>(GameMode.Cooperative);
        private readonly NetworkVariable<int> mapSeed = new NetworkVariable<int>(0);
        private ProceduralArena arena;
        public int PickupsRemaining => pickupsRemaining.Value;
        public int MapSeed => mapSeed.Value;
        public GameMode Mode => mode.Value;
        public bool CanStartWithPlayers(int players) => GameModeRules.CanStartRound(Mode, players);
        public static OnlineRound Instance { get; private set; }
        public OnlineRoundPhase Phase => phase.Value;
        public bool IsPlaying => Phase == OnlineRoundPhase.Playing;
        public int EnemiesRemaining => remaining.Value;
        public string StartError { get; private set; }

        public override void OnNetworkSpawn()
        {
            Instance = this;
            arena = FindFirstObjectByType<ProceduralArena>();
            mapSeed.OnValueChanged += ApplyMap;
            if (MapSeed != 0) ApplyMap(0, MapSeed);
        }

        private void ApplyMap(int previous, int next)
        {
            if (arena != null && next != 0) arena.Generate(next);
        }

        public override void OnNetworkDespawn()
        {
            mapSeed.OnValueChanged -= ApplyMap;
            if (arena != null) arena.Clear();
            if (Instance == this) Instance = null;
        }

        public void SetModeOnServer(GameMode selectedMode)
        {
            if (IsSpawned && IsServer && !IsPlaying) mode.Value = selectedMode;
        }

        public void StartRoundOnServer(bool newMap = false)
        {
            if (!IsSpawned || !IsServer || IsPlaying || !CanStartWithPlayers(NetworkManager.ConnectedClientsIds.Count)) return;
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
            if (arena == null || enemyPrefab == null || pickupPrefab == null)
            {
                StartError = "Update OnlineTest using Tools / Bomb Tag / Create or Open Online Test.";
                return;
            }
            phase.Value = OnlineRoundPhase.Waiting;
            ClearActors();
            if (newMap || MapSeed == 0)
            {
                int next;
                do { next = Random.Range(1, int.MaxValue); } while (next == MapSeed);
                mapSeed.Value = next; // Every peer builds the same geometry from this seed.
            }
            arena.Generate(MapSeed);
            NavMeshSurface surface = arena.GetComponent<NavMeshSurface>();
            surface.RemoveData();
            surface.BuildNavMesh();
            Physics.SyncTransforms();
            GeneratedMap layout = arena.Layout;
            var playerSpawns = new List<Vector3>();
            var enemySpawns = new List<Vector3>();
            var pickupSpawns = new List<Vector3>();
            if (!NavMesh.SamplePosition(ProceduralArena.Position(layout.Players[0], 0), out NavMeshHit start, 0.6f, NavMesh.AllAreas))
            {
                StartError = "The starting plaza has no navigation. Check Ground and rebuild the scene setup.";
                return;
            }
            Vector3 origin = start.position;
            if (!FindSpawns(layout.Players, 1f, origin, playerSpawns) ||
                !FindSpawns(layout.Enemies, 1f, origin, enemySpawns) ||
                !FindSpawns(layout.Pickups, 0.5f, origin, pickupSpawns))
            {
                StartError = "Map navigation could not connect all spawns. Check Ground or try New map.";
                return;
            }
            int slot = 0;
            foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
            {
                client.PlayerObject.GetComponent<OnlineHealth>().ResetOnServer();
                client.PlayerObject.GetComponent<OnlinePlayer>().ResetPositionOnServer(playerSpawns[slot++]);
            }
            foreach (Vector3 position in enemySpawns)
            {
                OnlineEnemy enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
                enemy.NetworkObject.Spawn();
                enemies.Add(enemy);
            }
            foreach (Vector3 position in pickupSpawns)
            {
                OnlineBombPickup pickup = Instantiate(pickupPrefab, position, Quaternion.identity);
                pickup.NetworkObject.Spawn();
                pickups.Add(pickup);
            }
            pickupsRemaining.Value = pickups.Count;
            remaining.Value = enemies.Count;
            phase.Value = OnlineRoundPhase.Playing;
        }

        private static bool FindSpawns(IReadOnlyList<MapCell> cells, float height, Vector3 origin, List<Vector3> output)
        {
            var path = new NavMeshPath();
            foreach (MapCell cell in cells)
            {
                if (!NavMesh.SamplePosition(ProceduralArena.Position(cell, 0), out NavMeshHit hit, 0.6f, NavMesh.AllAreas) ||
                    ((hit.position - origin).sqrMagnitude > 0.001f &&
                        (!NavMesh.CalculatePath(origin, hit.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)))
                    return false;
                output.Add(hit.position + Vector3.up * height);
            }
            return true;
        }

        private void ClearActors()
        {
            foreach (OnlineEnemy enemy in enemies)
                if (enemy != null && enemy.IsSpawned) enemy.NetworkObject.Despawn();
            enemies.Clear();
            foreach (OnlineBombPickup pickup in pickups)
                if (pickup != null && pickup.IsSpawned) pickup.NetworkObject.Despawn();
            pickups.Clear();
            // Bombs must also go before replacing walls and rebuilding navigation.
            foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
                if (client.PlayerObject != null) client.PlayerObject.GetComponent<OnlineBombInventory>().ResetOnServer();
            remaining.Value = 0;
            pickupsRemaining.Value = 0;
        }

        private void LateUpdate()
        {
            if (!IsSpawned || !IsServer || !IsPlaying) return;
            bool anyoneAlive = false;
            foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
                if (client.PlayerObject != null && client.PlayerObject.TryGetComponent(out OnlineHealth health) && !health.IsDead)
                    anyoneAlive = true;
            int aliveEnemies = 0;
            foreach (OnlineEnemy enemy in enemies)
                if (enemy != null && enemy.IsSpawned && !enemy.IsDefeated) aliveEnemies++;
            int availablePickups = 0;
            foreach (OnlineBombPickup pickup in pickups)
                if (pickup != null && pickup.IsSpawned && !pickup.IsCollected) availablePickups++;
            pickupsRemaining.Value = availablePickups;
            remaining.Value = aliveEnemies;
            phase.Value = CombatRules.EvaluateRound(anyoneAlive, aliveEnemies);
        }
    }
}
