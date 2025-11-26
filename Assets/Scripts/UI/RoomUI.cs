using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoomUI : MonoBehaviour
{
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerListItemPrefab;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text readyButtonText;

    private List<ScavengerHuntLobbyPlayer> players = new List<ScavengerHuntLobbyPlayer>();

    private void Start()
    {
        readyButton.onClick.AddListener(OnReadyClicked);
        startButton.onClick.AddListener(OnStartClicked);
        startButton.gameObject.SetActive(false); // Hidden by default
    }

    private void OnEnable()
    {
        ScavengerHuntLobbyPlayer.OnPlayerSpawned += AddPlayer;
        ScavengerHuntLobbyPlayer.OnPlayerDespawned += RemovePlayer;
        ScavengerHuntLobbyPlayer.OnPlayerListUpdated += UpdateUI;

        UpdateUI();
    }

    private void OnDisable()
    {
        ScavengerHuntLobbyPlayer.OnPlayerSpawned -= AddPlayer;
        ScavengerHuntLobbyPlayer.OnPlayerDespawned -= RemovePlayer;
        ScavengerHuntLobbyPlayer.OnPlayerListUpdated -= UpdateUI;
    }

    private void AddPlayer(ScavengerHuntLobbyPlayer player)
    {
        Debug.Log($"RoomUI: AddPlayer called for {player.name} (isLocal: {player.isLocalPlayer})");
        if (!players.Contains(player))
        {
            players.Add(player);
            UpdateUI();
        }
    }

    private void RemovePlayer(ScavengerHuntLobbyPlayer player)
    {
        if (players.Contains(player))
        {
            players.Remove(player);
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        // Update Player List
        foreach (Transform child in playerListContainer)
        {
            Destroy(child.gameObject);
        }

        bool allReady = true;

        foreach (var player in players)
        {
            GameObject item = Instantiate(playerListItemPrefab, playerListContainer);
            // Try getting TMP_Text first, then legacy Text
            TMP_Text tmpText = item.GetComponentInChildren<TMP_Text>();
            Text legacyText = item.GetComponentInChildren<Text>();

            string status = player.IsReady ? "Ready" : "Not Ready";
            Color color = player.IsReady ? Color.green : Color.red;

            if (tmpText != null)
            {
                tmpText.text = $"{player.PlayerName} - {status}";
                tmpText.color = color;
            }
            else if (legacyText != null)
            {
                legacyText.text = $"{player.PlayerName} - {status}";
                legacyText.color = color;
            }

            if (!player.IsReady) allReady = false;
        }

        // Update Start Button (Host Only)
        if (NetworkServer.active && players.Count > 0)
        {
            startButton.gameObject.SetActive(allReady);
        }
        else
        {
            startButton.gameObject.SetActive(false);
        }

        // Update Ready Button Text
        var localPlayer = GetLocalPlayer();
        if (localPlayer != null)
        {
            readyButtonText.text = localPlayer.IsReady ? "Unready" : "Ready";
        }
    }

    private void OnReadyClicked()
    {
        Debug.Log("Ready Button Clicked");
        var localPlayer = GetLocalPlayer();
        if (localPlayer != null)
        {
            Debug.Log($"Setting Ready State to: {!localPlayer.IsReady}");
            localPlayer.CmdSetReady(!localPlayer.IsReady);
        }
        else
        {
            Debug.LogError("Local Player not found in RoomUI players list.");
        }
    }

    private void OnStartClicked()
    {
        // Logic to start the game
        // For example, change scene or spawn game objects
        Debug.Log("Host Started the Game!");
        ScavengerHuntNetworkManager.singleton.ServerChangeScene("GameScene"); // Example
    }

    private ScavengerHuntLobbyPlayer GetLocalPlayer()
    {
        foreach (var player in players)
        {
            if (player.isLocalPlayer) return player;
        }
        return null;
    }
}
