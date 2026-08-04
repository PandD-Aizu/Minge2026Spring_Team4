using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Minge2026Spring.Scripts.Application.DTOs;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Minge2026Spring.Scripts.View
{
    public class ChoiceUIView : MonoBehaviour
    {
        private const float MaxButtonWidth = 440f;
        private const float ButtonHeight = 72f;
        private const float HorizontalTextPadding = 16f;
        private const float MaxFontSize = 18f;
        private const float MinFontSize = 14f;

        [Header("GameSettings")]
        [SerializeField] private Transform buttonParentObj;

        [Header("SpriteSettings")] 
        [SerializeField] private AssetReference choiceButtonSprite;

        private Sprite _choiceButtonSprite;
        
        /// <summary>
        /// 選択肢のデータをセットする
        /// </summary>
        /// <param name="choices">選択肢データ</param>
        /// <param name="onChoiceSelected">選択時コールバック</param>
        public async UniTask SetData(
            Choice[] choices,
            Action<int> onChoiceSelected = null,
            CancellationToken token = default)
        {
            try
            {
                await LoadAsset(token);
                token.ThrowIfCancellationRequested();

                if (buttonParentObj is null)
                {
                    Debug.LogError("ChoiceUIView.buttonParentObj が設定されていません。", this);
                    return;
                }

                ClearButtons();

                if (choices is null || choices.Length == 0)
                    return;

                var buttonWidth = CalculateButtonWidth(choices.Length);

                // 全ボタンの生成完了まで待ち、画面切替直後でも分岐UIを確実に表示する
                for (int i = 0; i < choices.Length; i++)
                    await CreateChoiceButton(choices[i], i, buttonWidth, onChoiceSelected, token);
            }
            finally
            {
                ReleaseAsset();
            }
        }

        private void ClearButtons()
        {
            for (int i = buttonParentObj.childCount - 1; i >= 0; i--)
            {
                Destroy(buttonParentObj.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// 選択肢ボタンを作成する
        /// </summary>
        /// <param name="choice">選択肢データ</param>
        /// <param name="choiceIndex">選択肢のインデックス</param>
        /// <param name="onChoiceSelected">選択肢を押したときの処理</param>
        private async UniTask CreateChoiceButton(
            Choice choice,
            int choiceIndex,
            float buttonWidth,
            Action<int> onChoiceSelected,
            CancellationToken token)
        {
            var buttonObj = new GameObject($"ChoiceButton_{choiceIndex}",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObj.transform.SetParent(buttonParentObj, false);

            var layoutElement = buttonObj.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = buttonWidth;
            layoutElement.preferredHeight = ButtonHeight;

            var image = buttonObj.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.95f);
            image.sprite = _choiceButtonSprite;

            var button = buttonObj.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.95f);
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            var textObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(buttonObj.transform, false);

            var textRect = (RectTransform)textObj.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(HorizontalTextPadding, 6f);
            textRect.offsetMax = new Vector2(-HorizontalTextPadding, -6f);

            var textComponent = textObj.GetComponent<TextMeshProUGUI>();
            var fontHandle = Addressables.LoadAssetAsync<TMP_FontAsset>("NotoSans_Regular");
            try
            {
                await fontHandle.ToUniTask(cancellationToken: token);
                textComponent.font = fontHandle.Result;
                textComponent.text = choice.choiceText;
                textComponent.fontSize = MaxFontSize;
                textComponent.enableAutoSizing = true;
                textComponent.fontSizeMin = MinFontSize;
                textComponent.fontSizeMax = MaxFontSize;
                textComponent.color = Color.white;
                textComponent.alignment = TextAlignmentOptions.Center;
                textComponent.textWrappingMode = TextWrappingModes.Normal;
                textComponent.overflowMode = TextOverflowModes.Ellipsis;
            }
            finally
            {
                if (fontHandle.IsValid())
                    Addressables.Release(fontHandle);
            }

            button.onClick.AddListener(() =>
            {
                SetButtonsInteractable(false);
                onChoiceSelected?.Invoke(choiceIndex);
            });
        }

        private float CalculateButtonWidth(int choiceCount)
        {
            var parentRect = buttonParentObj as RectTransform;
            var availableWidth = parentRect != null ? parentRect.rect.width : 0f;
            if (availableWidth <= 0f && parentRect != null)
                availableWidth = parentRect.sizeDelta.x;

            if (availableWidth <= 0f)
                return MaxButtonWidth;

            var layoutGroup = buttonParentObj.GetComponent<HorizontalLayoutGroup>();
            var spacing = layoutGroup != null ? layoutGroup.spacing : 0f;
            var horizontalPadding = layoutGroup != null
                ? layoutGroup.padding.left + layoutGroup.padding.right
                : 0f;
            var usableWidth = availableWidth - horizontalPadding - spacing * (choiceCount - 1);

            return Mathf.Min(MaxButtonWidth, usableWidth / choiceCount);
        }

        /// <summary>
        /// ボタンの操作可否を設定
        /// </summary>
        /// <param name="isInteractable">true: 操作可能</param>
        private void SetButtonsInteractable(bool isInteractable)
        {
            foreach (Transform child in buttonParentObj)
            {
                if (child.TryGetComponent<Button>(out var button))
                    button.interactable = isInteractable;
            }
        }

        private async UniTask LoadAsset(CancellationToken token)
        { 
            if (choiceButtonSprite != null)
            {
                var handle = choiceButtonSprite.LoadAssetAsync<Sprite>();
                await handle.ToUniTask(cancellationToken: token);
                _choiceButtonSprite = handle.Result;
            }
        }

        private void ReleaseAsset()
        {
            if (choiceButtonSprite != null)
                choiceButtonSprite.ReleaseAsset();
        }
    }
}
