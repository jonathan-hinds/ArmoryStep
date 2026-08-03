using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace OneStep.Platform
{
    public sealed class WebViewportMonitor : MonoBehaviour
    {
        public event Action ViewportChanged;

        private int _width;
        private int _height;
        private Rect _safeArea;
        private ScreenOrientation _orientation;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void OneStepRegisterViewportListener(string objectName);
#endif

        private void Awake()
        {
            gameObject.name = nameof(WebViewportMonitor);
            Capture();
#if UNITY_WEBGL && !UNITY_EDITOR
            OneStepRegisterViewportListener(gameObject.name);
#endif
        }

        private void Update()
        {
            if (_width == Screen.width && _height == Screen.height && _safeArea == Screen.safeArea &&
                _orientation == Screen.orientation)
            {
                return;
            }

            Capture();
            NotifyChanged();
        }

        public void OnBrowserViewportChanged(string _)
        {
            Capture();
            NotifyChanged();
        }

        private void Capture()
        {
            _width = Screen.width;
            _height = Screen.height;
            _safeArea = Screen.safeArea;
            _orientation = Screen.orientation;
        }

        private void NotifyChanged()
        {
            Canvas.ForceUpdateCanvases();
            ViewportChanged?.Invoke();
        }
    }
}
