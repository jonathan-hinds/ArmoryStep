using System;
using System.Threading;
using System.Threading.Tasks;
using OneStep.Core.Configuration;
using OneStep.Core.Services;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace OneStep.Networking
{
    public sealed class UnityMultiplayerDuelSessionService : MonoBehaviour, IDuelSessionService
    {
        [SerializeField] private ServicesConfiguration configuration;
        [SerializeField] private MonoBehaviour identityProvider;

        private IPlayerIdentityService _identity;
        private ISession _session;

        public DuelSessionState State { get; private set; } = DuelSessionState.Offline;
        public string JoinCode => _session?.Code ?? string.Empty;
        public bool IsHost => _session?.IsHost ?? false;
        public bool IsReady => State is DuelSessionState.Ready or DuelSessionState.Connected;
        public event Action<DuelSessionState> StateChanged;
        public event Action<string> HostChanged;

        public void Configure(ServicesConfiguration servicesConfiguration, MonoBehaviour playerIdentity)
        {
            configuration = servicesConfiguration;
            identityProvider = playerIdentity;
            _identity = playerIdentity as IPlayerIdentityService;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (IsReady)
            {
                return;
            }

            SetState(DuelSessionState.Initializing);
            _identity ??= identityProvider as IPlayerIdentityService;
            if (_identity == null)
            {
                throw new InvalidOperationException("An IPlayerIdentityService provider is required.");
            }

            try
            {
                await _identity.InitializeAsync(cancellationToken);
                if (!_identity.IsSignedIn)
                {
                    throw new InvalidOperationException("Player authentication did not complete.");
                }

                SetState(DuelSessionState.Ready);
            }
            catch
            {
                SetState(DuelSessionState.Failed);
                throw;
            }
        }

        public async Task<string> HostAsync(CancellationToken cancellationToken = default)
        {
            await InitializeAsync(cancellationToken);
            await LeaveCurrentSessionIfNeeded();
            SetState(DuelSessionState.Connecting);

            try
            {
                var options = new SessionOptions
                {
                    MaxPlayers = configuration != null ? configuration.DuelCapacity : 2,
                    IsPrivate = true,
                    Name = $"duel-{_identity.PlayerId[..Math.Min(8, _identity.PlayerId.Length)]}"
                };
                options.WithRelayNetwork(new RelayNetworkOptions(preserveRegion: true));
                options.WithNetworkOptions(new NetworkOptions { RelayProtocol = RelayProtocol.WSS });
                Attach(await MultiplayerService.Instance.CreateSessionAsync(options));
                SetState(DuelSessionState.Connected);
                return JoinCode;
            }
            catch
            {
                SetState(DuelSessionState.Failed);
                throw;
            }
        }

        public async Task JoinAsync(string joinCode, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(joinCode))
            {
                throw new ArgumentException("A join code is required.", nameof(joinCode));
            }

            await InitializeAsync(cancellationToken);
            await LeaveCurrentSessionIfNeeded();
            SetState(DuelSessionState.Connecting);

            try
            {
                var options = new JoinSessionOptions();
                options.WithNetworkOptions(new NetworkOptions { RelayProtocol = RelayProtocol.WSS });
                Attach(await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode.Trim().ToUpperInvariant(), options));
                SetState(DuelSessionState.Connected);
            }
            catch
            {
                SetState(DuelSessionState.Failed);
                throw;
            }
        }

        public async Task LeaveAsync(CancellationToken cancellationToken = default)
        {
            if (_session == null)
            {
                SetState(_identity?.IsSignedIn == true ? DuelSessionState.Ready : DuelSessionState.Offline);
                return;
            }

            SetState(DuelSessionState.Disconnecting);
            cancellationToken.ThrowIfCancellationRequested();
            await _session.LeaveAsync();
            Detach();
            SetState(DuelSessionState.Ready);
        }

        private async Task LeaveCurrentSessionIfNeeded()
        {
            if (_session != null)
            {
                await LeaveAsync();
            }
        }

        private void Attach(ISession session)
        {
            Detach();
            _session = session;
            _session.StateChanged += OnUnitySessionStateChanged;
            _session.SessionHostChanged += OnHostChanged;
            _session.Deleted += OnSessionEnded;
            _session.RemovedFromSession += OnSessionEnded;
        }

        private void Detach()
        {
            if (_session == null)
            {
                return;
            }

            _session.StateChanged -= OnUnitySessionStateChanged;
            _session.SessionHostChanged -= OnHostChanged;
            _session.Deleted -= OnSessionEnded;
            _session.RemovedFromSession -= OnSessionEnded;
            _session = null;
        }

        private void OnUnitySessionStateChanged(SessionState state)
        {
            if (state is SessionState.Disconnected or SessionState.Deleted)
            {
                Detach();
                SetState(DuelSessionState.Ready);
            }
        }

        private void OnHostChanged(string playerId) => HostChanged?.Invoke(playerId);

        private void OnSessionEnded()
        {
            Detach();
            SetState(DuelSessionState.Ready);
        }

        private void SetState(DuelSessionState state)
        {
            if (State == state)
            {
                return;
            }

            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
