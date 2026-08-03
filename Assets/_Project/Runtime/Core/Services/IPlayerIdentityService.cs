using System;

namespace OneStep.Core.Services
{
    public interface IPlayerIdentityService : IAsyncService
    {
        bool IsSignedIn { get; }
        string PlayerId { get; }
        event Action IdentityChanged;
    }
}
