using System;
using System.Collections.Generic;
using AlienFarms.Core;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Server-side scene loading through NGO scene management: the host loads the gameplay scene (Single) and every
    /// client, including late joiners, follows on synchronization. Raises <see cref="LoadCompleted"/> once every client
    /// finished (or timed out).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkSceneLoader : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("NetworkManager on this root (Enable Scene Management on).")]
        private NetworkManager _manager;

        private NetworkSceneManager _subscribedSceneManager;

        /// <summary>True between a successful <see cref="LoadGameplay"/> and its <see cref="LoadCompleted"/>.</summary>
        public bool IsLoading { get; private set; }

        /// <summary>Server: a networked scene load finished on every client. Payload: scene name.</summary>
        public event Action<string> LoadCompleted;

        /// <summary>Server only: loads the gameplay scene for everyone. False when not the server or the load did not start.</summary>
        public bool LoadGameplay()
        {
            if (_manager == null || !_manager.IsServer || _manager.SceneManager == null)
            {
                Debug.LogWarning($"[{nameof(NetworkSceneLoader)}] Only a running server with scene management can load scenes.", this);
                return false;
            }

            // Same guarantees as SceneFlow: never carry a frozen time or paused audio into the next scene.
            Time.timeScale = 1f;
            AudioListener.pause = false;

            Subscribe(_manager.SceneManager);
            SceneEventProgressStatus status = _manager.SceneManager.LoadScene(SceneNames.Gameplay, LoadSceneMode.Single);
            if (status != SceneEventProgressStatus.Started)
            {
                Debug.LogError($"[{nameof(NetworkSceneLoader)}] Could not load '{SceneNames.Gameplay}': {status}.", this);
                return false;
            }

            IsLoading = true;
            return true;
        }

        private void OnDisable()
        {
            Unsubscribe();
            IsLoading = false;
        }

        private void Subscribe(NetworkSceneManager sceneManager)
        {
            if (_subscribedSceneManager == sceneManager)
            {
                return;
            }

            // A new session creates a new scene manager: move the subscription over.
            Unsubscribe();
            _subscribedSceneManager = sceneManager;
            _subscribedSceneManager.OnLoadEventCompleted += HandleLoadEventCompleted;
        }

        private void Unsubscribe()
        {
            if (_subscribedSceneManager != null)
            {
                _subscribedSceneManager.OnLoadEventCompleted -= HandleLoadEventCompleted;
                _subscribedSceneManager = null;
            }
        }

        private void HandleLoadEventCompleted(string sceneName, LoadSceneMode mode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            IsLoading = false;
            if (clientsTimedOut != null && clientsTimedOut.Count > 0)
            {
                Debug.LogWarning($"[{nameof(NetworkSceneLoader)}] {clientsTimedOut.Count} client(s) timed out loading '{sceneName}'.", this);
            }

            LoadCompleted?.Invoke(sceneName);
        }

        private void OnValidate()
        {
            if (_manager == null)
            {
                TryGetComponent(out _manager);
            }

            if (_manager != null && !_manager.NetworkConfig.EnableSceneManagement)
            {
                Debug.LogWarning($"[{nameof(NetworkSceneLoader)}] Enable Scene Management is off on '{_manager.name}'.", this);
            }
        }
    }
}
