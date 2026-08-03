using System;
using OneStep.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace OneStep.Bootstrap
{
    public sealed class MultiplayerConnectionTestPanel : MonoBehaviour
    {
        [SerializeField] private Text statusText;
        [SerializeField] private InputField joinCodeInput;
        [SerializeField] private Button initializeButton;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button leaveButton;

        private UnityMultiplayerDuelSessionService _service;

        public void Configure(Text status, InputField joinCode, Button initialize, Button host, Button join, Button leave)
        {
            statusText = status;
            joinCodeInput = joinCode;
            initializeButton = initialize;
            hostButton = host;
            joinButton = join;
            leaveButton = leave;
        }

        private void Start()
        {
            initializeButton?.onClick.AddListener(Initialize);
            hostButton?.onClick.AddListener(Host);
            joinButton?.onClick.AddListener(Join);
            leaveButton?.onClick.AddListener(Leave);
            TryBind();
            RefreshStatus();
        }

        private void OnDestroy()
        {
            if (_service != null)
            {
                _service.StateChanged -= OnStateChanged;
            }
        }

        private bool TryBind()
        {
            if (_service != null)
            {
                return true;
            }

            _service = AppBootstrap.Instance?.DuelSessionService;
            if (_service != null)
            {
                _service.StateChanged += OnStateChanged;
                return true;
            }

            return false;
        }

        private async void Initialize()
        {
            if (!TryBind())
            {
                SetStatus("Load this scene through Bootstrap to initialize services.");
                return;
            }

            try
            {
                await _service.InitializeAsync();
                RefreshStatus();
            }
            catch (Exception exception)
            {
                SetStatus($"INITIALIZE FAILED\n{exception.Message}");
            }
        }

        private async void Host()
        {
            if (!TryBind())
            {
                SetStatus("Bootstrap service is unavailable.");
                return;
            }

            try
            {
                var code = await _service.HostAsync();
                if (joinCodeInput != null)
                {
                    joinCodeInput.text = code;
                }

                RefreshStatus();
            }
            catch (Exception exception)
            {
                SetStatus($"HOST FAILED\n{exception.Message}");
            }
        }

        private async void Join()
        {
            if (!TryBind())
            {
                SetStatus("Bootstrap service is unavailable.");
                return;
            }

            try
            {
                await _service.JoinAsync(joinCodeInput?.text);
                RefreshStatus();
            }
            catch (Exception exception)
            {
                SetStatus($"JOIN FAILED\n{exception.Message}");
            }
        }

        private async void Leave()
        {
            if (!TryBind())
            {
                return;
            }

            try
            {
                await _service.LeaveAsync();
                RefreshStatus();
            }
            catch (Exception exception)
            {
                SetStatus($"LEAVE FAILED\n{exception.Message}");
            }
        }

        private void OnStateChanged(DuelSessionState _) => RefreshStatus();

        private void RefreshStatus()
        {
            if (AppBootstrap.Instance?.InitializationError is { } initializationError)
            {
                SetStatus($"UGS INITIALIZATION FAILED\n{initializationError.Message}");
                return;
            }

            if (!TryBind())
            {
                SetStatus("UGS / RELAY  Bootstrap required");
                return;
            }

            var playerId = AppBootstrap.Instance?.IdentityService.PlayerId;
            var shortId = string.IsNullOrEmpty(playerId) ? "not signed in" : playerId[..Math.Min(12, playerId.Length)];
            var code = string.IsNullOrEmpty(_service.JoinCode) ? "—" : _service.JoinCode;
            SetStatus($"UGS / RELAY  {_service.State}\nPLAYER  {shortId}\nJOIN CODE  {code}  |  WSS");
        }

        private void SetStatus(string value)
        {
            if (statusText != null)
            {
                statusText.text = value;
            }
        }
    }
}
