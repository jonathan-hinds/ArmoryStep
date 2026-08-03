using UnityEngine;

namespace OneStep.Platform
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            Apply();
        }

        private void Update()
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            if (_lastSafeArea != Screen.safeArea || _lastScreenSize != size)
            {
                Apply();
            }
        }

        public void Apply()
        {
            if (_rectTransform == null)
            {
                _rectTransform = (RectTransform)transform;
            }

            _lastSafeArea = Screen.safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            var min = _lastSafeArea.position;
            var max = _lastSafeArea.position + _lastSafeArea.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;
            _rectTransform.anchorMin = min;
            _rectTransform.anchorMax = max;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
        }
    }
}
