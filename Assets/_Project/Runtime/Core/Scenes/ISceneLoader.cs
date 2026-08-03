using System.Threading;
using System.Threading.Tasks;

namespace OneStep.Core.Scenes
{
    public interface ISceneLoader
    {
        Task LoadAsync(string sceneName, CancellationToken cancellationToken = default);
    }
}
