// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Meta.Multiplayer.Core
{
    /// <summary>
    /// Handle scenes loading and keeps tracks of the current loaded scene and loading scenes through the NetCode
    /// NetworkManager.
    /// </summary>
    public class SceneLoader                                       // not a monobehavior
    {
        private static string s_currentScene = null;

        public bool SceneLoaded { get; private set; } = false;

        public SceneLoader() => SceneManager.sceneLoaded += OnSceneLoaded;         // SceneManager is a Unity class that has callbacks for Scene Loads   // when we construct this class, add a delegate to OnSceneLoaded
            

        ~SceneLoader()                              // destructor function
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)                // sets the Scene active once its been loaded, I guess that doesnt happen by default?????
        {
            SceneLoaded = true;
            s_currentScene = scene.name;
            _ = SceneManager.SetActiveScene(scene);
        }

        public void LoadScene(string scene, bool useNetManager = true)
        {
            Debug.Log($"LoadScene({scene}) (currentScene = {s_currentScene}, IsClient = {NetworkManager.Singleton.IsClient})");
            if (scene == s_currentScene) return;

            SceneLoaded = false;

            if (useNetManager && NetworkManager.Singleton.IsClient)           // seems like Levels that are expected to be Multiplayer should be loaded using NetworkManager scene loader
            {
                _ = NetworkManager.Singleton.SceneManager.LoadScene(scene, LoadSceneMode.Single);               // Clients load scene NOT asynchronously?? for some reason.    They use the NetworkManagers SceneManager singleton instead of the usual SceneManager??
                return;
            }

            _ = SceneManager.LoadSceneAsync(scene);          // Server (or maybe just offline player?) uses usually SceneManager
        }
    }
}