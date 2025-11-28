using Mirror;
using System;
using UnityEngine;

public class ScavengerHuntLobbyPlayer : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnPlayerNameChanged))]
    public string PlayerName;

    [SyncVar(hook = nameof(OnIsReadyChanged))]
    public bool IsReady;

    public static event Action<ScavengerHuntLobbyPlayer> OnPlayerSpawned;
    public static event Action<ScavengerHuntLobbyPlayer> OnPlayerDespawned;
    public static event Action OnPlayerListUpdated;

    public override void OnStartClient()
    {
        OnPlayerSpawned?.Invoke(this);
        OnPlayerListUpdated?.Invoke();

        // If we are the local player and the name is empty (e.g. testing in GameScene directly), set a default name
        if (isLocalPlayer && string.IsNullOrEmpty(PlayerName))
        {
            CmdSetPlayerName($"Player {netId}");
        }
    }

    public override void OnStopClient()
    {
        OnPlayerDespawned?.Invoke(this);
        OnPlayerListUpdated?.Invoke();
    }

    [Command]
    public void CmdSetPlayerName(string name)
    {
        PlayerName = name;
        // Save to NetworkManager for persistence across scene changes
        if (NetworkManager.singleton is ScavengerHuntNetworkManager manager)
        {
            manager.SetPlayerName(connectionToClient.connectionId, name);
        }
    }

    [Command]
    public void CmdSetReady(bool ready)
    {
        IsReady = ready;
    }

    private void OnPlayerNameChanged(string oldName, string newName)
    {
        OnPlayerListUpdated?.Invoke();
    }

    private void OnIsReadyChanged(bool oldReady, bool newReady)
    {
        OnPlayerListUpdated?.Invoke();
    }
}
