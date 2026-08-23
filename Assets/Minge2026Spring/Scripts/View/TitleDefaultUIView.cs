using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;

namespace Minge2026Spring.Scripts.View
{
    public class TitleDefaultUIView : MonoBehaviour
    {
        [SerializeField] public Button StartButton;
        [SerializeField] public Button OptionButton;
        [SerializeField] public Button AchievementButton;
        [SerializeField] public Button ExitButton;

        [Header("Version Display")]
        [SerializeField, Min(1f)] private float versionFontSize = 16f;
        [SerializeField, Range(0f, 1f)] private float versionAlpha = 0.65f;
        [SerializeField] private TMP_FontAsset versionFont;

        private void Awake()
        {
            CreateVersionLabelAsync().Forget();
        }

        private async UniTaskVoid CreateVersionLabelAsync()
        {
            var canvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .OrderByDescending(candidate => candidate.sortingOrder)
                .FirstOrDefault();

            if (canvas == null)
            {
                Debug.LogWarning("[TitleDefaultUIView] Version label could not find a Canvas.");
                return;
            }

            var fontAsset = versionFont;
            if (fontAsset == null)
            {
                var fontHandle = Addressables.LoadAssetAsync<TMP_FontAsset>("NotoSans_Regular");
                try
                {
                    await fontHandle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
                    fontAsset = fontHandle.Result;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[TitleDefaultUIView] Failed to load font 'NotoSans_Regular': {ex.Message}");
                }
            }

            var labelObject = new GameObject("VersionLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.layer = canvas.gameObject.layer;
            labelObject.transform.SetParent(canvas.transform, false);

            var rectTransform = labelObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(0f, 0f);
            rectTransform.pivot = new Vector2(0f, 0f);
            rectTransform.anchoredPosition = new Vector2(24f, 18f);
            rectTransform.sizeDelta = new Vector2(220f, 32f);

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null)
            {
                label.font = fontAsset;
            }
            UILocalization.Bind(label, "Common", "version", UnityEngine.Application.version);
            label.fontSize = versionFontSize;
            label.color = new Color(1f, 1f, 1f, versionAlpha);
            label.alignment = TextAlignmentOptions.BottomLeft;
            label.raycastTarget = false;
        }
        
        /// <summary>
        /// タイトル画面のボタンのインタラクト状態を設定する
        /// </summary>
        /// <param name="interactable">インタラクト可能にするかどうか: true</param>
        public void SetInteractable(bool interactable)
        {
            if (StartButton != null)
                StartButton.interactable = interactable;
            if (OptionButton != null)
                OptionButton.interactable = interactable;
            if (AchievementButton != null)
                AchievementButton.interactable = interactable;
            if (ExitButton != null)
                ExitButton.interactable = interactable;
        }
    }
}
