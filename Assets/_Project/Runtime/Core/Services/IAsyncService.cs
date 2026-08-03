using System.Threading;
using System.Threading.Tasks;

namespace OneStep.Core.Services
{
    public interface IAsyncService
    {
        bool IsReady { get; }
        Task InitializeAsync(CancellationToken cancellationToken = default);
    }
}
