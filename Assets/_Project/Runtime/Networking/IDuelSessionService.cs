using System;
using System.Threading;
using System.Threading.Tasks;
using OneStep.Core.Services;

namespace OneStep.Networking
{
    public interface IDuelSessionService : IAsyncService
    {
        DuelSessionState State { get; }
        string JoinCode { get; }
        bool IsHost { get; }
        event Action<DuelSessionState> StateChanged;
        event Action<string> HostChanged;
        Task<string> HostAsync(CancellationToken cancellationToken = default);
        Task JoinAsync(string joinCode, CancellationToken cancellationToken = default);
        Task LeaveAsync(CancellationToken cancellationToken = default);
    }
}
