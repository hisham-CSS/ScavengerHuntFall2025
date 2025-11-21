using UnityEngine;
using UnityEngine.UI;

public class LobbyUIManager : MonoBehaviour
{
    public static LobbyUIManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject lobbyListPanel;
    [SerializeField] private GameObject roomPanel;

    [Header("Main Menu")]
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button joinLobbyButton;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ShowMainMenu();

        createLobbyButton.onClick.AddListener(OnCreateLobbyClicked);
        joinLobbyButton.onClick.AddListener(OnJoinLobbyClicked);

        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnLobbyCreated += OnLobbyJoined; // Re-use OnLobbyJoined handler as it does the same thing (ShowRoom)
            LobbyManager.Instance.OnLobbyJoined += OnLobbyJoined;
            LobbyManager.Instance.OnLobbyLeft += OnLobbyLeft;
        }
    }

    private void OnDestroy()
    {
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnLobbyCreated -= OnLobbyJoined;
            LobbyManager.Instance.OnLobbyJoined -= OnLobbyJoined;
            LobbyManager.Instance.OnLobbyLeft -= OnLobbyLeft;
        }
    }

    private void OnLobbyJoined(Unity.Services.Lobbies.Models.Lobby lobby)
    {
        ShowRoom();
    }

    private void OnLobbyLeft()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        lobbyListPanel.SetActive(false);
        roomPanel.SetActive(false);
    }

    public void ShowLobbyList()
    {
        mainMenuPanel.SetActive(false);
        lobbyListPanel.SetActive(true);
        roomPanel.SetActive(false);
    }

    public void ShowRoom()
    {
        mainMenuPanel.SetActive(false);
        lobbyListPanel.SetActive(false);
        roomPanel.SetActive(true);
    }

    private bool isBusy = false;

    private void OnCreateLobbyClicked()
    {
        if (isBusy) return;
        isBusy = true;
        createLobbyButton.interactable = false;

        // For simplicity, we'll just start the host immediately with a default name
        // In a real app, you'd show a popup to enter name/settings
        ScavengerHuntNetworkManager networkManager = FindObjectOfType<ScavengerHuntNetworkManager>();
        if (networkManager != null)
        {
            networkManager.StartHostWithRelay();
            // ShowRoom(); // Handled by OnLobbyJoined event
        }
        
        // Reset busy state after a delay or when operation completes (simplified here)
        // In a robust implementation, you'd want callbacks for success/failure
        Invoke(nameof(ResetBusyState), 2f);
    }

    private void ResetBusyState()
    {
        isBusy = false;
        createLobbyButton.interactable = true;
    }

    private void OnJoinLobbyClicked()
    {
        ShowLobbyList();
    }
}
