using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Minge2026Spring.Scripts.Input
{
    /// <summary>
    /// Creates one persistent Input System virtual mouse for all Unity scenes.
    /// The Siv3D process is intentionally outside this component's scope.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GamepadVirtualCursor : MonoBehaviour
    {
        private const string GameObjectName = "Gamepad Virtual Cursor";
        private const string VirtualCursorMapName = "VirtualCursor";
        private const float StickActivityThreshold = 0.0001f;
        private const float MouseActivityThreshold = 0.01f;
        private const float AccelerationDuration = 0.75f;
        private const float MaximumSpeedMultiplier = 2.2f;
        private const int CursorWidth = 24;
        private const int CursorHeight = 32;

        private static GamepadVirtualCursor instance;

        private VirtualMouseInput virtualMouseInput;
        private GameObject virtualMouseObject;
        private InputAction moveAction;
        private InputAction clickAction;
        private InputAction scrollAction;
        private Mouse systemMouse;
        private Image cursorImage;
        private RectTransform cursorTransform;
        private Texture2D cursorTexture;
        private Sprite cursorSprite;
        private bool initialized;
        private bool gamepadCursorActive;
        private float moveHeldDuration;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
            {
                return;
            }

            var existing = FindFirstObjectByType<GamepadVirtualCursor>(FindObjectsInactive.Include);
            if (existing != null)
            {
                instance = existing;
                return;
            }

            new GameObject(GameObjectName).AddComponent<GamepadVirtualCursor>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;
            InputSystem.onDeviceChange += OnDeviceChange;

            TryInitialize();
        }

        private void Update()
        {
            if (!initialized)
            {
                TryInitialize();
                return;
            }

            RefreshSystemMouse();
            UpdateCursorSpeed();

            if (HasSystemMouseActivity())
            {
                ActivateSystemMouse();
            }
            else if (HasGamepadCursorActivity())
            {
                ActivateGamepadCursor();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                Cursor.visible = true;
                return;
            }

            Cursor.visible = !gamepadCursorActive;
        }

        private void OnDestroy()
        {
            if (instance != this)
            {
                return;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
            InputSystem.onDeviceChange -= OnDeviceChange;

            Cursor.visible = true;
            instance = null;

            if (cursorSprite != null)
            {
                Destroy(cursorSprite);
            }

            if (cursorTexture != null)
            {
                Destroy(cursorTexture);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!initialized)
            {
                TryInitialize();
                return;
            }

            ConfigureSceneInputModules();
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (!initialized)
            {
                return;
            }

            switch (change)
            {
                case InputDeviceChange.Added:
                case InputDeviceChange.Reconnected:
                case InputDeviceChange.Removed:
                case InputDeviceChange.Disconnected:
                    RefreshSystemMouse();
                    UpdateGamepadAvailability();
                    break;
            }
        }

        private void TryInitialize()
        {
            var actions = InputSystem.actions;
            if (actions == null)
            {
                return;
            }

            var virtualCursorMap = actions.FindActionMap(VirtualCursorMapName);
            var uiMap = actions.FindActionMap("UI");
            if (virtualCursorMap == null || uiMap == null || !HasRequiredUiActions(uiMap))
            {
                Debug.LogError(
                    "GamepadVirtualCursor requires the UI and VirtualCursor maps in the project-wide Input Actions asset.");
                enabled = false;
                return;
            }

            moveAction = virtualCursorMap.FindAction("Move", true);
            clickAction = virtualCursorMap.FindAction("Click", true);
            scrollAction = virtualCursorMap.FindAction("Scroll", true);

            CreateCursorVisual();
            CreateVirtualMouse();

            initialized = true;
            RefreshSystemMouse();
            UpdateGamepadAvailability();
            ConfigureSceneInputModules();

            if (systemMouse != null)
            {
                ActivateSystemMouse();
            }
            else
            {
                ActivateGamepadCursor();
            }
        }

        private static bool HasRequiredUiActions(InputActionMap uiMap)
        {
            return uiMap.FindAction("Navigate") != null
                   && uiMap.FindAction("Submit") != null
                   && uiMap.FindAction("Cancel") != null
                   && uiMap.FindAction("Point") != null
                   && uiMap.FindAction("Click") != null
                   && uiMap.FindAction("RightClick") != null
                   && uiMap.FindAction("MiddleClick") != null
                   && uiMap.FindAction("ScrollWheel") != null
                   && uiMap.FindAction("TrackedDevicePosition") != null
                   && uiMap.FindAction("TrackedDeviceOrientation") != null;
        }

        private void CreateCursorVisual()
        {
            var canvasObject = new GameObject("Cursor Canvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            var cursorObject = new GameObject("Cursor", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cursorObject.transform.SetParent(canvasObject.transform, false);

            cursorTransform = cursorObject.GetComponent<RectTransform>();
            cursorTransform.anchorMin = Vector2.zero;
            cursorTransform.anchorMax = Vector2.zero;
            cursorTransform.pivot = new Vector2(0f, 1f);
            cursorTransform.sizeDelta = new Vector2(CursorWidth, CursorHeight);
            cursorTransform.anchoredPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            cursorSprite = CreateCursorSprite();
            cursorImage = cursorObject.GetComponent<Image>();
            cursorImage.sprite = cursorSprite;
            cursorImage.preserveAspect = true;
            cursorImage.raycastTarget = false;
            cursorImage.enabled = false;
        }

        private void CreateVirtualMouse()
        {
            virtualMouseObject = new GameObject("Virtual Mouse Input");
            virtualMouseObject.transform.SetParent(transform, false);
            virtualMouseObject.SetActive(false);

            virtualMouseInput = virtualMouseObject.AddComponent<VirtualMouseInput>();
            virtualMouseInput.cursorMode = VirtualMouseInput.CursorMode.SoftwareCursor;
            virtualMouseInput.cursorGraphic = cursorImage;
            virtualMouseInput.cursorTransform = cursorTransform;
            virtualMouseInput.cursorSpeed = GetBaseCursorSpeed();
            virtualMouseInput.scrollSpeed = 6f;
            virtualMouseInput.stickAction = new InputActionProperty(moveAction);
            virtualMouseInput.leftButtonAction = new InputActionProperty(clickAction);
            virtualMouseInput.scrollWheelAction = new InputActionProperty(scrollAction);
        }

        private void ConfigureSceneInputModules()
        {
            var actions = InputSystem.actions;
            var modules = FindObjectsByType<InputSystemUIInputModule>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (var module in modules)
            {
                if (module.actionsAsset != actions)
                {
                    // The scenes use Input System's package default asset. Swapping to the
                    // project-wide asset remaps the identically named UI actions and keeps
                    // both sticks reserved for the virtual cursor.
                    module.actionsAsset = actions;
                }
            }
        }

        private void UpdateGamepadAvailability()
        {
            var hasGamepad = Gamepad.all.Count > 0;
            if (virtualMouseObject.activeSelf != hasGamepad)
            {
                virtualMouseObject.SetActive(hasGamepad);
            }

            if (!hasGamepad)
            {
                ActivateSystemMouse();
            }
            else if (systemMouse != null)
            {
                SyncVirtualCursorToSystemMouse();
            }
        }

        private void RefreshSystemMouse()
        {
            if (systemMouse != null && systemMouse.added && systemMouse.native)
            {
                return;
            }

            systemMouse = null;
            foreach (var device in InputSystem.devices)
            {
                if (device is Mouse mouse && mouse.native)
                {
                    systemMouse = mouse;
                    break;
                }
            }
        }

        private bool HasSystemMouseActivity()
        {
            if (systemMouse == null || !systemMouse.added || !systemMouse.enabled)
            {
                return false;
            }

            return systemMouse.delta.ReadValue().sqrMagnitude > MouseActivityThreshold
                   || systemMouse.scroll.ReadValue().sqrMagnitude > MouseActivityThreshold
                   || systemMouse.leftButton.wasPressedThisFrame
                   || systemMouse.leftButton.wasReleasedThisFrame
                   || systemMouse.rightButton.wasPressedThisFrame
                   || systemMouse.rightButton.wasReleasedThisFrame
                   || systemMouse.middleButton.wasPressedThisFrame
                   || systemMouse.middleButton.wasReleasedThisFrame;
        }

        private bool HasGamepadCursorActivity()
        {
            if (!virtualMouseObject.activeSelf)
            {
                return false;
            }

            return moveAction.ReadValue<Vector2>().sqrMagnitude > StickActivityThreshold
                   || scrollAction.ReadValue<Vector2>().sqrMagnitude > StickActivityThreshold
                   || clickAction.WasPressedThisFrame()
                   || clickAction.WasReleasedThisFrame();
        }

        private void ActivateSystemMouse()
        {
            if (systemMouse != null && virtualMouseObject.activeSelf)
            {
                SyncVirtualCursorToSystemMouse();
            }

            gamepadCursorActive = false;
            cursorImage.enabled = false;
            Cursor.visible = true;
        }

        private void ActivateGamepadCursor()
        {
            if (!virtualMouseObject.activeSelf)
            {
                return;
            }

            if (!gamepadCursorActive && systemMouse != null)
            {
                SyncVirtualCursorToSystemMouse();
            }

            gamepadCursorActive = true;
            cursorImage.enabled = true;
            Cursor.visible = false;
        }

        private void SyncVirtualCursorToSystemMouse()
        {
            var virtualMouse = virtualMouseInput.virtualMouse;
            if (systemMouse == null || virtualMouse == null || !virtualMouse.added)
            {
                return;
            }

            var position = systemMouse.position.ReadValue();
            position.x = Mathf.Clamp(position.x, 0f, Screen.width);
            position.y = Mathf.Clamp(position.y, 0f, Screen.height);

            InputState.Change(virtualMouse.position, position);
            cursorTransform.anchoredPosition = position;
        }

        private void UpdateCursorSpeed()
        {
            var move = moveAction.ReadValue<Vector2>();
            if (move.sqrMagnitude <= StickActivityThreshold)
            {
                moveHeldDuration = 0f;
                virtualMouseInput.cursorSpeed = GetBaseCursorSpeed();
                return;
            }

            moveHeldDuration += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(moveHeldDuration / AccelerationDuration);
            progress = progress * progress * (3f - 2f * progress);

            var baseSpeed = GetBaseCursorSpeed();
            virtualMouseInput.cursorSpeed = Mathf.Lerp(baseSpeed, baseSpeed * MaximumSpeedMultiplier, progress);
        }

        private static float GetBaseCursorSpeed()
        {
            return Mathf.Max(640f, Mathf.Min(Screen.width, Screen.height) * 0.65f);
        }

        private Sprite CreateCursorSprite()
        {
            cursorTexture = new Texture2D(CursorWidth, CursorHeight, TextureFormat.RGBA32, false)
            {
                name = "Runtime Gamepad Cursor",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var polygon = new[]
            {
                new Vector2(1f, 31f),
                new Vector2(1f, 4f),
                new Vector2(8f, 11f),
                new Vector2(12f, 2f),
                new Vector2(16f, 4f),
                new Vector2(12f, 13f),
                new Vector2(22f, 13f)
            };

            var pixels = new Color32[CursorWidth * CursorHeight];
            var transparent = new Color32(0, 0, 0, 0);
            var outline = new Color32(20, 20, 20, 255);
            var fill = new Color32(255, 255, 255, 255);

            for (var y = 0; y < CursorHeight; y++)
            {
                for (var x = 0; x < CursorWidth; x++)
                {
                    var point = new Vector2(x + 0.5f, y + 0.5f);
                    if (!IsInsidePolygon(point, polygon))
                    {
                        pixels[y * CursorWidth + x] = transparent;
                        continue;
                    }

                    var isOutline = !IsInsidePolygon(point + Vector2.left, polygon)
                                    || !IsInsidePolygon(point + Vector2.right, polygon)
                                    || !IsInsidePolygon(point + Vector2.up, polygon)
                                    || !IsInsidePolygon(point + Vector2.down, polygon);
                    pixels[y * CursorWidth + x] = isOutline ? outline : fill;
                }
            }

            cursorTexture.SetPixels32(pixels);
            cursorTexture.Apply(false, true);

            return Sprite.Create(
                cursorTexture,
                new Rect(0f, 0f, CursorWidth, CursorHeight),
                new Vector2(0f, 1f),
                100f,
                0,
                SpriteMeshType.FullRect);
        }

        private static bool IsInsidePolygon(Vector2 point, Vector2[] polygon)
        {
            var inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                var current = polygon[i];
                var previous = polygon[j];
                var crosses = (current.y > point.y) != (previous.y > point.y)
                              && point.x < (previous.x - current.x) * (point.y - current.y)
                              / (previous.y - current.y) + current.x;
                if (crosses)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}
