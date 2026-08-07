using System;
using System.IO;
using System.Linq;
using OneStep.Bootstrap;
using OneStep.Core.Configuration;
using OneStep.Input;
using OneStep.Gameplay.Overworld;
using OneStep.Networking;
using OneStep.Platform;
using OneStep.Presentation.Camera;
using OneStep.Presentation.Diagnostics;
using OneStep.Services;
using OneStep.Editor.Build;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OneStep.Editor
{
    public static class FoundationProjectBuilder
    {
        private const string Root = "Assets/_Project";
        private const string SettingsPath = Root + "/Settings";
        private const string ScenesPath = Root + "/Scenes";
        private const string GeneratedPath = Root + "/Art/Generated";
        private const string ViewportPath = SettingsPath + "/ViewportConfiguration.asset";
        private const string ServicesPath = SettingsPath + "/ServicesConfiguration.asset";
        private const string InputPath = SettingsPath + "/Input/OneStepControls.inputactions";
        private const string BootstrapScenePath = ScenesPath + "/Bootstrap.unity";
        private const string TestScenePath = ScenesPath + "/FoundationTest.unity";
        private const string AdventureScenePath = ScenesPath + "/Adventure.unity";
        private const string AdventureConfigurationPath = SettingsPath + "/AdventureConfiguration.asset";
        private const string BuildSettingsPath = SettingsPath + "/Build";

        [MenuItem("Tools/OneStep/Build Foundation")]
        public static void BuildFoundation()
        {
            EnsureFolders();
            var viewport = LoadOrCreate<ViewportConfiguration>(ViewportPath);
            var services = LoadOrCreate<ServicesConfiguration>(ServicesPath);
            var adventure = LoadOrCreate<AdventureConfiguration>(AdventureConfigurationPath);
            adventure.EnsureDefaults();
            EditorUtility.SetDirty(adventure);
            var developmentBuild = LoadOrCreate<WebBuildConfiguration>(BuildSettingsPath + "/Development.asset");
            developmentBuild.Configure("Builds/Web/Development", true);
            EditorUtility.SetDirty(developmentBuild);
            var productionBuild = LoadOrCreate<WebBuildConfiguration>(BuildSettingsPath + "/Production.asset");
            productionBuild.Configure("Builds/Web/Production", false);
            EditorUtility.SetDirty(productionBuild);
            AssetDatabase.ImportAsset(InputPath, ImportAssetOptions.ForceUpdate);
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            if (inputActions == null)
            {
                throw new InvalidOperationException($"Input actions could not be loaded at {InputPath}.");
            }

            var gridSprite = CreateGridSprite(viewport);
            var markerSprite = CreateMarkerSprite();
            if (!File.Exists(BootstrapScenePath))
            {
                CreateBootstrapScene(services);
            }
            CreateAdventureScene(viewport, adventure);
            if (!File.Exists(TestScenePath))
            {
                CreateFoundationTestScene(viewport, inputActions, gridSprite, markerSprite);
            }
            ApplyProjectSettings();
            ConfigureBuildScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(TestScenePath);
            Debug.Log("OneStep foundation rebuilt successfully.");
        }

        private static void CreateBootstrapScene(ServicesConfiguration services)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Bootstrap";

            var root = new GameObject("_Application");
            var identity = root.AddComponent<UnityPlayerIdentityService>();
            identity.Configure(services);
            var duel = root.AddComponent<UnityMultiplayerDuelSessionService>();
            duel.Configure(services, identity);
            root.AddComponent<WebViewportMonitor>();

            var networkManager = root.AddComponent<NetworkManager>();
            var transport = root.AddComponent<UnityTransport>();
            networkManager.NetworkConfig.NetworkTransport = transport;
            networkManager.NetworkConfig.EnableSceneManagement = true;
            networkManager.RunInBackground = true;

            var bootstrap = root.AddComponent<AppBootstrap>();
            bootstrap.Configure(identity, duel, "Adventure");

            var cameraObject = new GameObject("BootstrapCamera", typeof(UnityEngine.Camera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.GetComponent<UnityEngine.Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.02f, 0.03f, 1f);
            camera.cullingMask = 0;

            var lightObject = new GameObject("BootstrapGlobalLight2D");
            var light = lightObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;

            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
        }

        private static void CreateAdventureScene(ViewportConfiguration viewport, AdventureConfiguration adventure)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Adventure";

            var letterboxObject = new GameObject("LetterboxCamera", typeof(UnityEngine.Camera));
            letterboxObject.transform.position = new Vector3(0f, 0f, -10f);
            var letterboxCamera = letterboxObject.GetComponent<UnityEngine.Camera>();
            letterboxCamera.clearFlags = CameraClearFlags.SolidColor;
            letterboxCamera.backgroundColor = viewport.LetterboxColor;
            letterboxCamera.cullingMask = 0;
            letterboxCamera.depth = -100f;

            var cameraObject = new GameObject("PortraitCamera", typeof(UnityEngine.Camera), typeof(UnityEngine.U2D.PixelPerfectCamera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3((adventure.WorldWidth - 1) * 0.5f, viewport.OrthographicSize - 0.5f, -10f);
            var camera = cameraObject.GetComponent<UnityEngine.Camera>();
            camera.orthographic = true;
            camera.orthographicSize = viewport.OrthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = viewport.LetterboxColor;
            camera.depth = 0f;
            var pixelCamera = cameraObject.GetComponent<UnityEngine.U2D.PixelPerfectCamera>();
            pixelCamera.assetsPPU = viewport.AssetsPixelsPerUnit;
            pixelCamera.refResolutionX = viewport.ReferenceWidth;
            pixelCamera.refResolutionY = viewport.ReferenceHeight;
            pixelCamera.cropFrameX = false;
            pixelCamera.cropFrameY = false;
            pixelCamera.pixelSnapping = true;
            pixelCamera.upscaleRT = false;
            var fixedViewport = cameraObject.AddComponent<FixedPortraitViewport>();
            fixedViewport.Configure(viewport);

            var lightObject = new GameObject("GlobalLight2D");
            var light = lightObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;

            var game = new GameObject("OneBitGame");
            var root = game.AddComponent<OneBitGameRoot>();
            root.Configure(adventure);
            game.AddComponent<WebViewportMonitor>();

            EditorSceneManager.SaveScene(scene, AdventureScenePath);
        }

        private static void CreateFoundationTestScene(
            ViewportConfiguration viewport,
            InputActionAsset inputActions,
            Sprite gridSprite,
            Sprite markerSprite)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "FoundationTest";

            var world = new GameObject("WorldReference");
            var grid = new GameObject("9x16_LogicalGrid");
            grid.transform.SetParent(world.transform, false);
            var gridRenderer = grid.AddComponent<SpriteRenderer>();
            gridRenderer.sprite = gridSprite;
            gridRenderer.sortingOrder = -10;
            grid.AddComponent<GridReferenceGizmo>();

            var marker = new GameObject("PlayerMarker_NoGameplay");
            marker.transform.SetParent(world.transform, false);
            marker.transform.position = new Vector3(0f, -2.5f, 0f);
            var markerRenderer = marker.AddComponent<SpriteRenderer>();
            markerRenderer.sprite = markerSprite;
            markerRenderer.sortingOrder = 10;

            var letterboxObject = new GameObject("LetterboxCamera", typeof(UnityEngine.Camera));
            letterboxObject.transform.position = new Vector3(0f, 0f, -10f);
            var letterboxCamera = letterboxObject.GetComponent<UnityEngine.Camera>();
            letterboxCamera.clearFlags = CameraClearFlags.SolidColor;
            letterboxCamera.backgroundColor = viewport.LetterboxColor;
            letterboxCamera.cullingMask = 0;
            letterboxCamera.depth = -100f;

            var cameraObject = new GameObject("PortraitPixelCamera", typeof(UnityEngine.Camera), typeof(UnityEngine.U2D.PixelPerfectCamera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.GetComponent<UnityEngine.Camera>();
            camera.orthographic = true;
            camera.orthographicSize = viewport.OrthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = viewport.LetterboxColor;
            camera.depth = 0f;
            var pixelCamera = cameraObject.GetComponent<UnityEngine.U2D.PixelPerfectCamera>();
            pixelCamera.assetsPPU = viewport.AssetsPixelsPerUnit;
            pixelCamera.refResolutionX = viewport.ReferenceWidth;
            pixelCamera.refResolutionY = viewport.ReferenceHeight;
            pixelCamera.cropFrameX = false;
            pixelCamera.cropFrameY = false;
            pixelCamera.pixelSnapping = true;
            pixelCamera.upscaleRT = false;
            var fixedViewport = cameraObject.AddComponent<FixedPortraitViewport>();
            fixedViewport.Configure(viewport);

            var lightObject = new GameObject("GlobalLight2D");
            var light = lightObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;

            var systems = new GameObject("FoundationSystems");
            systems.AddComponent<WebViewportMonitor>();
            var inputReader = systems.AddComponent<GameInputReader>();
            inputReader.Configure(inputActions);

            CreateEventSystem();
            CreateCanvas(inputReader);
            EditorSceneManager.SaveScene(scene, TestScenePath);
        }

        private static void CreateCanvas(GameInputReader inputReader)
        {
            var canvasObject = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var safeArea = CreateLayer("SafeArea", canvasObject.transform);
            safeArea.gameObject.AddComponent<SafeAreaFitter>();
            var safeImage = safeArea.gameObject.AddComponent<Image>();
            safeImage.color = new Color(0.1f, 0.9f, 0.65f, 0.006f);
            safeImage.raycastTarget = false;
            var safeOutline = safeArea.gameObject.AddComponent<Outline>();
            safeOutline.effectColor = new Color(0.25f, 1f, 0.75f, 0.55f);
            safeOutline.effectDistance = new Vector2(3f, -3f);

            var hud = CreateLayer("GameplayHUD", safeArea);
            var modal = CreateLayer("Modal", safeArea);
            var overlay = CreateLayer("Overlay", canvasObject.transform);
            var transition = CreateLayer("ScreenTransition", canvasObject.transform);

            var title = CreateText("Title", hud, "FOUNDATION  ·  9 × 16 LOGICAL GRID", 34, TextAnchor.UpperCenter);
            SetRect(title.rectTransform, new Vector2(0.05f, 0.91f), new Vector2(0.95f, 0.985f), Vector2.zero, Vector2.zero);
            title.color = new Color(0.85f, 1f, 0.94f, 1f);

            var inputText = CreateText("InputDiagnostics", overlay, "INPUT  Waiting for input", 27, TextAnchor.UpperLeft);
            SetRect(inputText.rectTransform, new Vector2(0.04f, 0.82f), new Vector2(0.96f, 0.91f), Vector2.zero, Vector2.zero);
            inputText.color = new Color(0.62f, 0.94f, 1f, 1f);
            var inputView = inputText.gameObject.AddComponent<InputDiagnosticsView>();
            inputView.Configure(inputReader, inputText);

            CreateDirectionPad(hud);
            CreateConnectionPanel(modal);

            var transitionImage = transition.gameObject.AddComponent<Image>();
            transitionImage.color = Color.black;
            transitionImage.raycastTarget = true;
            transition.gameObject.SetActive(false);
        }

        private static void CreateDirectionPad(Transform parent)
        {
            var root = CreateLayer("DPad_InputValidation", parent);
            SetRect((RectTransform)root, new Vector2(0.58f, 0.025f), new Vector2(0.96f, 0.26f), Vector2.zero, Vector2.zero);
            CreateOnScreenButton("Up", root, "▲", new Vector2(0.34f, 0.62f), new Vector2(0.66f, 0.98f), "<Gamepad>/dpad/up");
            CreateOnScreenButton("Down", root, "▼", new Vector2(0.34f, 0.02f), new Vector2(0.66f, 0.38f), "<Gamepad>/dpad/down");
            CreateOnScreenButton("Left", root, "◀", new Vector2(0.02f, 0.32f), new Vector2(0.34f, 0.68f), "<Gamepad>/dpad/left");
            CreateOnScreenButton("Right", root, "▶", new Vector2(0.66f, 0.32f), new Vector2(0.98f, 0.68f), "<Gamepad>/dpad/right");
            CreateOnScreenButton("Wait", root, "WAIT", new Vector2(0.37f, 0.39f), new Vector2(0.63f, 0.61f), "<Gamepad>/start");
        }

        private static void CreateConnectionPanel(Transform parent)
        {
            var panel = CreateLayer("MultiplayerConnectionTest", parent);
            SetRect((RectTransform)panel, new Vector2(0.035f, 0.025f), new Vector2(0.56f, 0.36f), Vector2.zero, Vector2.zero);
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.025f, 0.035f, 0.055f, 0.92f);

            var status = CreateText("Status", panel, "UGS / RELAY  Bootstrap required", 25, TextAnchor.UpperLeft);
            SetRect(status.rectTransform, new Vector2(0.06f, 0.56f), new Vector2(0.94f, 0.94f), Vector2.zero, Vector2.zero);
            status.color = new Color(0.75f, 0.95f, 1f, 1f);

            var input = CreateInputField("JoinCode", panel, "JOIN CODE");
            SetRect(input.GetComponent<RectTransform>(), new Vector2(0.06f, 0.38f), new Vector2(0.94f, 0.55f), Vector2.zero, Vector2.zero);

            var initialize = CreateButton("Initialize", panel, "INIT", new Vector2(0.06f, 0.08f), new Vector2(0.26f, 0.32f));
            var host = CreateButton("Host", panel, "HOST", new Vector2(0.28f, 0.08f), new Vector2(0.48f, 0.32f));
            var join = CreateButton("Join", panel, "JOIN", new Vector2(0.50f, 0.08f), new Vector2(0.70f, 0.32f));
            var leave = CreateButton("Leave", panel, "LEAVE", new Vector2(0.72f, 0.08f), new Vector2(0.94f, 0.32f));

            var connectionPanel = panel.gameObject.AddComponent<MultiplayerConnectionTestPanel>();
            connectionPanel.Configure(status, input, initialize, host, join, leave);
        }

        private static void CreateEventSystem()
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private static RectTransform CreateLayer(string name, Transform parent)
        {
            var value = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            value.SetParent(parent, false);
            SetRect(value, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return value;
        }

        private static Text CreateText(string name, Transform parent, string value, int size, TextAnchor alignment)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 min, Vector2 max)
        {
            var button = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)).GetComponent<Button>();
            button.transform.SetParent(parent, false);
            SetRect(button.GetComponent<RectTransform>(), min, max, Vector2.zero, Vector2.zero);
            button.GetComponent<Image>().color = new Color(0.12f, 0.22f, 0.28f, 1f);
            var text = CreateText("Label", button.transform, label, 24, TextAnchor.MiddleCenter);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            text.color = Color.white;
            return button;
        }

        private static void CreateOnScreenButton(string name, Transform parent, string label, Vector2 min, Vector2 max, string controlPath)
        {
            var button = CreateButton(name, parent, label, min, max);
            button.transition = Selectable.Transition.ColorTint;
            var onScreen = button.gameObject.AddComponent<OnScreenButton>();
            var serialized = new SerializedObject(onScreen);
            serialized.FindProperty("m_ControlPath").stringValue = controlPath;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static InputField CreateInputField(string name, Transform parent, string placeholder)
        {
            var input = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField)).GetComponent<InputField>();
            input.transform.SetParent(parent, false);
            input.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.16f, 1f);
            var text = CreateText("Text", input.transform, string.Empty, 26, TextAnchor.MiddleLeft);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(18f, 0f), new Vector2(-18f, 0f));
            text.color = Color.white;
            var hint = CreateText("Placeholder", input.transform, placeholder, 24, TextAnchor.MiddleLeft);
            SetRect(hint.rectTransform, Vector2.zero, Vector2.one, new Vector2(18f, 0f), new Vector2(-18f, 0f));
            hint.color = new Color(1f, 1f, 1f, 0.35f);
            input.textComponent = text;
            input.placeholder = hint;
            input.characterLimit = 16;
            return input;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }

        private static Sprite CreateGridSprite(ViewportConfiguration configuration)
        {
            var path = GeneratedPath + "/LogicalGrid.asset";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null && (!texture.isReadable || texture.width != configuration.ReferenceWidth || texture.height != configuration.ReferenceHeight))
            {
                AssetDatabase.DeleteAsset(path);
                texture = null;
            }

            if (texture == null)
            {
                texture = new Texture2D(configuration.ReferenceWidth, configuration.ReferenceHeight, TextureFormat.RGBA32, false)
                {
                    name = "LogicalGridTexture"
                };
                AssetDatabase.CreateAsset(texture, path);
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            var ppu = configuration.AssetsPixelsPerUnit;
            var pixels = new Color32[texture.width * texture.height];
            for (var y = 0; y < texture.height; y++)
            {
                for (var x = 0; x < texture.width; x++)
                {
                    var line = x % ppu == 0 || y % ppu == 0;
                    var alternate = ((x / ppu) + (y / ppu)) % 2 == 0;
                    pixels[y * texture.width + x] = line
                        ? new Color32(42, 105, 96, 255)
                        : alternate ? new Color32(12, 22, 29, 255) : new Color32(15, 27, 35, 255);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            EditorUtility.SetDirty(texture);
            var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            if (sprite == null)
            {
                sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), ppu);
                sprite.name = "LogicalGridSprite";
                AssetDatabase.AddObjectToAsset(sprite, texture);
            }
            return sprite;
        }

        private static Sprite CreateMarkerSprite()
        {
            var path = GeneratedPath + "/PlayerMarker.asset";
            const int size = 16;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null && !texture.isReadable)
            {
                AssetDatabase.DeleteAsset(path);
                texture = null;
            }
            if (texture == null)
            {
                texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    name = "PlayerMarkerTexture"
                };
                AssetDatabase.CreateAsset(texture, path);
            }
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var border = x is 1 or 14 || y is 1 or 14;
                    var center = x >= 5 && x <= 10 && y >= 4 && y <= 11;
                    pixels[y * size + x] = border || center
                        ? new Color32(235, 246, 221, 255)
                        : new Color32(36, 215, 163, 255);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            EditorUtility.SetDirty(texture);
            var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            if (sprite == null)
            {
                sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
                sprite.name = "PlayerMarkerSprite";
                AssetDatabase.AddObjectToAsset(sprite, texture);
            }
            return sprite;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolders()
        {
            var folders = new[]
            {
                Root, SettingsPath, SettingsPath + "/Input", BuildSettingsPath, ScenesPath, Root + "/Art", GeneratedPath,
                Root + "/Audio", Root + "/Prefabs", Root + "/Data", Root + "/Tests/EditMode", Root + "/Tests/PlayMode"
            };

            foreach (var folder in folders)
            {
                if (AssetDatabase.IsValidFolder(folder))
                {
                    continue;
                }

                var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
                var name = Path.GetFileName(folder);
                if (!string.IsNullOrEmpty(parent))
                {
                    AssetDatabase.CreateFolder(parent, name);
                }
            }
        }

        private static void ApplyProjectSettings()
        {
            PlayerSettings.companyName = "OneStep";
            PlayerSettings.productName = "OneStep";
            PlayerSettings.defaultScreenWidth = 720;
            PlayerSettings.defaultScreenHeight = 1280;
            PlayerSettings.defaultWebScreenWidth = 720;
            PlayerSettings.defaultWebScreenHeight = 1280;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.WebGL.template = "PROJECT:OneStepResponsive";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.threadsSupport = false;
            PlayerSettings.WebGL.initialMemorySize = 128;
            PlayerSettings.WebGL.maximumMemorySize = 512;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 0;
            QualitySettings.shadows = ShadowQuality.Disable;
        }

        private static void ConfigureBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(AdventureScenePath, true)
            };
        }
    }
}
