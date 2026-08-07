using System;
using OneStep.Platform;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

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
        private Font _font;
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
        private Text _cardText;
        private Button _cardButton;
        private InputField _nameInput;
        private Text _healthText;
        private Text _manaText;
        private Text _progressText;
        private Text _levelText;
        private Text _messageText;
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

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
                ShiftSlotLeft();
            }
            else if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            {
                ShiftSlotRight();
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
            var title = CreateText("Title", root, "CHOOSE YOUR CHARACTER", 40, TextAnchor.MiddleCenter, _accent);
            SetRect(title.rectTransform, new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.95f));
            var hint = CreateText("Hint", root, "Five persistent character slots", 27, TextAnchor.MiddleCenter, new Color(0.72f, 0.8f, 0.79f));
            SetRect(hint.rectTransform, new Vector2(0.08f, 0.81f), new Vector2(0.92f, 0.86f));

            _slotCounter = CreateText("SlotCounter", root, "1 / 5", 32, TextAnchor.MiddleCenter, Color.white);
            SetRect(_slotCounter.rectTransform, new Vector2(0.38f, 0.74f), new Vector2(0.62f, 0.8f));
            var left = CreateButton("PreviousSlot", root, "<", 64, new Vector2(0.04f, 0.36f), new Vector2(0.19f, 0.71f), ShiftSlotLeft);
            var right = CreateButton("NextSlot", root, ">", 64, new Vector2(0.81f, 0.36f), new Vector2(0.96f, 0.71f), ShiftSlotRight);
            left.image.color = new Color(0.08f, 0.14f, 0.15f, 1f);
            right.image.color = new Color(0.08f, 0.14f, 0.15f, 1f);

            _cardButton = CreateButton("CharacterSlotCard", root, string.Empty, 34, new Vector2(0.2f, 0.3f), new Vector2(0.8f, 0.73f), ActivateSelectedSlot);
            _cardButton.image.color = _panel;
            var outline = _cardButton.gameObject.AddComponent<Outline>();
            outline.effectColor = _accent;
            outline.effectDistance = new Vector2(4f, -4f);
            _cardText = _cardButton.GetComponentInChildren<Text>();
            _cardText.alignment = TextAnchor.MiddleCenter;

            var controls = CreateText("Controls", root, "Tap a slot to create, start, or resume", 25, TextAnchor.MiddleCenter, new Color(0.56f, 0.68f, 0.67f));
            SetRect(controls.rectTransform, new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.26f));
            return root;
        }

        private RectTransform BuildCreationScreen(Transform parent)
        {
            var root = CreateScreen("CharacterCreation", parent);
            var title = CreateText("Title", root, "CREATE CHARACTER", 52, TextAnchor.MiddleCenter, _accent);
            SetRect(title.rectTransform, new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.93f));
            var classPanel = CreatePanel("ClassCard", root, _panel);
            SetRect(classPanel.rectTransform, new Vector2(0.12f, 0.48f), new Vector2(0.88f, 0.79f));
            var classTitle = CreateText("ClassName", classPanel.transform, "WAYFARER", 44, TextAnchor.MiddleCenter, _warm);
            SetRect(classTitle.rectTransform, new Vector2(0.08f, 0.68f), new Vector2(0.92f, 0.94f));
            var classCopy = CreateText("ClassCopy", classPanel.transform,
                $"Placeholder class\n\n{configuration.BaseHealth} Health  •  {configuration.BaseMana} Mana\n{configuration.BaseMeleeDamage} Melee Damage\n\nMove cardinally and bump enemies to attack.",
                29, TextAnchor.MiddleCenter, Color.white);
            SetRect(classCopy.rectTransform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.69f));

            _nameInput = CreateInputField("CharacterName", root, "Character name");
            SetRect(_nameInput.GetComponent<RectTransform>(), new Vector2(0.12f, 0.36f), new Vector2(0.88f, 0.44f));
            CreateButton("Create", root, "CREATE", 34, new Vector2(0.12f, 0.23f), new Vector2(0.88f, 0.32f), CreateCharacter);
            CreateButton("Back", root, "BACK", 30, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.2f), ShowCharacterSelection);
            return root;
        }

        private RectTransform BuildGameplayScreen(Transform parent)
        {
            var root = CreateScreen("Gameplay", parent);
            var inputSurface = CreatePanel("DynamicJoystickSurface", root, new Color(0f, 0f, 0f, 0f));
            Stretch(inputSurface.rectTransform);
            inputSurface.raycastTarget = true;

            var ring = CreatePanel("JoystickRing", inputSurface.transform, new Color(0.7f, 0.9f, 0.83f, 0.2f));
            ring.raycastTarget = false;
            ring.sprite = CreateCircleSprite(true);
            ring.preserveAspect = true;
            ring.rectTransform.sizeDelta = new Vector2(190f, 190f);
            ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            var knob = CreatePanel("JoystickKnob", ring.transform, new Color(0.35f, 1f, 0.72f, 0.55f));
            knob.raycastTarget = false;
            knob.sprite = CreateCircleSprite(false);
            knob.preserveAspect = true;
            knob.rectTransform.sizeDelta = new Vector2(72f, 72f);
            knob.rectTransform.anchorMin = knob.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            knob.rectTransform.anchoredPosition = Vector2.zero;
            _floatingJoystick = inputSurface.gameObject.AddComponent<FloatingJoystickInput>();
            _floatingJoystick.Configure(configuration, ring.rectTransform, knob.rectTransform);
            _floatingJoystick.ActionRequested += HandlePlayerAction;

            var hud = CreatePanel("HUD", root, new Color(0.02f, 0.04f, 0.045f, 0.92f));
            SetRect(hud.rectTransform, new Vector2(0.025f, 0.865f), new Vector2(0.975f, 0.985f));
            hud.raycastTarget = false;
            _healthText = CreateText("Health", hud.transform, "HP", 29, TextAnchor.MiddleLeft, new Color(1f, 0.43f, 0.42f));
            SetRect(_healthText.rectTransform, new Vector2(0.03f, 0.5f), new Vector2(0.38f, 0.95f));
            _manaText = CreateText("Mana", hud.transform, "MP", 29, TextAnchor.MiddleLeft, new Color(0.42f, 0.7f, 1f));
            SetRect(_manaText.rectTransform, new Vector2(0.03f, 0.05f), new Vector2(0.38f, 0.5f));
            _progressText = CreateText("Progress", hud.transform, "0 STEPS", 32, TextAnchor.MiddleRight, _warm);
            SetRect(_progressText.rectTransform, new Vector2(0.55f, 0.5f), new Vector2(0.97f, 0.95f));
            _levelText = CreateText("Level", hud.transform, "LV 1", 25, TextAnchor.MiddleRight, Color.white);
            SetRect(_levelText.rectTransform, new Vector2(0.45f, 0.05f), new Vector2(0.97f, 0.5f));

            _messageText = CreateText("Message", root, "Drag anywhere to move • tap to wait", 25, TextAnchor.MiddleCenter, new Color(0.86f, 0.91f, 0.89f));
            SetRect(_messageText.rectTransform, new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.085f));
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
            RefreshCharacterCard();
        }

        private void ShiftSlotLeft()
        {
            _selectedSlot = (_selectedSlot + CharacterRosterData.SlotCount - 1) % CharacterRosterData.SlotCount;
            RefreshCharacterCard();
        }

        private void ShiftSlotRight()
        {
            _selectedSlot = (_selectedSlot + 1) % CharacterRosterData.SlotCount;
            RefreshCharacterCard();
        }

        private void RefreshCharacterCard()
        {
            if (_slotCounter == null)
            {
                return;
            }

            _slotCounter.text = $"{_selectedSlot + 1} / {CharacterRosterData.SlotCount}";
            var character = _roster.slots[_selectedSlot].character;
            if (character == null)
            {
                _cardText.text = "+\n\nEMPTY SLOT\n\nCREATE CHARACTER";
                _cardButton.image.color = new Color(0.06f, 0.12f, 0.12f, 1f);
                return;
            }

            var action = character.HasSavedAdventure ? $"RESUME ADVENTURE  •  {character.activeAdventure.progress} STEPS" : "START NEW ADVENTURE";
            _cardText.text = $"{character.displayName.ToUpperInvariant()}\n\n{character.classId.ToUpperInvariant()}\nLEVEL {character.level}\nHP {character.maxHealth}  •  MP {character.maxMana}\nDAMAGE {character.meleeDamage}\nBEST {character.bestProgress} STEPS\n\n{action}";
            _cardButton.image.color = _panel;
        }

        private void ActivateSelectedSlot()
        {
            var character = _roster.slots[_selectedSlot].character;
            if (character == null)
            {
                _selectionScreen.SetActive(false);
                _creationScreen.SetActive(true);
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
            _session.MessageRaised += ShowMessage;

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
            ShowMessage(savedAdventure == null ? "Drag to move • tap to wait • bonfire at 100" : "Resumed saved adventure");
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
            ShowMessage($"LEVEL {level} • permanent stats increased");
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
            ShowMessage("Adventure continues. Death will end this active run.");
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

            _healthText.text = $"HP  {_session.Health} / {_activeCharacter.maxHealth}";
            _manaText.text = $"MP  {_session.Mana} / {_activeCharacter.maxMana}";
            _progressText.text = $"{_session.Progress} STEPS";
            _levelText.text = $"LV {_activeCharacter.level}  •  XP {_activeCharacter.experience}/{_activeCharacter.ExperienceToNextLevel}  •  DMG {_activeCharacter.meleeDamage}";
        }

        private void ShowMessage(string message)
        {
            if (_messageText != null)
            {
                _messageText.text = message;
            }
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
                _session.MessageRaised -= ShowMessage;
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

        private Image CreatePanel(string name, Transform parent, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            return image;
        }

        private Text CreateText(string name, Transform parent, string value, int size, TextAnchor alignment, Color color)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = _font;
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private Button CreateButton(string name, Transform parent, string label, int size, Vector2 min, Vector2 max, Action clicked)
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

        private InputField CreateInputField(string name, Transform parent, string placeholderText)
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

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rect, Vector2? offsetMin = null, Vector2? offsetMax = null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin ?? Vector2.zero;
            rect.offsetMax = offsetMax ?? Vector2.zero;
            rect.localScale = Vector3.one;
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
