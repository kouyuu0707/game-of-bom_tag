using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace BombTag.Online
{
    [RequireComponent(typeof(NetworkObject), typeof(CharacterController), typeof(OnlineBombInventory))]
    [RequireComponent(typeof(OnlineHealth))]
    public class OnlinePlayer : NetworkBehaviour
    {
        [SerializeField, Min(0)] private float moveSpeed = 5f;
        private CharacterController character;
        private Vector2 movement;
        private float lastInputTime;
        private Camera followCamera;
        private OnlineBombInventory inventory;
        private OnlineHealth health;
        public bool CanAct => IsSpawned && health != null && !health.IsDead &&
            OnlineRound.Instance != null && OnlineRound.Instance.IsPlaying;

        public override void OnNetworkSpawn()
        {
            character = GetComponent<CharacterController>();
            inventory = GetComponent<OnlineBombInventory>();
            health = GetComponent<OnlineHealth>();
            // Only the host moves the collider. NetworkTransform distributes its position.
            character.enabled = IsServer;
            if (IsOwner)
                followCamera = Camera.main;

            var properties = new MaterialPropertyBlock();
            Color color = OwnerClientId == Unity.Netcode.NetworkManager.ServerClientId
                ? new Color(0.2f, 0.65f, 1f) : new Color(1f, 0.65f, 0.15f);
            properties.SetColor("_BaseColor", color);
            properties.SetColor("_Color", color);
            GetComponent<Renderer>().SetPropertyBlock(properties);
        }

        private void Update()
        {
            if (!CanAct || !IsOwner || !Application.isFocused || inventory == null)
                return;
            if (Input.GetKeyDown(KeyCode.B))
                inventory.RequestPlace();
            if (Input.GetKeyDown(KeyCode.E))
                inventory.RequestDetonate();
        }

        private void FixedUpdate()
        {
            if (!IsSpawned)
                return;
            if (IsOwner)
            {
                Vector2 input = CanAct && Application.isFocused
                    ? new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"))
                    : Vector2.zero;
                SubmitMovementServerRpc(input);
            }
            if (!IsServer)
                return;

            character.enabled = health != null && !health.IsDead;
            if (!CanAct)
            {
                movement = Vector2.zero;
                return;
            }
            // A lost connection or lost packets must not leave a character walking forever.
            Vector2 current = Time.unscaledTime - lastInputTime <= 0.25f ? movement : Vector2.zero;
            Vector3 displacement = new Vector3(current.x, 0, current.y) * (moveSpeed * Time.fixedDeltaTime);
            Vector3 target = transform.position + displacement;
            target.x = Mathf.Clamp(target.x, -ConnectionRules.ArenaLimit, ConnectionRules.ArenaLimit);
            target.z = Mathf.Clamp(target.z, -ConnectionRules.ArenaLimit, ConnectionRules.ArenaLimit);
            character.Move(target - transform.position);
        }

        [ServerRpc(Delivery = RpcDelivery.Unreliable)]
        private void SubmitMovementServerRpc(Vector2 input)
        {
            // Ownership checks are enforced by NGO. Clients send input, never positions or speed.
            movement = CanAct ? ConnectionRules.SanitizeMovement(input) : Vector2.zero;
            lastInputTime = Time.unscaledTime;
        }

        public void ResetPositionOnServer(Vector3 position)
        {
            if (!IsSpawned || !IsServer)
                return;
            character.enabled = false;
            transform.position = position;
            GetComponent<NetworkTransform>().Teleport(position, Quaternion.identity, transform.localScale);
            character.enabled = true;
            movement = Vector2.zero;
        }

        private void LateUpdate()
        {
            if (!IsSpawned || !IsOwner || followCamera == null)
                return;
            followCamera.transform.position = transform.position + new Vector3(0, 12, -10);
            followCamera.transform.LookAt(transform.position);
        }
    }
}
