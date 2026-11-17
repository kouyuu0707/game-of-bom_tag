using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace BombTag.Online
{
    [RequireComponent(typeof(NetworkObject), typeof(NavMeshAgent))]
    public class OnlineEnemy : NetworkBehaviour
    {
        private NavMeshAgent agent;
        private bool defeated;
        public bool IsDefeated => defeated;

        public override void OnNetworkSpawn()
        {
            agent = GetComponent<NavMeshAgent>();
            // The prefab saves the agent disabled; clients never enable navigation.
            agent.enabled = IsServer;
            var properties = new MaterialPropertyBlock();
            properties.SetColor("_BaseColor", Color.red);
            properties.SetColor("_Color", Color.red);
            GetComponent<Renderer>().SetPropertyBlock(properties);
        }

        private void Update()
        {
            if (!IsSpawned || !IsServer || !agent.isOnNavMesh)
                return;
            OnlineRound round = OnlineRound.Instance;
            if (defeated || round == null || !round.IsPlaying)
            {
                agent.isStopped = true;
                return;
            }

            OnlineHealth target = null;
            float nearest = float.PositiveInfinity;
            foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
            {
                if (client.PlayerObject == null || !client.PlayerObject.TryGetComponent(out OnlineHealth health) || health.IsDead)
                    continue;
                float distance = (health.transform.position - transform.position).sqrMagnitude;
                if (distance < nearest)
                {
                    nearest = distance;
                    target = health;
                }
            }
            agent.isStopped = target == null;
            if (target == null)
                return;
            agent.SetDestination(target.transform.position);
            if (nearest <= 1.2f * 1.2f && ArenaVisibility.IsClear(transform.position, target.transform.position))
                target.TakeDamageOnServer(1);
        }

        public void DefeatOnServer()
        {
            if (!IsSpawned || !IsServer || defeated || OnlineRound.Instance == null || !OnlineRound.Instance.IsPlaying)
                return;
            defeated = true;
            if (agent.isOnNavMesh)
                agent.isStopped = true;
            NetworkObject.Despawn();
        }
    }
}
