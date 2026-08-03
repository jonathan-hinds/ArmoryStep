using System.Threading;
using System.Threading.Tasks;

namespace OneStep.Networking
{
    public interface IHostMigrationStrategy
    {
        Task CaptureAsync(CancellationToken cancellationToken = default);
        Task RestoreAsync(CancellationToken cancellationToken = default);
    }
}
