using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    public event Action<Lobby> OnLobbyCreated;
    public event Action<Lobby> OnLobbyJoined;
    public event Action OnLobbyLeft;

    private Lobby currentLobby;
    private float heartbeatTimer;
    private const float HEARTBEAT_INTERVAL = 15f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        HandleLobbyHeartbeat();
    }

    private async void HandleLobbyHeartbeat()
    {
        if (currentLobby != null && currentLobby.HostId == AuthenticationService.Instance.PlayerId)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer <= 0f)
            {
                float heartbeatTimerMax = 15f;
                heartbeatTimer = heartbeatTimerMax;

                try
                {
                    await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
                }
                catch (LobbyServiceException e)
                {
                    Debug.Log(e);
                }
            }
        }
    }

    public async Task CreateLobby(string lobbyName, int maxPlayers, string relayJoinCode, bool isPrivate = false)
    {
        try
        {
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
                }
            };

            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            Debug.Log($"Created Lobby: {currentLobby.Name} with Code: {currentLobby.LobbyCode}");
            OnLobbyCreated?.Invoke(currentLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public async Task JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions
            {
                Player = GetPlayer()
            };

            currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, options);
            Debug.Log($"Joined Lobby: {currentLobby.Name}");
            OnLobbyJoined?.Invoke(currentLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public async Task JoinLobbyById(string lobbyId)
    {
        try
        {
            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions
            {
                Player = GetPlayer()
            };

            currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);
            Debug.Log($"Joined Lobby: {currentLobby.Name}");
            OnLobbyJoined?.Invoke(currentLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    private float lastQueryTime = -10f;
    private const float QUERY_COOLDOWN = 1.5f; // 1.5s buffer for 1s limit
    private List<Lobby> cachedLobbies = new List<Lobby>();

    public async Task<List<Lobby>> ListLobbies()
    {
        if (Time.time - lastQueryTime < QUERY_COOLDOWN)
        {
            Debug.LogWarning($"[LobbyManager] Rate limit hit. Returning cached lobbies. Next query allowed in {QUERY_COOLDOWN - (Time.time - lastQueryTime):F1}s");
            return cachedLobbies;
        }

        try
        {
            lastQueryTime = Time.time;
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder>
                {
                    new QueryOrder(true, QueryOrder.FieldOptions.Created)
                }
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);
            cachedLobbies = response.Results;
            return response.Results;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
            return new List<Lobby>();
        }
    }

    public async Task LeaveLobby()
    {
        if (currentLobby != null)
        {
            try
            {
                if (currentLobby.HostId == AuthenticationService.Instance.PlayerId)
                {
                    await DeleteLobby();
                }
                else
                {
                    await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
                }
                currentLobby = null;
                OnLobbyLeft?.Invoke();
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError(e);
            }
        }
    }

    public async Task DeleteLobby()
    {
        if (currentLobby != null)
        {
            try
            {
                await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
                currentLobby = null;
                Debug.Log("Lobby Deleted.");
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError(e);
            }
        }
    }

    private void OnApplicationQuit()
    {
        if (currentLobby != null && currentLobby.HostId == AuthenticationService.Instance.PlayerId)
        {
            LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
        }
    }

    private Player GetPlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, AuthenticationService.Instance.PlayerId) }
            }
        };
    }

    public string GetRelayJoinCode()
    {
        if (currentLobby != null)
        {
            if (currentLobby.Data != null)
            {
                if (currentLobby.Data.ContainsKey("RelayJoinCode"))
                {
                    string code = currentLobby.Data["RelayJoinCode"].Value;
                    Debug.Log($"[LobbyManager] Found RelayJoinCode: {code}");
                    return code;
                }
                else
                {
                    Debug.LogWarning("[LobbyManager] Lobby Data does not contain 'RelayJoinCode'. Available keys: " + string.Join(", ", currentLobby.Data.Keys));
                }
            }
            else
            {
                Debug.LogWarning("[LobbyManager] Lobby Data is null.");
            }
        }
        else
        {
            Debug.LogWarning("[LobbyManager] currentLobby is null.");
        }
        return null;
    }
}
