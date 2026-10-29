using System;
using BombTag.Online;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;

public class SinglePlayerTransportTests
{
    [Test]
    public void OfflineTransportStartsOnlyHostAndHasNoIncomingEvents()
    {
        var owner = new GameObject("Offline transport test");
        try
        {
            var transport = owner.AddComponent<SinglePlayerTransport>();
            transport.Initialize();
            Assert.That(transport.StartServer(), Is.True);
            Assert.That(transport.StartClient(), Is.False);
            Assert.That(transport.PollEvent(out _, out _, out _), Is.EqualTo(NetworkEvent.Nothing));
            transport.Shutdown();
        }
        finally { UnityEngine.Object.DestroyImmediate(owner); }
    }

    [Test]
    public void OfflineTransportRejectsRemoteSends()
    {
        var owner = new GameObject("Offline transport test");
        try
        {
            var transport = owner.AddComponent<SinglePlayerTransport>();
            Assert.Throws<InvalidOperationException>(() =>
                transport.Send(transport.ServerClientId + 1, default, NetworkDelivery.Reliable));
        }
        finally { UnityEngine.Object.DestroyImmediate(owner); }
    }
}
