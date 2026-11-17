using Unity.Netcode;
using UnityEngine;

namespace BombTag.Online
{
    [RequireComponent(typeof(NetworkObject))]
    public class OnlineHealth : NetworkBehaviour
    {
        private readonly NetworkVariable<int> current = new NetworkVariable<int>(CombatRules.PlayerHealth);
        private readonly NetworkVariable<double> invulnerableUntil = new NetworkVariable<double>(0);
        private readonly HealthState state = new HealthState();
        private Renderer body;
        private MaterialPropertyBlock properties;
        public int Current => current.Value;
        public bool IsDead => Current == 0;

        public override void OnNetworkSpawn()
        {
            body = GetComponent<Renderer>();
            properties = new MaterialPropertyBlock();
            if (IsServer)
                ResetOnServer();
        }

        public bool TakeDamageOnServer(int amount)
        {
            if (!IsSpawned || !IsServer || OnlineRound.Instance == null || !OnlineRound.Instance.IsPlaying ||
                !state.TryDamage(amount, NetworkManager.ServerTime.Time))
                return false;
            current.Value = state.Current;
            invulnerableUntil.Value = state.InvulnerableUntil;
            return true;
        }

        public void ResetOnServer()
        {
            if (!IsSpawned || !IsServer)
                return;
            state.Reset();
            current.Value = state.Current;
            invulnerableUntil.Value = 0;
        }

        private void Update()
        {
            if (!IsSpawned || body == null)
                return;
            Color color = OwnerClientId == Unity.Netcode.NetworkManager.ServerClientId
                ? new Color(0.2f, 0.65f, 1f) : new Color(1f, 0.65f, 0.15f);
            if (IsDead)
                color = Color.gray;
            else if (NetworkManager.ServerTime.Time < invulnerableUntil.Value && Time.frameCount % 12 < 6)
                color = Color.white;
            properties.SetColor("_BaseColor", color);
            properties.SetColor("_Color", color);
            body.SetPropertyBlock(properties);
        }
    }
}
