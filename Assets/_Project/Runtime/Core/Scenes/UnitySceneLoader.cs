using System.Threading;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace OneStep.Core.Scenes
{
    public sealed class UnitySceneLoader : ISceneLoader
    {
        public async Task LoadAsync(string sceneName, CancellationToken cancellationToken = default)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (operation is { isDone: false })
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }
        }
    }
}
