using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System;
using Fusion.Sockets;

public class MainSpawner : MonoBehaviour ,INetworkRunnerCallbacks
{

    public NetworkObject playerPF;
    //public Transform mainAreaTransform;

    PlayerInputController playerInputController;

    private Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();


    public void OnConnectedToServer(NetworkRunner runner)
    {

        Debug.Log("Connected to server");
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        
    }

    public void OnDisconnectedFromServer(NetworkRunner runner)
    {
        Debug.Log("Disconnected from server");
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        
        if(playerInputController == null)
        {
            if (Player.localPlayer != null)
            {
                playerInputController = Player.localPlayer.GetComponent<PlayerInputController>();
            }
        }
       

        if(playerInputController != null)
        {
            input.Set(playerInputController.GetPlayerInputData());
        }
       

    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            NetworkObject playerChar = runner.Spawn(playerPF, Utils.GetNewSpawnPoint(50,50), Quaternion.identity, player);

            _spawnedPlayers.Add(player, playerChar);

        }



        Debug.Log($"Player {player.PlayerId} joined session");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedPlayers.TryGetValue(player, out NetworkObject playerObject))
        {
            runner.Despawn(playerObject);
            _spawnedPlayers.Remove(player);
        }


        Debug.Log($"Player {player.PlayerId} left session");
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)
    {
        
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log("Server Shutdown");
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        
    }

    
    
}
