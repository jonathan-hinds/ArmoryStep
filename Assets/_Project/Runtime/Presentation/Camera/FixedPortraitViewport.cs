using OneStep.Core.Configuration;
using UnityEngine;
using UnityEngine.U2D;

namespace OneStep.Presentation.Camera
{
    [RequireComponent(typeof(UnityEngine.Camera), typeof(PixelPerfectCamera))]
    public sealed class FixedPortraitViewport : MonoBehaviour
    {
        [SerializeField] private ViewportConfiguration configuration;

        private UnityEngine.Camera _camera;
        private Vector2Int _lastScreen;

        public void Configure(ViewportConfiguration value) => configuration = value;

        private void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();
            Apply();
        }

        private void Update()
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            if (size != _lastScreen)
            {
                Apply();
            }
        }

        public void Apply()
        {
            if (configuration == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            _camera ??= GetComponent<UnityEngine.Camera>();
            _lastScreen = new Vector2Int(Screen.width, Screen.height);
            var windowAspect = (float)Screen.width / Screen.height;
            var targetAspect = configuration.TargetAspect;
            var rect = new Rect(0f, 0f, 1f, 1f);

            if (windowAspect > targetAspect)
            {
                var targetPixels = Mathf.FloorToInt(Screen.height * targetAspect);
                targetPixels -= targetPixels % 2;
                rect.width = (float)targetPixels / Screen.width;
                rect.x = (1f - rect.width) * 0.5f;
            }
            else
            {
                var targetPixels = Mathf.FloorToInt(Screen.width / targetAspect);
                targetPixels -= targetPixels % 2;
                rect.height = (float)targetPixels / Screen.height;
                rect.y = (1f - rect.height) * 0.5f;
            }

            _camera.rect = rect;
            _camera.orthographicSize = configuration.OrthographicSize;
            _camera.backgroundColor = configuration.LetterboxColor;
        }
    }
}
