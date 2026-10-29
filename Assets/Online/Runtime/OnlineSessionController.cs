using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace BombTag.Online
{
    public class OnlineSessionController : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        private readonly ConnectionSlots slots = new ConnectionSlots();
        private NetworkManager manager;
        private UnityTransport transport;
        private ISession session;
        private string code = "";
        private string status = "Choose Host or Join. WASD / arrows to move.";
        private bool busy;
        private bool disposed;
        private bool localOptions;
        private bool connecting;
        private float connectionDeadline;
        private bool stopping;
        private int attemptVersion;

        private void Awake()
        {
            Application.runInBackground = true;
            if (NetworkManager.Singleton != null)
            {
                status = "Another network session is already running. Stop it first.";
                enabled = false;
                return;
            }
            // Kept on this scene object so stopping Play Mode removes all networking state.
            transport = gameObject.AddComponent<UnityTransport>();
            manager = gameObject.AddComponent<NetworkManager>();
            manager.NetworkConfig ??= new NetworkConfig();
            manager.NetworkConfig.NetworkTransport = transport;
            manager.NetworkConfig.PlayerPrefab = playerPrefab;
            manager.NetworkConfig.EnableSceneManagement = false;
            manager.NetworkConfig.ConnectionApproval = true;
            manager.NetworkConfig.TickRate = 30;
            manager.ConnectionApprovalCallback = ApproveConnection;
            manager.OnClientConnectedCallback += Connected;
            manager.OnClientDisconnectCallback += Disconnected;
            manager.OnTransportFailure += TransportFailed;
            if (playerPrefab == null)
            {
                status = "Missing player prefab. Recreate the online test scene from Tools / Bomb Tag.";
                enabled = false;
            }
        }

        private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = slots.TryReserve(request.ClientNetworkId, out Vector3 position);
            response.CreatePlayerObject = response.Approved;
            response.Position = position;
            response.Rotation = Quaternion.identity;
            response.Reason = response.Approved ? "" : "This room is full (2 players).";
            response.Pending = false;
        }

        private void Connected(ulong clientId)
        {
            if (clientId == manager.LocalClientId)
                connecting = false;
            status = manager.IsHost ? "Hosting. Waiting for / connected to your friend." : "Connected to host.";
        }

        private void Disconnected(ulong clientId)
        {
            slots.Release(clientId);
            if (stopping || disposed)
                return;
            if (clientId == manager.LocalClientId)
            {
                string reason = manager.DisconnectReason;
                StopConnection(string.IsNullOrEmpty(reason)
                    ? "Disconnected. If the host leaves, start a new room."
                    : "Disconnected: " + reason);
            }
            else if (manager.IsHost)
                status = "Your friend disconnected. They can join again.";
        }

        private void TransportFailed() => StopConnection("Connection failed. Check your network and retry.");

        private void Update()
        {
            if (connecting && Time.unscaledTime > connectionDeadline)
                StopConnection("Connection timed out. Check that the host is running, then retry.");
        }

        private async Task InitializeServices()
        {
            if (string.IsNullOrWhiteSpace(Application.cloudProjectId))
                throw new InvalidOperationException("Link the project in Edit / Project Settings / Services first.");

            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                // Editor and build on one Windows PC need separate anonymous player identities.
                var options = new InitializationOptions();
                options.SetProfile(Application.isEditor ? "bombtag-editor" : "bombtag-player");
                await UnityServices.InitializeAsync(options);
            }
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        private async void StartOnline(bool host)
        {
            if (!CanStart())
                return;
            if (!host && string.IsNullOrWhiteSpace(code))
            {
                status = "Enter the host's join code.";
                return;
            }
            busy = true;
            int attempt = ++attemptVersion;
            slots.Clear();
            status = host ? "Creating room..." : "Joining room...";
            try
            {
                await InitializeServices();
                if (disposed || attempt != attemptVersion)
                    return;
                // MPS starts NGO and configures Relay; do not call StartHost/StartClient twice.
                ISession created = host
                    ? await MultiplayerService.Instance.CreateSessionAsync(
                        new SessionOptions { MaxPlayers = ConnectionRules.MaxPlayers, IsPrivate = true }
                            .WithRelayNetwork())
                    : await MultiplayerService.Instance.JoinSessionByCodeAsync(code.Trim().ToUpperInvariant(), new JoinSessionOptions());
                if (disposed || attempt != attemptVersion)
                {
                    await created.LeaveAsync();
                    return;
                }
                session = created;
                code = created.Code;
                status = host ? "Room ready. Share the join code with your friend." : "Room joined. Connecting...";
                connecting = !manager.IsConnectedClient;
                connectionDeadline = Time.unscaledTime + 30f;
            }
            catch (Exception exception)
            {
                if (!disposed && attempt == attemptVersion)
                {
                    manager.Shutdown();
                    slots.Clear();
                    status = "Could not connect: " + exception.Message;
                }
            }
            finally
            {
                if (!disposed && attempt == attemptVersion)
                    busy = false;
            }
        }

        private bool CanStart() => !disposed && !busy && !connecting && manager != null &&
            !manager.IsListening && !manager.ShutdownInProgress && playerPrefab != null;

        private void StartLocal(bool host)
        {
            if (!CanStart())
                return;
            slots.Clear();
            // Explicitly loopback only. Local testing does not expose a public server.
            transport.SetConnectionData("127.0.0.1", ConnectionRules.Port, "127.0.0.1");
            bool started = host ? manager.StartHost() : manager.StartClient();
            status = started ? "Connecting locally..." : "Could not start. Is port 7777 already in use?";
            connecting = started && !manager.IsConnectedClient;
            connectionDeadline = Time.unscaledTime + 15f;
        }

        private async void StopConnection(string message)
        {
            if (stopping || disposed)
                return;
            stopping = true;
            attemptVersion++;
            busy = true;
            connecting = false;
            ISession previous = session;
            session = null;
            try
            {
                if (manager != null)
                    manager.Shutdown();
                slots.Clear();
                if (previous != null)
                    await previous.LeaveAsync();
            }
            catch (Exception exception)
            {
                if (!disposed)
                    Debug.LogWarning("Room cleanup failed: " + exception.Message);
            }
            finally
            {
                if (!disposed)
                {
                    code = "";
                    status = message;
                    busy = false;
                    stopping = false;
                }
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, 16, 340, 380), GUI.skin.box);
            GUILayout.Label("Bomb Tag - 2 player connection test");
            GUILayout.Label("Movement only. Bombs and AI are not enabled here yet.");
            GUILayout.Space(8);
            GUILayout.Label(status);
            bool running = manager != null && manager.IsListening;
            GUI.enabled = CanStart();
            if (!running)
            {
                if (GUILayout.Button("Host online room"))
                    StartOnline(true);
                code = GUILayout.TextField(code, 12);
                if (GUILayout.Button("Join using code"))
                    StartOnline(false);
                localOptions = GUILayout.Toggle(localOptions, "Same-PC test (no cloud account required)");
                if (localOptions)
                {
                    if (GUILayout.Button("Local Host"))
                        StartLocal(true);
                    if (GUILayout.Button("Local Join"))
                        StartLocal(false);
                }
            }
            GUI.enabled = true;
            if (running && session != null)
            {
                GUILayout.Label("Join code: " + code);
                if (GUILayout.Button("Copy code"))
                    GUIUtility.systemCopyBuffer = code;
            }
            if (running && manager.IsServer)
                GUILayout.Label("Players: " + manager.ConnectedClientsIds.Count + "/2");
            GUI.enabled = !busy && (running || session != null || connecting);
            if (GUILayout.Button("Disconnect"))
                StopConnection("Disconnected. You can start another room.");
            GUI.enabled = true;
            GUILayout.EndArea();
        }

        private async void OnDestroy()
        {
            disposed = true;
            if (manager != null)
            {
                manager.OnClientConnectedCallback -= Connected;
                manager.OnClientDisconnectCallback -= Disconnected;
                manager.OnTransportFailure -= TransportFailed;
                manager.Shutdown();
            }
            ISession previous = session;
            session = null;
            if (previous != null)
            {
                try { await previous.LeaveAsync(); }
                catch (Exception) { /* Best effort while Unity is shutting down. */ }
            }
        }
    }
}
