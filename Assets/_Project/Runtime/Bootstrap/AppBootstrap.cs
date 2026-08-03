using System;
using System.Threading;
using OneStep.Core.Scenes;
using OneStep.Networking;
using OneStep.Services;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OneStep.Bootstrap
{
    [DefaultExecutionOrder(-1000)]
    public sealed class AppBootstrap : MonoBehaviour
    {
        [SerializeField] private UnityPlayerIdentityService identityService;
        [SerializeField] private UnityMultiplayerDuelSessionService duelSessionService;
        [SerializeField] private string startupScene = "FoundationTest";

        private CancellationTokenSource _lifetime;

        public static AppBootstrap Instance { get; private set; }
        public UnityPlayerIdentityService IdentityService => identityService;
        public UnityMultiplayerDuelSessionService DuelSessionService => duelSessionService;
        public Exception InitializationError { get; private set; }
        public bool IsInitialized { get; private set; }

        public void Configure(UnityPlayerIdentityService identity, UnityMultiplayerDuelSessionService duel, string sceneName)
        {
            identityService = identity;
            duelSessionService = duel;
            startupScene = sceneName;
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            _lifetime = new CancellationTokenSource();
        }

        private async void Start()
        {
            try
            {
                await identityService.InitializeAsync(_lifetime.Token);
                await duelSessionService.InitializeAsync(_lifetime.Token);
                IsInitialized = true;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception)
            {
                InitializationError = exception;
                Debug.LogException(exception, this);
            }

            if (SceneManager.GetActiveScene().name == "Bootstrap" && !string.IsNullOrWhiteSpace(startupScene))
            {
                try
                {
                    await new UnitySceneLoader().LoadAsync(startupScene, _lifetime.Token);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            _lifetime?.Cancel();
            _lifetime?.Dispose();
        }
    }
}
