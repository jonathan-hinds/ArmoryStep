using System;
using UnityEngine;
using UnityEngine.UI;
using static OneStep.Gameplay.Overworld.RuntimeUiFactory;

namespace OneStep.Gameplay.Overworld
{
    /// <summary>
    /// Runtime uGUI presentation for the persistent Adventure HUD. Gameplay state remains owned by
    /// AdventureSession and CharacterData; this view only formats and displays their values.
    /// </summary>
    public sealed class AdventureHudView : MonoBehaviour
    {
        private Text _stepsText;
        private Text _healthText;
        private Text _manaText;
        private Image _healthFill;
        private Image _manaFill;
        private Image _experienceFill;

        public event Action InventoryRequested;
        public event Action EquipmentRequested;

        public void Build(Sprite circleFrame, Sprite circleFill, Color ink, Color panel, Color accent, Color warm)
        {
            var root = GetComponent<RectTransform>();
            Stretch(root);

            _stepsText = CreateText("Steps", root, "STEPS --", 34, TextAnchor.MiddleCenter, warm);
            SetRect(_stepsText.rectTransform, new Vector2(0.22f, 0.64f), new Vector2(0.78f, 0.69f));

            var experienceFrame = CreatePanel("ExperienceBar", root, ink);
            SetRect(experienceFrame.rectTransform, new Vector2(0.055f, 0.136f), new Vector2(0.945f, 0.148f));
            experienceFrame.raycastTarget = false;
            var experienceOutline = experienceFrame.gameObject.AddComponent<Outline>();
            experienceOutline.effectColor = new Color(1f, 1f, 1f, 0.82f);
            experienceOutline.effectDistance = new Vector2(2f, -2f);

            _experienceFill = CreatePanel("Fill", experienceFrame.transform, accent);
            Stretch(_experienceFill.rectTransform, new Vector2(4f, 4f), new Vector2(-4f, -4f));
            _experienceFill.raycastTarget = false;
            _experienceFill.type = Image.Type.Filled;
            _experienceFill.fillMethod = Image.FillMethod.Horizontal;
            _experienceFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _experienceFill.fillAmount = 0f;

            _healthFill = CreateResourceOrb(
                "HealthOrb", root, circleFrame, circleFill, panel,
                new Color(0.92f, 0.20f, 0.18f, 1f),
                new Vector2(0.04f, 0.025f), new Vector2(0.215f, 0.1235f), out _healthText);

            _manaFill = CreateResourceOrb(
                "ManaOrb", root, circleFrame, circleFill, panel,
                new Color(0.18f, 0.48f, 0.93f, 1f),
                new Vector2(0.785f, 0.025f), new Vector2(0.96f, 0.1235f), out _manaText);

            CreateMenuButton(
                "InventoryButton", root, "BAG",
                new Vector2(0.382f, 0.025f), new Vector2(0.489f, 0.0852f),
                panel, accent, HandleInventoryClicked);

            CreateMenuButton(
                "EquipmentButton", root, "EQP",
                new Vector2(0.511f, 0.025f), new Vector2(0.618f, 0.0852f),
                panel, warm, HandleEquipmentClicked);
        }

        public void Bind(AdventureSession session, CharacterData character)
        {
            if (session == null || character == null)
            {
                ShowPlaceholders();
                return;
            }

            _stepsText.text = $"STEPS {Mathf.Max(0, session.Progress)}";
            _healthText.text = $"{Mathf.Max(0, session.Health)}/{Mathf.Max(0, character.maxHealth)}";
            _manaText.text = $"{Mathf.Max(0, session.Mana)}/{Mathf.Max(0, character.maxMana)}";
            _healthFill.fillAmount = CalculateNormalizedFill(session.Health, character.maxHealth);
            _manaFill.fillAmount = CalculateNormalizedFill(session.Mana, character.maxMana);
            _experienceFill.fillAmount = CalculateNormalizedFill(character.experience, character.ExperienceToNextLevel);
        }

        public void ShowPlaceholders()
        {
            if (_stepsText == null)
            {
                return;
            }

            _stepsText.text = "STEPS --";
            _healthText.text = "--/--";
            _manaText.text = "--/--";
            _healthFill.fillAmount = 0f;
            _manaFill.fillAmount = 0f;
            _experienceFill.fillAmount = 0f;
        }

        public static float CalculateNormalizedFill(int current, int maximum)
        {
            return maximum <= 0 ? 0f : Mathf.Clamp01(current / (float)maximum);
        }

        private static Image CreateResourceOrb(
            string name,
            Transform parent,
            Sprite circleFrame,
            Sprite circleFill,
            Color backgroundColor,
            Color fillColor,
            Vector2 min,
            Vector2 max,
            out Text valueText)
        {
            var orb = CreateRect(name, parent);
            SetRect(orb, min, max);

            var background = CreatePanel("Background", orb, backgroundColor);
            Stretch(background.rectTransform);
            background.sprite = circleFill;
            background.preserveAspect = true;
            background.raycastTarget = false;

            var fill = CreatePanel("Fill", orb, fillColor);
            Stretch(fill.rectTransform, new Vector2(8f, 8f), new Vector2(-8f, -8f));
            fill.sprite = circleFill;
            fill.preserveAspect = true;
            fill.raycastTarget = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Vertical;
            fill.fillOrigin = (int)Image.OriginVertical.Bottom;
            fill.fillAmount = 0f;

            var frame = CreatePanel("Frame", orb, Color.white);
            Stretch(frame.rectTransform);
            frame.sprite = circleFrame;
            frame.preserveAspect = true;
            frame.raycastTarget = false;

            valueText = CreateText("Value", orb, "--/--", 27, TextAnchor.MiddleCenter, Color.white);
            Stretch(valueText.rectTransform, new Vector2(8f, 8f), new Vector2(-8f, -8f));
            valueText.resizeTextForBestFit = true;
            valueText.resizeTextMinSize = 18;
            valueText.resizeTextMaxSize = 27;
            return fill;
        }

        private static void CreateMenuButton(
            string name,
            Transform parent,
            string label,
            Vector2 min,
            Vector2 max,
            Color background,
            Color border,
            Action clicked)
        {
            var button = CreateButton(name, parent, label, 20, min, max, clicked);
            button.image.color = background;
            var buttonLabel = button.GetComponentInChildren<Text>();
            buttonLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            var outline = button.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(border.r, border.g, border.b, 0.95f);
            outline.effectDistance = new Vector2(3f, -3f);
        }

        // TODO: Connect these requests to their dedicated screens when those features are added.
        private void HandleInventoryClicked() => InventoryRequested?.Invoke();
        private void HandleEquipmentClicked() => EquipmentRequested?.Invoke();
    }
}
