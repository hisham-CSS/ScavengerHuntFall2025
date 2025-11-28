using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    public event Action<string> OnRelayCreated;
    public event Action<string> OnRelayJoined;

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

    public async Task<string> CreateRelay(int maxConnections)
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log($"Relay Created. Join Code: {joinCode}");
            OnRelayCreated?.Invoke(joinCode);

            // Pass allocation data to Transport
            var transport = FindFirstObjectByType<MirrorUnityTransport>();
            if (transport != null)
            {
                transport.SetRelayData(
                    allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port,
                    allocation.AllocationIdBytes,
                    allocation.Key,
                    allocation.ConnectionData,
                    null, // Host doesn't need HostConnectionData
                    false // Default to UDP (insecure) for now
                );
            }
            else
            {
                Debug.LogWarning("MirrorUnityTransport not found. Relay data not set on Transport.");
            }

            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
            return null;
        }
    }

    public async Task JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            Debug.Log($"Joined Relay with code: {joinCode}");
            OnRelayJoined?.Invoke(joinCode);

            // Pass join allocation data to Transport
            var transport = FindFirstObjectByType<MirrorUnityTransport>();
            if (transport != null)
            {
                transport.SetRelayData(
                    joinAllocation.RelayServer.IpV4,
                    (ushort)joinAllocation.RelayServer.Port,
                    joinAllocation.AllocationIdBytes,
                    joinAllocation.Key,
                    joinAllocation.ConnectionData,
                    joinAllocation.HostConnectionData,
                    false // Default to UDP (insecure) for now
                );
            }
            else
            {
                Debug.LogWarning("MirrorUnityTransport not found. Relay data not set on Transport.");
            }
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
        }
    }
}
