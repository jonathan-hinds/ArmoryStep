using System;
using UnityEngine;
using UnityEngine.UI;
using static OneStep.Gameplay.Overworld.RuntimeUiFactory;

namespace OneStep.Gameplay.Overworld
{
    [DisallowMultipleComponent]
    public sealed class CharacterSlotCardView : MonoBehaviour
    {
        private static readonly Color Ink = new(0.025f, 0.04f, 0.05f, 1f);
        private static readonly Color Panel = new(0.07f, 0.11f, 0.12f, 1f);
        private static readonly Color InnerPanel = new(0.04f, 0.075f, 0.08f, 1f);
        private static readonly Color Accent = new(0.25f, 0.86f, 0.61f, 1f);
        private static readonly Color Warm = new(1f, 0.66f, 0.30f, 1f);
        private static readonly Color Muted = new(0.59f, 0.68f, 0.67f, 1f);

        private int _slotIndex;
        private Action<int> _activated;
        private Image _background;
        private Text _slotLabel;
        private Text _statusLabel;
        private Text _nameLabel;
        private Text _classLabel;
        private Text _crestLabel;
        private Text _healthValue;
        private Text _manaValue;
        private Text _damageValue;
        private Text _levelValue;
        private Text _experienceValue;
        private Text _bestValue;
        private Image _actionPanel;
        private Text _actionLabel;
        private GameObject _occupiedDetails;
        private GameObject _emptyDetails;

        public static CharacterSlotCardView Create(Transform parent, int slotIndex, Action<int> activated)
        {
            var background = CreatePanel($"CharacterSlot_{slotIndex + 1}", parent, Panel);
            background.raycastTarget = true;
            var view = background.gameObject.AddComponent<CharacterSlotCardView>();
            view._background = background;
            view._slotIndex = slotIndex;
            view._activated = activated;
            view.Build();
            return view;
        }

        public void Bind(CharacterSlotData slot)
        {
            var character = slot?.character;
            var occupied = character != null;
            _occupiedDetails.SetActive(occupied);
            _emptyDetails.SetActive(!occupied);
            _slotLabel.text = $"SLOT {_slotIndex + 1:00}";

            if (!occupied)
            {
                _background.color = new Color(0.045f, 0.085f, 0.09f, 1f);
                _statusLabel.text = "AVAILABLE";
                _statusLabel.color = Muted;
                _actionPanel.color = new Color(0.08f, 0.23f, 0.20f, 1f);
                _actionLabel.text = "CREATE CHARACTER";
                return;
            }

            _background.color = Panel;
            _statusLabel.text = character.HasSavedAdventure ? "ADVENTURE SAVED" : "READY";
            _statusLabel.color = character.HasSavedAdventure ? Warm : Accent;
            _nameLabel.text = character.displayName.ToUpperInvariant();
            _classLabel.text = character.classId.ToUpperInvariant();
            _crestLabel.text = string.IsNullOrWhiteSpace(character.classId) ? "?" : character.classId[..1].ToUpperInvariant();
            _healthValue.text = character.maxHealth.ToString();
            _manaValue.text = character.maxMana.ToString();
            _damageValue.text = character.meleeDamage.ToString();
            _levelValue.text = character.level.ToString();
            _experienceValue.text = $"{character.experience} / {character.ExperienceToNextLevel}";
            _bestValue.text = $"{character.bestProgress} STEPS";
            _actionPanel.color = character.HasSavedAdventure
                ? new Color(0.32f, 0.19f, 0.075f, 1f)
                : new Color(0.08f, 0.28f, 0.235f, 1f);
            _actionLabel.text = character.HasSavedAdventure
                ? $"RESUME  -  {character.activeAdventure.progress} STEPS"
                : "START ADVENTURE";
        }

        private void Build()
        {
            var outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.25f, 0.86f, 0.61f, 0.78f);
            outline.effectDistance = new Vector2(4f, -4f);

            var button = gameObject.AddComponent<Button>();
            button.targetGraphic = _background;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f);
            colors.pressedColor = new Color(0.78f, 0.88f, 0.84f, 1f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.onClick.AddListener(() => _activated?.Invoke(_slotIndex));

            _slotLabel = CreateText("Slot", transform, string.Empty, 22, TextAnchor.MiddleLeft, Muted);
            SetRect(_slotLabel.rectTransform, new Vector2(0.055f, 0.925f), new Vector2(0.40f, 0.98f));
            _statusLabel = CreateText("Status", transform, string.Empty, 20, TextAnchor.MiddleRight, Accent);
            SetRect(_statusLabel.rectTransform, new Vector2(0.40f, 0.925f), new Vector2(0.945f, 0.98f));

            _occupiedDetails = CreateRect("OccupiedCharacter", transform).gameObject;
            Stretch(_occupiedDetails.GetComponent<RectTransform>());
            BuildOccupiedCard(_occupiedDetails.transform);

            _emptyDetails = CreateRect("EmptySlot", transform).gameObject;
            Stretch(_emptyDetails.GetComponent<RectTransform>());
            BuildEmptyCard(_emptyDetails.transform);

            _actionPanel = CreatePanel("Action", transform, new Color(0.08f, 0.28f, 0.235f, 1f));
            SetRect(_actionPanel.rectTransform, new Vector2(0.055f, 0.045f), new Vector2(0.945f, 0.145f));
            _actionPanel.raycastTarget = false;
            _actionLabel = CreateText("ActionLabel", _actionPanel.transform, string.Empty, 27, TextAnchor.MiddleCenter, Color.white);
            Stretch(_actionLabel.rectTransform, new Vector2(12f, 4f), new Vector2(-12f, -4f));
        }

        private void BuildOccupiedCard(Transform parent)
        {
            _nameLabel = CreateText("Name", parent, string.Empty, 40, TextAnchor.MiddleCenter, Color.white);
            SetRect(_nameLabel.rectTransform, new Vector2(0.05f, 0.845f), new Vector2(0.95f, 0.925f));
            _classLabel = CreateText("Class", parent, string.Empty, 22, TextAnchor.MiddleCenter, Warm);
            SetRect(_classLabel.rectTransform, new Vector2(0.05f, 0.79f), new Vector2(0.95f, 0.845f));

            var portrait = CreatePanel("ClassCrest", parent, Ink);
            SetRect(portrait.rectTransform, new Vector2(0.35f, 0.59f), new Vector2(0.65f, 0.785f));
            portrait.raycastTarget = false;
            var portraitOutline = portrait.gameObject.AddComponent<Outline>();
            portraitOutline.effectColor = new Color(Warm.r, Warm.g, Warm.b, 0.8f);
            portraitOutline.effectDistance = new Vector2(3f, -3f);
            _crestLabel = CreateText("Initial", portrait.transform, "W", 76, TextAnchor.MiddleCenter, Warm);
            Stretch(_crestLabel.rectTransform);

            var section = CreateText("AttributesHeader", parent, "CORE ATTRIBUTES", 20, TextAnchor.MiddleLeft, Muted);
            SetRect(section.rectTransform, new Vector2(0.055f, 0.535f), new Vector2(0.945f, 0.585f));
            _healthValue = CreateStatTile(parent, "Health", "HP", new Vector2(0.055f, 0.38f), new Vector2(0.335f, 0.53f), new Color(1f, 0.45f, 0.43f, 1f));
            _manaValue = CreateStatTile(parent, "Mana", "MP", new Vector2(0.36f, 0.38f), new Vector2(0.64f, 0.53f), new Color(0.43f, 0.7f, 1f, 1f));
            _damageValue = CreateStatTile(parent, "Damage", "DMG", new Vector2(0.665f, 0.38f), new Vector2(0.945f, 0.53f), Warm);

            var progress = CreatePanel("Progression", parent, InnerPanel);
            SetRect(progress.rectTransform, new Vector2(0.055f, 0.17f), new Vector2(0.945f, 0.355f));
            progress.raycastTarget = false;
            CreateRowLabel(progress.transform, "LEVEL", 0.67f, 0.94f);
            _levelValue = CreateRowValue(progress.transform, "LevelValue", 0.67f, 0.94f, Accent);
            CreateRowLabel(progress.transform, "XP", 0.36f, 0.66f);
            _experienceValue = CreateRowValue(progress.transform, "ExperienceValue", 0.36f, 0.66f, Color.white);
            CreateRowLabel(progress.transform, "BEST RUN", 0.06f, 0.35f);
            _bestValue = CreateRowValue(progress.transform, "BestValue", 0.06f, 0.35f, Warm);
        }

        private static void BuildEmptyCard(Transform parent)
        {
            var plusFrame = CreatePanel("NewCharacterCrest", parent, Ink);
            SetRect(plusFrame.rectTransform, new Vector2(0.34f, 0.57f), new Vector2(0.66f, 0.78f));
            plusFrame.raycastTarget = false;
            var outline = plusFrame.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.25f, 0.86f, 0.61f, 0.65f);
            outline.effectDistance = new Vector2(3f, -3f);
            var plus = CreateText("Plus", plusFrame.transform, "+", 86, TextAnchor.MiddleCenter, Accent);
            Stretch(plus.rectTransform);

            var title = CreateText("Title", parent, "EMPTY CHARACTER SLOT", 34, TextAnchor.MiddleCenter, Color.white);
            SetRect(title.rectTransform, new Vector2(0.08f, 0.44f), new Vector2(0.92f, 0.54f));
            var copy = CreateText("Copy", parent,
                "Begin a new permanent hero\nand build their legacy.",
                25, TextAnchor.MiddleCenter, Muted);
            SetRect(copy.rectTransform, new Vector2(0.10f, 0.30f), new Vector2(0.90f, 0.43f));
            var hint = CreateText("Hint", parent, "TAP TO CREATE", 21, TextAnchor.MiddleCenter, Accent);
            SetRect(hint.rectTransform, new Vector2(0.10f, 0.19f), new Vector2(0.90f, 0.27f));
        }

        private static Text CreateStatTile(Transform parent, string name, string label, Vector2 min, Vector2 max, Color valueColor)
        {
            var tile = CreatePanel(name, parent, InnerPanel);
            SetRect(tile.rectTransform, min, max);
            tile.raycastTarget = false;
            var caption = CreateText("Label", tile.transform, label, 18, TextAnchor.UpperCenter, Muted);
            SetRect(caption.rectTransform, new Vector2(0.05f, 0.54f), new Vector2(0.95f, 0.91f));
            var value = CreateText("Value", tile.transform, "0", 39, TextAnchor.LowerCenter, valueColor);
            SetRect(value.rectTransform, new Vector2(0.05f, 0.09f), new Vector2(0.95f, 0.62f));
            return value;
        }

        private static void CreateRowLabel(Transform parent, string value, float minY, float maxY)
        {
            var label = CreateText(value + "Label", parent, value, 18, TextAnchor.MiddleLeft, Muted);
            SetRect(label.rectTransform, new Vector2(0.04f, minY), new Vector2(0.38f, maxY));
        }

        private static Text CreateRowValue(Transform parent, string name, float minY, float maxY, Color color)
        {
            var value = CreateText(name, parent, string.Empty, 23, TextAnchor.MiddleRight, color);
            SetRect(value.rectTransform, new Vector2(0.34f, minY), new Vector2(0.96f, maxY));
            return value;
        }
    }
}
