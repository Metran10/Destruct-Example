using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine.SceneManagement;

public class NetworkRunnerController : MonoBehaviour
{
    public NetworkRunner runnerPF;

    NetworkRunner networkRunner;


    void Start()
    {

        networkRunner = Instantiate(runnerPF);
        networkRunner.name = "Destruction NR";
        var clientTask = InitializeNetworkRunner(networkRunner, GameMode.AutoHostOrClient, NetAddress.Any(), SceneManager.GetActiveScene().buildIndex, null);

        Debug.Log("Runner has started");
    }

    private INetworkSceneManager GetSceneManagerForRunner(NetworkRunner runner)
    {
        var sceneProvider = runner.GetComponents(typeof(MonoBehaviour)).OfType<INetworkSceneManager>().FirstOrDefault();

        if (sceneProvider == null)
        {
            sceneProvider = runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
        }

        return sceneProvider;
    }

    protected virtual Task InitializeNetworkRunner(NetworkRunner runner, GameMode mode, NetAddress address, SceneRef scene, Action<NetworkRunner> initialized)
    {
        var sceneProvider = GetSceneManagerForRunner(runner);

        runner.ProvideInput = true;

        return runner.StartGame(new StartGameArgs
        {
            GameMode = mode,
            Scene = scene,
            SessionName = "Destruction Test",
            Initialized = initialized,
            Address = address,
            SceneManager = sceneProvider
        });

    }


}
