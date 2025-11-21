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

    public override void Start()
    {
        base.Start();
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

    public async void JoinRandomGame()
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
}
