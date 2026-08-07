using System;
using UnityEngine;
using UnityEngine.UI;

namespace OneStep.Gameplay.Overworld
{
    internal static class RuntimeUiFactory
    {
        private static Font _font;

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        public static Image CreatePanel(string name, Transform parent, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            return image;
        }

        public static Text CreateText(string name, Transform parent, string value, int size, TextAnchor alignment, Color color)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = _font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(
            string name,
            Transform parent,
            string label,
            int size,
            Vector2 min,
            Vector2 max,
            Action clicked)
        {
            var button = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)).GetComponent<Button>();
            button.transform.SetParent(parent, false);
            SetRect(button.GetComponent<RectTransform>(), min, max);
            button.image.color = new Color(0.1f, 0.28f, 0.24f, 1f);
            var text = CreateText("Label", button.transform, label, size, TextAnchor.MiddleCenter, Color.white);
            Stretch(text.rectTransform, new Vector2(14f, 8f), new Vector2(-14f, -8f));
            button.onClick.AddListener(() => clicked());
            return button;
        }

        public static InputField CreateInputField(string name, Transform parent, string placeholderText)
        {
            var input = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField)).GetComponent<InputField>();
            input.transform.SetParent(parent, false);
            input.image.color = new Color(0.04f, 0.08f, 0.09f, 1f);
            var value = CreateText("Text", input.transform, string.Empty, 32, TextAnchor.MiddleLeft, Color.white);
            Stretch(value.rectTransform, new Vector2(24f, 0f), new Vector2(-24f, 0f));
            var placeholder = CreateText("Placeholder", input.transform, placeholderText, 30, TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.35f));
            Stretch(placeholder.rectTransform, new Vector2(24f, 0f), new Vector2(-24f, 0f));
            input.textComponent = value;
            input.placeholder = placeholder;
            input.characterLimit = 16;
            return input;
        }

        public static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        public static void Stretch(RectTransform rect, Vector2? offsetMin = null, Vector2? offsetMax = null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin ?? Vector2.zero;
            rect.offsetMax = offsetMax ?? Vector2.zero;
            rect.localScale = Vector3.one;
        }
    }
}
