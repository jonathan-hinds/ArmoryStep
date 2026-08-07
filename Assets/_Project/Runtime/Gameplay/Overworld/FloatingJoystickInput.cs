using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OneStep.Gameplay.Overworld
{
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public sealed class FloatingJoystickInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private AdventureConfiguration _configuration;
        private RectTransform _surface;
        private RectTransform _ring;
        private RectTransform _knob;
        private Vector2 _originScreen;
        private Vector2Int _direction;
        private int _pointerId = int.MinValue;
        private bool _active;
        private bool _meaningfulDrag;
        private float _nextRepeatTime;

        public event Action<Vector2Int> ActionRequested;

        public bool InputEnabled { get; set; }

        public void Configure(AdventureConfiguration configuration, RectTransform ring, RectTransform knob)
        {
            _configuration = configuration;
            _surface = GetComponent<RectTransform>();
            _ring = ring;
            _knob = knob;
            Hide();
        }

        private void Update()
        {
            if (!_active || _direction == Vector2Int.zero || !InputEnabled || Time.unscaledTime < _nextRepeatTime)
            {
                return;
            }

            ActionRequested?.Invoke(_direction);
            _nextRepeatTime = Time.unscaledTime + _configuration.JoystickRepeatRate;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!InputEnabled || _active || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            _active = true;
            _meaningfulDrag = false;
            _pointerId = eventData.pointerId;
            _originScreen = eventData.position;
            _direction = Vector2Int.zero;
            PositionVisual(_ring, eventData.position, eventData.pressEventCamera);
            _ring.gameObject.SetActive(true);
            _knob.anchoredPosition = Vector2.zero;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_active || eventData.pointerId != _pointerId)
            {
                return;
            }

            var delta = eventData.position - _originScreen;
            var threshold = Mathf.Min(Screen.width, Screen.height) * _configuration.JoystickThreshold;
            var nextDirection = delta.magnitude < threshold ? Vector2Int.zero : Quantize(delta);
            var maxVisualDistance = Mathf.Max(threshold * 1.35f, 36f);
            var visualDelta = Vector2.ClampMagnitude(delta, maxVisualDistance);
            _knob.anchoredPosition = ScreenDeltaToLocal(visualDelta);

            if (nextDirection == Vector2Int.zero)
            {
                _direction = Vector2Int.zero;
                return;
            }

            _meaningfulDrag = true;
            if (nextDirection == _direction)
            {
                return;
            }

            _direction = nextDirection;
            ActionRequested?.Invoke(_direction);
            _nextRepeatTime = Time.unscaledTime + _configuration.JoystickInitialRepeatDelay;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_active || eventData.pointerId != _pointerId)
            {
                return;
            }

            if (InputEnabled && !_meaningfulDrag)
            {
                ActionRequested?.Invoke(Vector2Int.zero);
            }

            Hide();
        }

        private void OnDisable() => Hide();

        private void Hide()
        {
            _active = false;
            _pointerId = int.MinValue;
            _direction = Vector2Int.zero;
            if (_ring != null)
            {
                _ring.gameObject.SetActive(false);
            }
        }

        private void PositionVisual(RectTransform target, Vector2 screenPosition, Camera eventCamera)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_surface, screenPosition, eventCamera, out var local))
            {
                target.anchoredPosition = local;
            }
        }

        private Vector2 ScreenDeltaToLocal(Vector2 screenDelta)
        {
            var canvas = _surface.GetComponentInParent<Canvas>();
            return screenDelta / Mathf.Max(0.001f, canvas != null ? canvas.scaleFactor : 1f);
        }

        private static Vector2Int Quantize(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                return new Vector2Int(delta.x > 0f ? 1 : -1, 0);
            }

            return new Vector2Int(0, delta.y > 0f ? 1 : -1);
        }
    }
}
