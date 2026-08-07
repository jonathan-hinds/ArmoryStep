using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OneStep.Gameplay.Overworld
{
    public static class CharacterCarouselMath
    {
        public static int CalculateTargetIndex(
            int startIndex,
            float dragDelta,
            float releaseVelocity,
            int slotCount,
            float dragThreshold,
            float velocityThreshold)
        {
            if (slotCount <= 0)
            {
                return 0;
            }

            var direction = 0;
            if (Mathf.Abs(dragDelta) >= dragThreshold)
            {
                direction = dragDelta < 0f ? 1 : -1;
            }
            else if (Mathf.Abs(releaseVelocity) >= velocityThreshold)
            {
                direction = releaseVelocity < 0f ? 1 : -1;
            }

            return Mathf.Clamp(startIndex + direction, 0, slotCount - 1);
        }
    }

    [DisallowMultipleComponent]
    public sealed class CharacterCarousel : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        private const float DragThreshold = 72f;
        private const float VelocityThreshold = 420f;
        private const float SettleTime = 0.14f;
        private const float CardWidthRatio = 0.68f;
        private const float CardHeightRatio = 0.92f;
        private const float CardGapRatio = 0.055f;

        private readonly List<RectTransform> _cards = new();
        private readonly List<CanvasGroup> _cardGroups = new();
        private ScrollRect _scrollRect;
        private RectTransform _viewport;
        private RectTransform _content;
        private float _slotStride;
        private float _settleVelocity;
        private float _dragStartX;
        private float _lastDragEndTime = float.NegativeInfinity;
        private bool _dragging;
        private bool _settling;
        private int _selectedIndex;
        private int _dragStartIndex;
        private Vector2 _lastViewportSize;

        public event Action<int> SelectionChanged;
        public event Action<int> SlotActivated;

        public int SelectedIndex => _selectedIndex;

        public void Configure(ScrollRect scrollRect, RectTransform viewport, RectTransform content)
        {
            _scrollRect = scrollRect;
            _viewport = viewport;
            _content = content;
            _scrollRect.horizontal = true;
            _scrollRect.vertical = false;
            _scrollRect.inertia = false;
            _scrollRect.movementType = ScrollRect.MovementType.Elastic;
            _scrollRect.elasticity = 0.12f;
            _scrollRect.scrollSensitivity = 0f;
        }

        public void RegisterCard(RectTransform card)
        {
            _cards.Add(card);
            var canvasGroup = card.gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = card.gameObject.AddComponent<CanvasGroup>();
            }
            _cardGroups.Add(canvasGroup);
            RebuildLayout();
        }

        public void Select(int index, bool animated)
        {
            if (_cards.Count == 0 || _content == null)
            {
                return;
            }

            SetSelectedIndex(Mathf.Clamp(index, 0, _cards.Count - 1));
            if (animated)
            {
                _settling = true;
                _settleVelocity = 0f;
            }
            else
            {
                _settling = false;
                SetContentX(TargetContentX(_selectedIndex));
                UpdateCardEmphasis();
            }
        }

        public void SelectRelative(int offset) => Select(_selectedIndex + offset, true);

        public void HandleCardTapped(int index)
        {
            if (_dragging || Time.unscaledTime - _lastDragEndTime < 0.12f)
            {
                return;
            }

            if (index != _selectedIndex)
            {
                Select(index, true);
                return;
            }

            SlotActivated?.Invoke(index);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragging = true;
            _settling = false;
            _dragStartIndex = _selectedIndex;
            _dragStartX = _content.anchoredPosition.x;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragging = false;
            _lastDragEndTime = Time.unscaledTime;
            var dragDelta = _content.anchoredPosition.x - _dragStartX;
            var target = CharacterCarouselMath.CalculateTargetIndex(
                _dragStartIndex,
                dragDelta,
                _scrollRect.velocity.x,
                _cards.Count,
                DragThreshold,
                VelocityThreshold);
            Select(target, true);
        }

        private void LateUpdate()
        {
            if (_content == null || _cards.Count == 0)
            {
                return;
            }

            if ((_viewport.rect.size - _lastViewportSize).sqrMagnitude > 0.01f)
            {
                RebuildLayout();
            }

            if (_dragging)
            {
                var nearest = Mathf.Clamp(Mathf.RoundToInt(-_content.anchoredPosition.x / _slotStride), 0, _cards.Count - 1);
                SetSelectedIndex(nearest);
            }
            else if (_settling)
            {
                var targetX = TargetContentX(_selectedIndex);
                var currentX = Mathf.SmoothDamp(
                    _content.anchoredPosition.x,
                    targetX,
                    ref _settleVelocity,
                    SettleTime,
                    Mathf.Infinity,
                    Time.unscaledDeltaTime);
                SetContentX(currentX);
                if (Mathf.Abs(currentX - targetX) < 0.5f)
                {
                    SetContentX(targetX);
                    _settling = false;
                    _settleVelocity = 0f;
                }
            }

            UpdateCardEmphasis();
        }

        private void SetSelectedIndex(int value)
        {
            if (_selectedIndex == value)
            {
                return;
            }

            _selectedIndex = value;
            SelectionChanged?.Invoke(value);
        }

        private float TargetContentX(int index) => -index * _slotStride;

        private void RebuildLayout()
        {
            if (_viewport == null || _content == null || _cards.Count == 0 || _viewport.rect.width <= 0f)
            {
                return;
            }

            _lastViewportSize = _viewport.rect.size;
            var cardWidth = _lastViewportSize.x * CardWidthRatio;
            var cardHeight = _lastViewportSize.y * CardHeightRatio;
            var cardGap = _lastViewportSize.x * CardGapRatio;
            _slotStride = cardWidth + cardGap;
            var sidePadding = (_lastViewportSize.x - cardWidth) * 0.5f;
            _content.sizeDelta = new Vector2(
                sidePadding * 2f + _cards.Count * cardWidth + (_cards.Count - 1) * cardGap,
                cardHeight);

            for (var index = 0; index < _cards.Count; index++)
            {
                var card = _cards[index];
                card.anchorMin = card.anchorMax = new Vector2(0f, 0.5f);
                card.pivot = new Vector2(0.5f, 0.5f);
                card.sizeDelta = new Vector2(cardWidth, cardHeight);
                card.anchoredPosition = new Vector2(sidePadding + cardWidth * 0.5f + index * _slotStride, 0f);
            }

            SetContentX(TargetContentX(_selectedIndex));
            UpdateCardEmphasis();
        }

        private void SetContentX(float value)
        {
            var position = _content.anchoredPosition;
            position.x = value;
            _content.anchoredPosition = position;
        }

        private void UpdateCardEmphasis()
        {
            var viewportCenter = _viewport.TransformPoint(_viewport.rect.center);
            for (var index = 0; index < _cards.Count; index++)
            {
                var card = _cards[index];
                var cardCenter = card.TransformPoint(card.rect.center);
                var distance = Mathf.Clamp01(Mathf.Abs(cardCenter.x - viewportCenter.x) / _slotStride);
                var scale = Mathf.Lerp(1f, 0.86f, distance);
                card.localScale = new Vector3(scale, scale, 1f);
                _cardGroups[index].alpha = Mathf.Lerp(1f, 0.42f, distance);
            }
        }
    }
}
