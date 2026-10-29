using Unity.Netcode;
using UnityEngine;

namespace BombTag.Online
{
    [RequireComponent(typeof(NetworkObject), typeof(CharacterController))]
    public class OnlinePlayer : NetworkBehaviour
    {
        [SerializeField, Min(0)] private float moveSpeed = 5f;
        private CharacterController character;
        private Vector2 movement;
        private float lastInputTime;
        private Camera followCamera;

        public override void OnNetworkSpawn()
        {
            character = GetComponent<CharacterController>();
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

        private void FixedUpdate()
        {
            if (!IsSpawned)
                return;
            if (IsOwner)
            {
                Vector2 input = Application.isFocused
                    ? new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"))
                    : Vector2.zero;
                SubmitMovementServerRpc(input);
            }
            if (!IsServer)
                return;

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
            movement = ConnectionRules.SanitizeMovement(input);
            lastInputTime = Time.unscaledTime;
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
