using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyListUI : MonoBehaviour
{
    [SerializeField] private Transform lobbyContainer;
    [SerializeField] private GameObject lobbyItemPrefab;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button backButton;

    private void Start()
    {
        refreshButton.onClick.AddListener(RefreshLobbyList);
        backButton.onClick.AddListener(() => LobbyUIManager.Instance.ShowMainMenu());
    }

    private void OnEnable()
    {
        RefreshLobbyList();
    }

    private float lastRefreshTime;
    private const float REFRESH_COOLDOWN = 2f;

    private async void RefreshLobbyList()
    {
        if (Time.time - lastRefreshTime < REFRESH_COOLDOWN) return;
        lastRefreshTime = Time.time;

        foreach (Transform child in lobbyContainer)
        {
            Destroy(child.gameObject);
        }

        List<Lobby> lobbies = await LobbyManager.Instance.ListLobbies();

        foreach (Lobby lobby in lobbies)
        {
            GameObject lobbyItem = Instantiate(lobbyItemPrefab, lobbyContainer);
            // Assuming the prefab has a Text component for the name and a Button to join
            Text lobbyNameText = lobbyItem.GetComponentInChildren<Text>();
            Button joinButton = lobbyItem.GetComponentInChildren<Button>();

            if (lobbyNameText != null)
            {
                lobbyNameText.text = $"{lobby.Name} ({lobby.Players.Count}/{lobby.MaxPlayers})";
            }

            if (joinButton != null)
            {
                joinButton.onClick.AddListener(() => JoinLobby(lobby.Id));
            }
        }
    }

    private void JoinLobby(string lobbyId)
    {
        ScavengerHuntNetworkManager networkManager = FindObjectOfType<ScavengerHuntNetworkManager>();
        if (networkManager != null)
        {
            networkManager.JoinGameWithRelay(lobbyId);
            // LobbyUIManager.Instance.ShowRoom(); // Handled by OnLobbyJoined event
        }
    }
}
