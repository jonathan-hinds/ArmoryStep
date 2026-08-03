using System;
using System.Threading;
using System.Threading.Tasks;
using OneStep.Core.Configuration;
using OneStep.Core.Services;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;

namespace OneStep.Services
{
    public sealed class UnityPlayerIdentityService : MonoBehaviour, IPlayerIdentityService
    {
        [SerializeField] private ServicesConfiguration configuration;

        public bool IsReady { get; private set; }
        public bool IsSignedIn => AuthenticationService.Instance.IsSignedIn;
        public string PlayerId => IsSignedIn ? AuthenticationService.Instance.PlayerId : string.Empty;
        public event Action IdentityChanged;

        public void Configure(ServicesConfiguration value) => configuration = value;

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (IsReady)
            {
                return;
            }

            if (configuration == null)
            {
                throw new InvalidOperationException("ServicesConfiguration is required.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            var options = new InitializationOptions();
            if (!string.IsNullOrWhiteSpace(configuration.EnvironmentName))
            {
                options.SetEnvironmentName(configuration.EnvironmentName.Trim());
            }

            await UnityServices.InitializeAsync(options);
            cancellationToken.ThrowIfCancellationRequested();

            if (configuration.SignInAnonymously && !AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            IsReady = true;
            IdentityChanged?.Invoke();
        }
    }
}
