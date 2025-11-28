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

    [Header("Spawning")]
    [SerializeField] private GameObject arMarkerPrefab; // Assign NetworkedCube prefab here

    private void OnOriginSet(Pose pose)
    {
        Debug.Log("[GameSceneManager] Shared Origin Established! Spawning Player...");
        
        if (NetworkClient.active)
        {
            CmdPlayerReady();
        }

        // Host Logic: Spawn the AR Marker at (0,0,0) to prove the origin is set
        if (NetworkServer.active && arMarkerPrefab != null)
        {
            Debug.Log("[GameSceneManager] Host spawning AR Marker at (0,0,0)");
            GameObject marker = Instantiate(arMarkerPrefab, Vector3.zero, Quaternion.identity);
            NetworkServer.Spawn(marker);
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdPlayerReady(NetworkConnectionToClient sender = null)
    {
        Debug.Log($"[GameSceneManager] Player {sender.connectionId} is ready with AR Origin.");
        // Logic to spawn the player character or enable their interaction
    }
}
