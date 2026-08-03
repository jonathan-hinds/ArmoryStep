using System.Threading;
using System.Threading.Tasks;

namespace OneStep.Core.Persistence
{
    public interface IPlayerDataStore
    {
        Task<string> LoadAsync(string slot, CancellationToken cancellationToken = default);
        Task SaveAsync(string slot, string serializedData, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string slot, CancellationToken cancellationToken = default);
    }
}
