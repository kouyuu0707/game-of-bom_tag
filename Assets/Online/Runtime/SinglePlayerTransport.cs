using System;
using Unity.Netcode;
using UnityEngine;

namespace BombTag.Online
{
    // Reuse the same host-authoritative game logic without opening any sockets.
    // NGO executes host-local RPCs directly; this transport never accepts remote peers.
    public class SinglePlayerTransport : NetworkTransport
    {
        public override ulong ServerClientId => Unity.Netcode.NetworkManager.ServerClientId;
        public override bool StartServer() => true;
        public override bool StartClient() => false;
        public override void Initialize(NetworkManager networkManager = null) { }
        public override void Shutdown() { }
        public override void DisconnectRemoteClient(ulong clientId) { }
        public override void DisconnectLocalClient() { }
        public override ulong GetCurrentRtt(ulong clientId) => 0;

        public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery)
        {
            if (clientId != ServerClientId)
                throw new InvalidOperationException("Single player does not support remote clients.");
        }

        public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            clientId = ServerClientId;
            payload = default;
            receiveTime = Time.realtimeSinceStartup;
            return NetworkEvent.Nothing;
        }
    }
}
