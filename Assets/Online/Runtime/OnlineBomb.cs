using Unity.Netcode;
using UnityEngine;

namespace BombTag.Online
{
    [RequireComponent(typeof(NetworkObject))]
    public class OnlineBomb : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            // Match the planter's avatar so ownership is visible to both players.
            var properties = new MaterialPropertyBlock();
            Color color = OwnerClientId == Unity.Netcode.NetworkManager.ServerClientId
                ? new Color(0.2f, 0.65f, 1f) : new Color(1f, 0.65f, 0.15f);
            properties.SetColor("_BaseColor", color);
            properties.SetColor("_Color", color);
            GetComponent<Renderer>().SetPropertyBlock(properties);
        }
    }
}
