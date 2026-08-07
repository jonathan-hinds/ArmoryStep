using System;
using System.Collections.Generic;
using OneStep.Platform;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static OneStep.Gameplay.Overworld.RuntimeUiFactory;

namespace OneStep.Gameplay.Overworld
{
    [DefaultExecutionOrder(-200)]
    public sealed class OneBitGameRoot : MonoBehaviour
    {
        [SerializeField] private AdventureConfiguration configuration;

        private readonly Color _ink = new(0.035f, 0.055f, 0.065f, 1f);
        private readonly Color _panel = new(0.075f, 0.115f, 0.125f, 0.97f);
        private readonly Color _accent = new(0.25f, 0.86f, 0.61f, 1f);
        private readonly Color _warm = new(1f, 0.58f, 0.22f, 1f);
        private ICharacterRepository _repository;
        private CharacterRosterData _roster;
        private CharacterData _activeCharacter;
        private AdventureSession _session;
        private AdventureWorldView _worldView;
        private GameObject _selectionScreen;
        private GameObject _creationScreen;
        private GameObject _gameplayScreen;
        private GameObject _campfireModal;
        private GameObject _deathModal;
        private Text _slotCounter;
        private readonly List<CharacterSlotCardView> _characterCards = new();
        private CharacterCarousel _characterCarousel;
        private Text[] _slotDots;
        private Text _creationSlotLabel;
        private InputField _nameInput;
        private AdventureHudView _adventureHud;
        private Text _deathText;
        private FloatingJoystickInput _floatingJoystick;
        private DiscreteInputDriver _discreteInput;
        private VerticalCameraFollower _cameraFollower;
        private int _selectedSlot;

        public void Configure(AdventureConfiguration value) => configuration = value;

        private void Awake()
        {
            if (configuration == null)
            {
                configuration = ScriptableObject.CreateInstance<AdventureConfiguration>();
                configuration.ConfigureDefaults();
            }
            configuration.EnsureDefaults();

            _repository = new PlayerPrefsCharacterRepository();
            _roster = _repository.Load();
            EnsureEventSystem();
            BuildInterface();
            _discreteInput = gameObject.AddComponent<DiscreteInputDriver>();
            _discreteInput.Configure(configuration);
            _discreteInput.ActionRequested += HandlePlayerAction;
            ShowCharacterSelection();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                _repository?.Save(_roster);
            }
        }

        private void OnApplicationQuit() => _repository?.Save(_roster);

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !_selectionScreen.activeSelf)
            {
                return;
            }

            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            {
                _characterCarousel.SelectRelative(-1);
            }
            else if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            {
                _characterCarousel.SelectRelative(1);
            }
            else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                ActivateSelectedSlot();
            }
        }

        private void BuildInterface()
        {
            var canvasObject = new GameObject("GameUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var backdrop = CreatePanel("Backdrop", canvasObject.transform, new Color(_ink.r, _ink.g, _ink.b, 0f));
            Stretch(backdrop.rectTransform);
            backdrop.raycastTarget = false;

            var safeArea = CreateRect("SafeArea", canvasObject.transform);
            Stretch(safeArea);
            safeArea.gameObject.AddComponent<SafeAreaFitter>();
            var portraitFrame = CreateRect("PortraitFrame_9x16", safeArea);
            Stretch(portraitFrame);
            var aspect = portraitFrame.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = 9f / 16f;

            _selectionScreen = BuildSelectionScreen(portraitFrame).gameObject;
            _creationScreen = BuildCreationScreen(portraitFrame).gameObject;
            _gameplayScreen = BuildGameplayScreen(portraitFrame).gameObject;
            _campfireModal = BuildCampfireModal(portraitFrame).gameObject;
            _deathModal = BuildDeathModal(portraitFrame).gameObject;
        }

        private RectTransform BuildSelectionScreen(Transform parent)
        {
            var root = CreateScreen("CharacterSelection", parent);
            var title = CreateText("Title", root, "CHOOSE YOUR CHARACTER", 34, TextAnchor.MiddleCenter, _accent);
            SetRect(title.rectTransform, new Vector2(0.06f, 0.89f), new Vector2(0.94f, 0.96f));
            var hint = CreateText("Hint", root, "Your heroes. Your progress. Your adventure.", 24, TextAnchor.MiddleCenter, new Color(0.66f, 0.74f, 0.73f));
            SetRect(hint.rectTransform, new Vector2(0.06f, 0.845f), new Vector2(0.94f, 0.89f));

            _slotCounter = CreateText("SlotCounter", root, "01 / 05", 25, TextAnchor.MiddleCenter, Color.white);
            SetRect(_slotCounter.rectTransform, new Vector2(0.35f, 0.795f), new Vector2(0.65f, 0.84f));

            var viewportImage = CreatePanel("CharacterCarousel", root, new Color(0f, 0f, 0f, 0.001f));
            SetRect(viewportImage.rectTransform, new Vector2(0f, 0.245f), new Vector2(1f, 0.795f));
            viewportImage.gameObject.AddComponent<RectMask2D>();
            var content = CreateRect("Content", viewportImage.transform);
            content.anchorMin = content.anchorMax = new Vector2(0f, 0.5f);
            content.pivot = new Vector2(0f, 0.5f);

            content.anchoredPosition = Vector2.zero;

            var scrollRect = viewportImage.gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewportImage.rectTransform;
            scrollRect.content = content;
            _characterCarousel = viewportImage.gameObject.AddComponent<CharacterCarousel>();
            _characterCarousel.Configure(scrollRect, viewportImage.rectTransform, content);
            _characterCarousel.SelectionChanged += HandleCarouselSelectionChanged;
            _characterCarousel.SlotActivated += HandleCarouselSlotActivated;

            for (var index = 0; index < CharacterRosterData.SlotCount; index++)
            {
                var card = CharacterSlotCardView.Create(content, index, _characterCarousel.HandleCardTapped);
                var cardRect = card.GetComponent<RectTransform>();
                _characterCards.Add(card);
                _characterCarousel.RegisterCard(cardRect);
            }

            var dots = CreateRect("SlotIndicators", root);
            SetRect(dots, new Vector2(0.28f, 0.19f), new Vector2(0.72f, 0.24f));
            _slotDots = new Text[CharacterRosterData.SlotCount];
            for (var index = 0; index < _slotDots.Length; index++)
            {
                _slotDots[index] = CreateText($"SlotDot_{index + 1}", dots, "o", 27, TextAnchor.MiddleCenter, new Color(0.30f, 0.39f, 0.39f));
                SetRect(_slotDots[index].rectTransform,
                    new Vector2(index / (float)_slotDots.Length, 0f),
                    new Vector2((index + 1f) / _slotDots.Length, 1f));
            }

            var controls = CreateText("Controls", root, "SWIPE TO BROWSE   -   TAP TO SELECT", 22, TextAnchor.MiddleCenter, new Color(0.52f, 0.63f, 0.62f));
            SetRect(controls.rectTransform, new Vector2(0.06f, 0.135f), new Vector2(0.94f, 0.19f));
            return root;
        }

        private RectTransform BuildCreationScreen(Transform parent)
        {
            var root = CreateScreen("CharacterCreation", parent);
            var title = CreateText("Title", root, "CREATE CHARACTER", 45, TextAnchor.MiddleCenter, _accent);
            SetRect(title.rectTransform, new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.95f));
            _creationSlotLabel = CreateText("Slot", root, "SLOT 01", 23, TextAnchor.MiddleCenter, new Color(0.6f, 0.69f, 0.68f));
            SetRect(_creationSlotLabel.rectTransform, new Vector2(0.08f, 0.835f), new Vector2(0.92f, 0.88f));
            var classPanel = CreatePanel("ClassCard", root, _panel);
            SetRect(classPanel.rectTransform, new Vector2(0.08f, 0.43f), new Vector2(0.92f, 0.81f));
            var classTitle = CreateText("ClassName", classPanel.transform, "WAYFARER", 42, TextAnchor.MiddleLeft, _warm);
            SetRect(classTitle.rectTransform, new Vector2(0.055f, 0.80f), new Vector2(0.68f, 0.95f));
            var role = CreateText("Role", classPanel.transform, "BALANCED MELEE ADVENTURER", 20, TextAnchor.MiddleRight, new Color(0.61f, 0.69f, 0.68f));
            SetRect(role.rectTransform, new Vector2(0.48f, 0.80f), new Vector2(0.945f, 0.95f));

            var crest = CreatePanel("ClassCrest", classPanel.transform, new Color(0.025f, 0.045f, 0.05f, 1f));
            SetRect(crest.rectTransform, new Vector2(0.055f, 0.38f), new Vector2(0.29f, 0.75f));
            crest.raycastTarget = false;
            var crestOutline = crest.gameObject.AddComponent<Outline>();
            crestOutline.effectColor = new Color(_warm.r, _warm.g, _warm.b, 0.8f);
            crestOutline.effectDistance = new Vector2(3f, -3f);
            var crestText = CreateText("Initial", crest.transform, "W", 76, TextAnchor.MiddleCenter, _warm);
            Stretch(crestText.rectTransform);

            var attributes = CreateText("Attributes", classPanel.transform, "STARTING ATTRIBUTES", 19, TextAnchor.MiddleLeft, new Color(0.61f, 0.69f, 0.68f));
            SetRect(attributes.rectTransform, new Vector2(0.34f, 0.68f), new Vector2(0.94f, 0.78f));
            CreateCreationStatRow(classPanel.transform, "HEALTH", configuration.BaseHealth.ToString(), 0.53f, new Color(1f, 0.45f, 0.43f));
            CreateCreationStatRow(classPanel.transform, "MANA", configuration.BaseMana.ToString(), 0.39f, new Color(0.43f, 0.7f, 1f));
            CreateCreationStatRow(classPanel.transform, "MELEE DAMAGE", configuration.BaseMeleeDamage.ToString(), 0.25f, _warm);

            var trait = CreatePanel("ClassTrait", classPanel.transform, new Color(0.04f, 0.075f, 0.08f, 1f));
            SetRect(trait.rectTransform, new Vector2(0.055f, 0.055f), new Vector2(0.945f, 0.21f));
            trait.raycastTarget = false;
            var traitLabel = CreateText("Label", trait.transform, "COMBAT STYLE", 17, TextAnchor.MiddleLeft, new Color(0.55f, 0.64f, 0.63f));
            SetRect(traitLabel.rectTransform, new Vector2(0.035f, 0.50f), new Vector2(0.37f, 0.92f));
            var traitCopy = CreateText("Copy", trait.transform, "Cardinal movement. Bump enemies to strike.", 21, TextAnchor.MiddleRight, Color.white);
            SetRect(traitCopy.rectTransform, new Vector2(0.31f, 0.08f), new Vector2(0.965f, 0.92f));

            var nameLabel = CreateText("NameLabel", root, "CHARACTER NAME", 21, TextAnchor.MiddleLeft, new Color(0.62f, 0.71f, 0.7f));
            SetRect(nameLabel.rectTransform, new Vector2(0.10f, 0.36f), new Vector2(0.90f, 0.405f));
            _nameInput = CreateInputField("CharacterName", root, "Character name");
            SetRect(_nameInput.GetComponent<RectTransform>(), new Vector2(0.10f, 0.285f), new Vector2(0.90f, 0.36f));
            var create = CreateButton("Create", root, "CREATE WAYFARER", 30, new Vector2(0.10f, 0.17f), new Vector2(0.90f, 0.255f), CreateCharacter);
            create.image.color = new Color(0.08f, 0.31f, 0.25f, 1f);
            CreateButton("Back", root, "BACK TO CHARACTERS", 25, new Vector2(0.10f, 0.08f), new Vector2(0.90f, 0.145f), ShowCharacterSelection);
            return root;
        }

        private static void CreateCreationStatRow(Transform parent, string label, string value, float minY, Color valueColor)
        {
            var row = CreatePanel(label.Replace(" ", string.Empty), parent, new Color(0.04f, 0.075f, 0.08f, 1f));
            SetRect(row.rectTransform, new Vector2(0.34f, minY), new Vector2(0.945f, minY + 0.115f));
            row.raycastTarget = false;
            var caption = CreateText("Label", row.transform, label, 19, TextAnchor.MiddleLeft, new Color(0.59f, 0.68f, 0.67f));
            SetRect(caption.rectTransform, new Vector2(0.045f, 0.08f), new Vector2(0.68f, 0.92f));
            var amount = CreateText("Value", row.transform, value, 30, TextAnchor.MiddleRight, valueColor);
            SetRect(amount.rectTransform, new Vector2(0.64f, 0.08f), new Vector2(0.955f, 0.92f));
        }

        private RectTransform BuildGameplayScreen(Transform parent)
        {
            var root = CreateScreen("Gameplay", parent);
            var inputSurface = CreatePanel("DynamicJoystickSurface", root, new Color(0f, 0f, 0f, 0f));
            Stretch(inputSurface.rectTransform);
            inputSurface.raycastTarget = true;

            var circleFrame = CreateCircleSprite(true);
            var circleFill = CreateCircleSprite(false);
            var ring = CreatePanel("JoystickRing", inputSurface.transform, new Color(0.7f, 0.9f, 0.83f, 0.2f));
            ring.raycastTarget = false;
            ring.sprite = circleFrame;
            ring.preserveAspect = true;
            ring.rectTransform.sizeDelta = new Vector2(190f, 190f);
            ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            var knob = CreatePanel("JoystickKnob", ring.transform, new Color(0.35f, 1f, 0.72f, 0.55f));
            knob.raycastTarget = false;
            knob.sprite = circleFill;
            knob.preserveAspect = true;
            knob.rectTransform.sizeDelta = new Vector2(72f, 72f);
            knob.rectTransform.anchorMin = knob.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            knob.rectTransform.anchoredPosition = Vector2.zero;
            _floatingJoystick = inputSurface.gameObject.AddComponent<FloatingJoystickInput>();
            _floatingJoystick.Configure(configuration, ring.rectTransform, knob.rectTransform);
            _floatingJoystick.ActionRequested += HandlePlayerAction;

            var hud = CreateRect("AdventureHUD", root);
            _adventureHud = hud.gameObject.AddComponent<AdventureHudView>();
            _adventureHud.Build(circleFrame, circleFill, _ink, _panel, _accent, _warm);
            return root;
        }

        private RectTransform BuildCampfireModal(Transform parent)
        {
            var shade = CreatePanel("CampfireModal", parent, new Color(0f, 0f, 0f, 0.78f));
            Stretch(shade.rectTransform);
            var panel = CreatePanel("Panel", shade.transform, _panel);
            SetRect(panel.rectTransform, new Vector2(0.08f, 0.23f), new Vector2(0.92f, 0.77f));
            var title = CreateText("Title", panel.transform, "BONFIRE", 52, TextAnchor.MiddleCenter, _warm);
            SetRect(title.rectTransform, new Vector2(0.08f, 0.79f), new Vector2(0.92f, 0.96f));
            var copy = CreateText("Copy", panel.transform,
                "Rest restores health and mana.\nIt is not a respawn checkpoint.", 29, TextAnchor.MiddleCenter, Color.white);
            SetRect(copy.rectTransform, new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.78f));
            CreateButton("Rest", panel.transform, "REST", 31, new Vector2(0.09f, 0.42f), new Vector2(0.91f, 0.55f), RestAtBonfire);
            CreateButton("Continue", panel.transform, "CONTINUE ADVENTURE", 29, new Vector2(0.09f, 0.25f), new Vector2(0.91f, 0.38f), ContinueAdventure);
            var save = CreateButton("SaveAndHome", panel.transform, "SAVE AND GO HOME", 29, new Vector2(0.09f, 0.08f), new Vector2(0.91f, 0.21f), SaveAndGoHome);
            save.image.color = new Color(0.31f, 0.19f, 0.08f, 1f);
            return shade.rectTransform;
        }

        private RectTransform BuildDeathModal(Transform parent)
        {
            var shade = CreatePanel("DeathModal", parent, new Color(0f, 0f, 0f, 0.84f));
            Stretch(shade.rectTransform);
            var panel = CreatePanel("Panel", shade.transform, _panel);
            SetRect(panel.rectTransform, new Vector2(0.09f, 0.29f), new Vector2(0.91f, 0.71f));
            var title = CreateText("Title", panel.transform, "ADVENTURE ENDED", 48, TextAnchor.MiddleCenter, new Color(1f, 0.4f, 0.39f));
            SetRect(title.rectTransform, new Vector2(0.06f, 0.73f), new Vector2(0.94f, 0.94f));
            _deathText = CreateText("Summary", panel.transform, string.Empty, 30, TextAnchor.MiddleCenter, Color.white);
            SetRect(_deathText.rectTransform, new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.72f));
            CreateButton("GoHome", panel.transform, "RETURN TO CHARACTERS", 29, new Vector2(0.09f, 0.09f), new Vector2(0.91f, 0.27f), ReturnHomeAfterDeath);
            return shade.rectTransform;
        }

        private void ShowCharacterSelection()
        {
            DestroyWorld();
            _selectionScreen.SetActive(true);
            _creationScreen.SetActive(false);
            _gameplayScreen.SetActive(false);
            _campfireModal.SetActive(false);
            _deathModal.SetActive(false);
            SetGameplayInput(false);
            RefreshCharacterCards();
        }

        private void RefreshCharacterCards()
        {
            if (_characterCards.Count != CharacterRosterData.SlotCount)
            {
                return;
            }

            for (var index = 0; index < _characterCards.Count; index++)
            {
                _characterCards[index].Bind(_roster.slots[index]);
            }

            _characterCarousel.Select(_selectedSlot, false);
            UpdateSlotIndicator();
        }

        private void HandleCarouselSelectionChanged(int slotIndex)
        {
            _selectedSlot = slotIndex;
            UpdateSlotIndicator();
        }

        private void HandleCarouselSlotActivated(int slotIndex)
        {
            _selectedSlot = slotIndex;
            ActivateSelectedSlot();
        }

        private void UpdateSlotIndicator()
        {
            _slotCounter.text = $"{_selectedSlot + 1:00} / {CharacterRosterData.SlotCount:00}";
            for (var index = 0; index < _slotDots.Length; index++)
            {
                var selected = index == _selectedSlot;
                _slotDots[index].text = selected ? "O" : "o";
                _slotDots[index].color = selected ? _accent : new Color(0.30f, 0.39f, 0.39f);
            }
        }

        private void ActivateSelectedSlot()
        {
            var character = _roster.slots[_selectedSlot].character;
            if (character == null)
            {
                _selectionScreen.SetActive(false);
                _creationScreen.SetActive(true);
                _creationSlotLabel.text = $"SLOT {_selectedSlot + 1:00}";
                _nameInput.text = string.Empty;
                _nameInput.ActivateInputField();
                return;
            }

            StartAdventure(character, character.HasSavedAdventure ? character.activeAdventure : null);
        }

        private void CreateCharacter()
        {
            if (_roster.slots[_selectedSlot].character != null)
            {
                return;
            }

            var character = CharacterData.Create(_nameInput.text, configuration);
            _roster.slots[_selectedSlot].occupied = true;
            _roster.slots[_selectedSlot].character = character;
            _repository.Save(_roster);
            ShowCharacterSelection();
        }

        private void StartAdventure(CharacterData character, AdventureSaveData savedAdventure)
        {
            _activeCharacter = character;
            if (savedAdventure == null)
            {
                character.hasActiveAdventure = false;
                character.activeAdventure = null;
                character.adventuresStarted++;
            }

            _session = new AdventureSession(configuration, character, savedAdventure);
            _session.Changed += HandleSessionChanged;
            _session.BonfireEntered += OpenCampfire;
            _session.Died += HandleDeath;
            _session.LeveledUp += HandleLevelUp;

            var worldObject = new GameObject("AdventureWorld");
            _worldView = worldObject.AddComponent<AdventureWorldView>();
            _worldView.Configure(_session, configuration);

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                _cameraFollower = mainCamera.GetComponent<VerticalCameraFollower>() ?? mainCamera.gameObject.AddComponent<VerticalCameraFollower>();
                _cameraFollower.Configure(_session, configuration.WorldWidth);
            }

            _selectionScreen.SetActive(false);
            _creationScreen.SetActive(false);
            _gameplayScreen.SetActive(true);
            _campfireModal.SetActive(false);
            _deathModal.SetActive(false);
            SetGameplayInput(true);
            UpdateHud();
        }

        private void HandlePlayerAction(Vector2Int direction)
        {
            if (_session == null || _session.IsDead || !_gameplayScreen.activeSelf || _campfireModal.activeSelf || _deathModal.activeSelf)
            {
                return;
            }

            _session.TryTakeTurn(direction);
        }

        private void HandleSessionChanged()
        {
            _worldView?.Synchronize();
            UpdateHud();
            _repository.Save(_roster);
        }

        private void HandleLevelUp(int level)
        {
            _repository.Save(_roster);
        }

        private void OpenCampfire()
        {
            _campfireModal.SetActive(true);
            SetGameplayInput(false);
        }

        private void RestAtBonfire()
        {
            _session?.Rest();
            UpdateHud();
        }

        private void ContinueAdventure()
        {
            _campfireModal.SetActive(false);
            SetGameplayInput(true);
        }

        private void SaveAndGoHome()
        {
            if (_session == null || !_session.World.IsBonfire(_session.PlayerPosition))
            {
                return;
            }

            _activeCharacter.activeAdventure = _session.CreateSave();
            _activeCharacter.hasActiveAdventure = true;
            _repository.Save(_roster);
            ShowCharacterSelection();
        }

        private void HandleDeath()
        {
            SetGameplayInput(false);
            _activeCharacter.activeAdventure = null;
            _activeCharacter.hasActiveAdventure = false;
            _repository.Save(_roster);
            _deathText.text = $"{_activeCharacter.displayName}\nreached {_session.Progress} steps.\n\nLevel {_activeCharacter.level} and permanent stats remain.\nThe saved adventure is gone.";
            _deathModal.SetActive(true);
        }

        private void ReturnHomeAfterDeath() => ShowCharacterSelection();

        private void UpdateHud()
        {
            if (_session == null)
            {
                return;
            }

            _adventureHud.Bind(_session, _activeCharacter);
        }

        private void SetGameplayInput(bool enabled)
        {
            if (_floatingJoystick != null)
            {
                _floatingJoystick.InputEnabled = enabled;
            }
            if (_discreteInput != null)
            {
                _discreteInput.InputEnabled = enabled;
            }
        }

        private void DestroyWorld()
        {
            if (_session != null)
            {
                _session.Changed -= HandleSessionChanged;
                _session.BonfireEntered -= OpenCampfire;
                _session.Died -= HandleDeath;
                _session.LeveledUp -= HandleLevelUp;
            }
            if (_worldView != null)
            {
                Destroy(_worldView.gameObject);
            }
            _session = null;
            _worldView = null;
            _activeCharacter = null;
        }

        private RectTransform CreateScreen(string name, Transform parent)
        {
            var rect = CreateRect(name, parent);
            Stretch(rect);
            return rect;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            }
        }

        private static Sprite CreateCircleSprite(bool ring)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = ring ? "RuntimeJoystickRing" : "RuntimeJoystickKnob",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            var pixels = new Color32[size * size];
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var normalizedDistance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    var filled = ring ? normalizedDistance is >= 0.78f and <= 0.98f : normalizedDistance <= 0.96f;
                    pixels[y * size + x] = filled ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }
    }
}
