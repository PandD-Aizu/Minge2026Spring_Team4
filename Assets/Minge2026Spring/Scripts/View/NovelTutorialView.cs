using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace Minge2026Spring.Scripts.View
{
    /// <summary>
    /// ノベルシーンの初回操作ガイドをAddressables Prefabから表示するView。
    /// </summary>
    public sealed class NovelTutorialView : MonoBehaviour
    {
        private const string TutorialPrefabAddress = "UITutorial";
        private const float FadeDuration = 0.2f;

        [SerializeField]
        [Tooltip("チュートリアル吹き出しで使用するフォント。未指定時はPrefabのフォントを使用")]
        private TMP_FontAsset tutorialFont;

        private static readonly Color32 DiscordBubbleColor = new(47, 49, 54, 255);
        private static readonly Color32 DiscordTextColor = new(242, 243, 245, 255);

        private RectTransform _root;
        private AsyncOperationHandle<GameObject> _prefabHandle;

        private void Start()
        {
            _root = GetOrCreateDynamicCanvas();
            _prefabHandle = Addressables.LoadAssetAsync<GameObject>(TutorialPrefabAddress);
            _prefabHandle.Completed += OnTutorialPrefabLoaded;
        }

        private RectTransform GetOrCreateDynamicCanvas()
        {
            var dynamicCanvasObject = GameObject.Find("DynamicCanvas");
            var canvasParent = GameObject.Find("HUD_Dynamic");
            if (dynamicCanvasObject is null)
            {
                var parentTransform = canvasParent is not null ? canvasParent.transform : transform.parent;
                dynamicCanvasObject = new GameObject("DynamicCanvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
                dynamicCanvasObject.transform.SetParent(parentTransform, false);
            }

            var dynamicCanvasRect = dynamicCanvasObject.GetComponent<RectTransform>();
            dynamicCanvasRect.anchorMin = Vector2.zero;
            dynamicCanvasRect.anchorMax = Vector2.one;
            dynamicCanvasRect.offsetMin = Vector2.zero;
            dynamicCanvasRect.offsetMax = Vector2.zero;

            var dynamicCanvas = dynamicCanvasObject.GetComponent<Canvas>()
                ?? dynamicCanvasObject.AddComponent<Canvas>();
            if (dynamicCanvasObject.GetComponent<GraphicRaycaster>() is null)
                dynamicCanvasObject.AddComponent<GraphicRaycaster>();
            var parentCanvas = canvasParent?.GetComponent<Canvas>()
                ?? dynamicCanvasObject.transform.parent?.GetComponentInParent<Canvas>();
            if (parentCanvas is not null && dynamicCanvas != parentCanvas)
            {
                dynamicCanvas.renderMode = parentCanvas.renderMode;
                dynamicCanvas.worldCamera = parentCanvas.worldCamera;
                dynamicCanvas.overrideSorting = true;
                dynamicCanvas.sortingLayerName = "HUDDynamic";
                dynamicCanvas.sortingOrder = parentCanvas.sortingOrder + 1;
            }
            else if (dynamicCanvas.renderMode == RenderMode.WorldSpace)
            {
                dynamicCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            return dynamicCanvasRect;
        }

        private void OnTutorialPrefabLoaded(AsyncOperationHandle<GameObject> handle)
        {
            if (!IsValid(handle))
                return;

            CreateTutorial("DMButton", "ここを押すと\nDM画面へ！", new Vector2(0.25f, 0.90f), true);
            CreateTutorial("MenuButton", "ここを押すと\nオプション画面へ！", new Vector2(0.30f, 0.17f), false);
        }

        private bool IsValid(AsyncOperationHandle<GameObject> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result is not null)
                return true;

            Debug.LogError($"[NovelTutorialView] Failed to instantiate Addressable prefab: {TutorialPrefabAddress}", this);
            return false;
        }

        private void CreateTutorial(string targetName, string message,
            Vector2 normalizedPosition, bool flipHukidashi)
        {
            if (GameObject.Find(targetName) is null)
            {
                Debug.LogWarning($"[NovelTutorialView] Target button was not found: {targetName}", this);
                return;
            }

            var bubble = Instantiate(_prefabHandle.Result, _root, false);
            bubble.name = $"Tutorial_{targetName}";
            bubble.SetActive(false);

            var bubbleRect = bubble.GetComponent<RectTransform>();
            bubble.transform.SetParent(_root, false);
            bubbleRect.anchorMin = normalizedPosition;
            bubbleRect.anchorMax = normalizedPosition;
            bubbleRect.pivot = new Vector2(0.5f, 0.5f);
            bubbleRect.sizeDelta = new Vector2(500f, 150f);
            bubbleRect.anchoredPosition = Vector2.zero;
            bubbleRect.localScale = flipHukidashi ? new Vector3(1f, -1f, 1f) : Vector3.one;

            var canvasGroup = bubble.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            var image = bubble.GetComponentInChildren<Image>(true);
            image.rectTransform.sizeDelta = new Vector2(500f, 150f);
            image.color = DiscordBubbleColor;
            image.raycastTarget = true;

            var label = bubble.GetComponentInChildren<TextMeshProUGUI>(true);
            label.text = message;
            label.font = tutorialFont is not null ? tutorialFont : label.font;
            label.color = DiscordTextColor;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            if (flipHukidashi)
                label.transform.localScale = new Vector3(1f, -1f, 1f);

            var dismiss = bubble.AddComponent<DismissOnPointerEnter>();
            dismiss.Initialize(canvasGroup);
            bubble.SetActive(true);
            StartCoroutine(FadeIn(canvasGroup));
        }

        private static IEnumerator FadeIn(CanvasGroup canvasGroup)
        {
            for (var elapsed = 0f; elapsed < FadeDuration; elapsed += Time.unscaledDeltaTime)
            {
                canvasGroup.alpha = Mathf.Clamp01(elapsed / FadeDuration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        private void OnDestroy()
        {
            if (_prefabHandle.IsValid())
                Addressables.Release(_prefabHandle);
        }

        private sealed class DismissOnPointerEnter : MonoBehaviour, IPointerEnterHandler
        {
            private CanvasGroup _canvasGroup;
            private bool _isDismissing;

            public void Initialize(CanvasGroup canvasGroup)
            {
                _canvasGroup = canvasGroup;
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (_isDismissing)
                    return;

                _isDismissing = true;
                _canvasGroup.blocksRaycasts = false;
                StartCoroutine(FadeOutAndDestroy());
            }

            private IEnumerator FadeOutAndDestroy()
            {
                var startAlpha = _canvasGroup.alpha;
                for (var elapsed = 0f; elapsed < FadeDuration; elapsed += Time.unscaledDeltaTime)
                {
                    _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / FadeDuration);
                    yield return null;
                }

                Destroy(gameObject);
            }
        }
    }
}
