using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScavengerHuntNetworkManager : NetworkManager
{
    [Header("UGS Settings")]
    [Header("UGS Settings")]
    [SerializeField] private string lobbyName = "ScavengerHuntLobby";
    [Scene] [SerializeField] private string gameScene = "GameScene";
    // spawnPrefabs is already defined in NetworkManager base class

    public override void Start()
    {
        base.Start();
        // spawnPrefabs are automatically registered by NetworkManager if assigned in Inspector
        // But if we modify the list at runtime (like in Editor script), we might need to ensure they are registered.
        // Actually, NetworkManager.Start() registers them. Since we call base.Start(), it should be fine.
        
        // Subscribe to events
        if (AuthenticationManager.Instance != null)
        {
            AuthenticationManager.Instance.OnSignedIn += OnUserSignedIn;
        }
    }

    private void OnUserSignedIn()
    {
        Debug.Log("User Signed In. Ready to Create or Join Game.");
        // Enable UI buttons or logic to start game
    }

    private bool isBusy = false;

    public async void StartHostWithRelay()
    {
        if (NetworkServer.active || isBusy)
        {
            Debug.LogWarning($"Host start blocked. Active: {NetworkServer.active}, Busy: {isBusy}");
            return;
        }
        isBusy = true;

        // 1. Create Relay
        string joinCode = await RelayManager.Instance.CreateRelay(maxConnections);
        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogError("Failed to create Relay.");
            isBusy = false;
            return;
        }
        Debug.Log($"[Host] Relay Created with Code: {joinCode}");

        // 2. Create Lobby with Join Code
        await LobbyManager.Instance.CreateLobby(lobbyName, maxConnections, joinCode);

        // 3. Start Mirror Host
        // Note: Transport configuration happens in RelayManager or here depending on Transport used
        StartHost();
        isBusy = false;
    }

    public async void JoinGameWithRelay(string lobbyId)
    {
        // 1. Join Lobby
        await LobbyManager.Instance.JoinLobbyById(lobbyId);

        // 2. Get Relay Join Code from Lobby Data
        string joinCode = LobbyManager.Instance.GetRelayJoinCode();
        Debug.Log($"[Client] Retrieved Relay Join Code from Lobby: '{joinCode}'");

        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogError("Could not find Relay Join Code in Lobby Data.");
            return;
        }

        // 3. Join Relay
        await RelayManager.Instance.JoinRelay(joinCode);

        // 4. Start Mirror Client
        StartClient();
    }

    public async void JoinRandomGame_Debug()
    {
        var lobbies = await LobbyManager.Instance.ListLobbies();
        if (lobbies.Count > 0)
        {
            Debug.Log($"Found {lobbies.Count} lobbies. Joining the first one: {lobbies[0].Name}");
            JoinGameWithRelay(lobbies[0].Id);
        }
        else
        {
            Debug.Log("No lobbies found.");
        }
    }
    // Player Data Persistence
    public Dictionary<int, string> playerNames = new Dictionary<int, string>();

    public void SetPlayerName(int connId, string name)
    {
        if (playerNames.ContainsKey(connId))
            playerNames[connId] = name;
        else
            playerNames.Add(connId, name);
            
        Debug.Log($"[NetworkManager] Stored name '{name}' for connection {connId}");
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // Manual instantiation to set SyncVars BEFORE spawning
        Transform startPos = GetStartPosition();
        GameObject player = startPos != null
            ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
            : Instantiate(playerPrefab);

        // Restore player name if it exists
        if (playerNames.TryGetValue(conn.connectionId, out string name))
        {
            var lobbyPlayer = player.GetComponent<ScavengerHuntLobbyPlayer>();
            if (lobbyPlayer != null)
            {
                lobbyPlayer.PlayerName = name;
                Debug.Log($"[NetworkManager] Restored name '{name}' for player {conn.connectionId} BEFORE spawn.");
            }
        }

        // Now spawn the player - SyncVars will be sent in the spawn message
        NetworkServer.AddPlayerForConnection(conn, player);
    }
}
