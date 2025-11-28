using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Collections;

// Alias Unity's NetworkConnection to avoid conflict with Mirror's NetworkConnection
using UnityConnection = Unity.Networking.Transport.NetworkConnection;

public class MirrorUnityTransport : Transport
{
    public const string Scheme = "relay";

    private NetworkDriver driver;
    private NetworkPipeline pipeline;
    private UnityConnection clientConnection;
    private NativeList<UnityConnection> serverConnections;
    
    // Relay Data
    private RelayServerData relayServerData;
    private bool useRelay = false;

    [Header("Configuration")]
    public int MaxConnections = 100;

    private void OnDestroy()
    {
        if (driver.IsCreated)
        {
            driver.Dispose();
        }
        if (serverConnections.IsCreated)
        {
            serverConnections.Dispose();
        }
    }

    public void SetRelayData(string ip, ushort port, byte[] allocationId, byte[] key, byte[] connectionData, byte[] hostConnectionData = null, bool isSecure = false)
    {
        // Ensure no null arrays are passed to RelayServerData
        if (allocationId == null) allocationId = new byte[0];
        if (key == null) key = new byte[0];
        if (connectionData == null) connectionData = new byte[0];
        if (hostConnectionData == null) hostConnectionData = new byte[0];

        // Use the constructor that takes basic types directly
        // Correct order: allocationId, connectionData, hostConnectionData, key
        relayServerData = new RelayServerData(
            ip,
            port,
            allocationId,
            connectionData,
            hostConnectionData,
            key,
            isSecure
        );

        useRelay = true;
        Debug.Log($"[MirrorUnityTransport] Relay Data Set. Target: {ip}:{port}");
    }

    public override bool Available()
    {
        return Application.platform != RuntimePlatform.WebGLPlayer;
    }

    public override void ClientConnect(string address)
    {
        if (!useRelay)
        {
            Debug.LogError("[MirrorUnityTransport] ClientConnect called but Relay data not set. Direct connect not fully implemented.");
            return;
        }

        var settings = new NetworkSettings();
        settings.WithRelayParameters(ref relayServerData);

        driver = NetworkDriver.Create(settings);
        pipeline = driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));

        clientConnection = driver.Connect(NetworkEndpoint.AnyIpv4);
        Debug.Log("[MirrorUnityTransport] Client Connecting to Relay...");
    }

    public override bool ClientConnected()
    {
        return clientConnection.IsCreated && driver.GetConnectionState(clientConnection) == UnityConnection.State.Connected;
    }

    public override void ClientDisconnect()
    {
        if (driver.IsCreated && clientConnection.IsCreated)
        {
            clientConnection.Disconnect(driver);
            driver.ScheduleUpdate().Complete();
        }
        if (driver.IsCreated)
        {
            driver.Dispose();
        }
        useRelay = false;
    }

    public override void ClientSend(ArraySegment<byte> segment, int channelId = 0)
    {
        if (!clientConnection.IsCreated) return;

        int ret = driver.BeginSend(pipeline, clientConnection, out var writer);
        if (ret >= 0)
        {
            writer.WriteBytes(new NativeArray<byte>(segment.Array, Allocator.Temp).GetSubArray(segment.Offset, segment.Count));
            driver.EndSend(writer);
        }
    }

    public override void ServerStart()
    {
        if (!useRelay)
        {
            Debug.LogError("[MirrorUnityTransport] ServerStart called but Relay data not set.");
            return;
        }

        var settings = new NetworkSettings();
        settings.WithRelayParameters(ref relayServerData);

        driver = NetworkDriver.Create(settings);
        pipeline = driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
        serverConnections = new NativeList<UnityConnection>(MaxConnections, Allocator.Persistent);

        if (driver.Bind(NetworkEndpoint.AnyIpv4) != 0)
        {
            Debug.LogError("[MirrorUnityTransport] Server failed to bind.");
            return;
        }

        if (driver.Listen() != 0)
        {
            Debug.LogError("[MirrorUnityTransport] Server failed to listen.");
            return;
        }
        
        Debug.Log("[MirrorUnityTransport] Server Started on Relay.");
    }

    public override void ServerStop()
    {
        if (driver.IsCreated)
        {
            driver.Dispose();
        }
        if (serverConnections.IsCreated)
        {
            serverConnections.Dispose();
        }
        useRelay = false;
    }

    public override bool ServerActive()
    {
        return driver.IsCreated && serverConnections.IsCreated;
    }

    public override void ServerSend(int connectionId, ArraySegment<byte> segment, int channelId = 0)
    {
        // Mirror uses 1-based IDs for clients (0 is local player)
        int index = connectionId - 1;
        if (index < 0 || !serverConnections.IsCreated || index >= serverConnections.Length) return;
        
        UnityConnection conn = serverConnections[index];
        if (!conn.IsCreated) return;

        int ret = driver.BeginSend(pipeline, conn, out var writer);
        if (ret >= 0)
        {
            writer.WriteBytes(new NativeArray<byte>(segment.Array, Allocator.Temp).GetSubArray(segment.Offset, segment.Count));
            driver.EndSend(writer);
        }
    }

    public override void ServerDisconnect(int connectionId)
    {
        int index = connectionId - 1;
        if (index < 0 || !serverConnections.IsCreated || index >= serverConnections.Length) return;
        
        UnityConnection conn = serverConnections[index];
        if (conn.IsCreated)
        {
            conn.Disconnect(driver);
        }
    }

    public override string ServerGetClientAddress(int connectionId)
    {
        return "RelayClient";
    }

    public override Uri ServerUri()
    {
        return new Uri($"{Scheme}://relay");
    }

    public override int GetMaxPacketSize(int channelId = 0)
    {
        // Default MTU for UTP is usually around 1400, but let's be safe
        return 1200;
    }

    public override void Shutdown()
    {
        ClientDisconnect();
        ServerStop();
    }

    public new void Update()
    {
        if (!driver.IsCreated) return;

        driver.ScheduleUpdate().Complete();

        // Client Update
        if (clientConnection.IsCreated)
        {
            NetworkEvent.Type cmd;
            while ((cmd = clientConnection.PopEvent(driver, out var stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Connect)
                {
                    OnClientConnected.Invoke();
                }
                else if (cmd == NetworkEvent.Type.Data)
                {
                    byte[] data = new byte[stream.Length];
                    NativeArray<byte> nativeData = new NativeArray<byte>(data.Length, Allocator.Temp);
                    stream.ReadBytes(nativeData);
                    nativeData.CopyTo(data);
                    
                    OnClientDataReceived.Invoke(new ArraySegment<byte>(data), 0);
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    OnClientDisconnected.Invoke();
                    clientConnection = default;
                }
            }
        }

        // Server Update
        if (serverConnections.IsCreated)
        {
            // Accept new connections
            UnityConnection c;
            while ((c = driver.Accept()) != default(UnityConnection))
            {
                serverConnections.Add(c);
                // Mirror expects 1-based connection IDs
                // Use OnServerConnectedWithAddress instead of OnServerConnected
                OnServerConnectedWithAddress.Invoke(serverConnections.Length, "RelayClient"); 
            }

            // Process events for all connections
            for (int i = 0; i < serverConnections.Length; i++)
            {
                if (!serverConnections.IsCreated) break; // Safety check
                if (!serverConnections[i].IsCreated) continue;

                NetworkEvent.Type cmd;
                while ((cmd = driver.PopEventForConnection(serverConnections[i], out var stream)) != NetworkEvent.Type.Empty)
                {
                    if (cmd == NetworkEvent.Type.Data)
                    {
                        byte[] data = new byte[stream.Length];
                        NativeArray<byte> nativeData = new NativeArray<byte>(data.Length, Allocator.Temp);
                        stream.ReadBytes(nativeData);
                        nativeData.CopyTo(data);
                        
                        // Pass ID = index + 1
                        OnServerDataReceived.Invoke(i + 1, new ArraySegment<byte>(data), 0);
                    }
                    else if (cmd == NetworkEvent.Type.Disconnect)
                    {
                        // Pass ID = index + 1
                        OnServerDisconnected.Invoke(i + 1);
                        serverConnections[i] = default;
                    }
                }
            }
        }
    }
}
