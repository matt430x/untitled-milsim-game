namespace MilSim.Autoloads;

/// <summary>
/// Handles all multiplayer connectivity. Prototype phase uses Godot ENet (LAN/direct IP).
/// Post-prototype: replace transport layer with GodotSteam + Steam Datagram Relay.
/// All game logic RPCs are routed through here to keep networking concerns isolated.
/// </summary>
public partial class NetworkManager : Node
{
    public static NetworkManager Instance { get; private set; }

    public bool IsHost => Multiplayer.IsServer();
    public int LocalPlayerId => Multiplayer.GetUniqueId();

    public override void _Ready()
    {
        Instance = this;
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
    }

    public override void _ExitTree()
    {
        Multiplayer.PeerConnected -= OnPeerConnected;
        Multiplayer.PeerDisconnected -= OnPeerDisconnected;
    }

    public void HostGame(int port = 7777)
    {
        var peer = new ENetMultiplayerPeer();
        peer.CreateServer(port);
        Multiplayer.MultiplayerPeer = peer;
    }

    public void JoinGame(string address, int port = 7777)
    {
        var peer = new ENetMultiplayerPeer();
        peer.CreateClient(address, port);
        Multiplayer.MultiplayerPeer = peer;
    }

    public void Disconnect()
    {
        Multiplayer.MultiplayerPeer?.Close();
        Multiplayer.MultiplayerPeer = null;
    }

    private void OnPeerConnected(long id)
    {
        EventBus.RaisePlayerJoined((int)id);
    }

    private void OnPeerDisconnected(long id)
    {
        EventBus.RaisePlayerDisconnected((int)id);
    }
}
