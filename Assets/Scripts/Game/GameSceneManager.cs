using UnityEngine;
using Mirror;
using System.Collections;

public class GameSceneManager : NetworkBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private GameObject sharedOriginManagerObject;
    
    private ISharedOriginManager sharedOriginManager;

    private void Awake()
    {
        if (sharedOriginManagerObject != null)
        {
            sharedOriginManager = sharedOriginManagerObject.GetComponent<ISharedOriginManager>();
        }
        else
        {
            Debug.LogError("[GameSceneManager] SharedOriginManagerObject is not assigned!");
        }
    }

    private void Start()
    {
        if (sharedOriginManager != null)
        {
            sharedOriginManager.OnOriginSet += OnOriginSet;
            Debug.Log("[GameSceneManager] Waiting for Shared Origin...");
        }
    }

    private void OnDestroy()
    {
        if (sharedOriginManager != null)
        {
            sharedOriginManager.OnOriginSet -= OnOriginSet;
        }
    }

    private void OnOriginSet(Pose pose)
    {
        Debug.Log("[GameSceneManager] Shared Origin Established! Spawning Player...");
        
        // If we are the client, we tell the server we are ready to be spawned?
        // Or if we are using NetworkManager's auto-spawn, we might need to disable it and do it manually.
        // For now, let's assume we just log it.
        
        if (NetworkClient.active)
        {
            CmdPlayerReady();
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdPlayerReady(NetworkConnectionToClient sender = null)
    {
        Debug.Log($"[GameSceneManager] Player {sender.connectionId} is ready with AR Origin.");
        // Logic to spawn the player character or enable their interaction
    }
}
