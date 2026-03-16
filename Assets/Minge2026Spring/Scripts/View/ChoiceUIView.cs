using System;
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
        [SerializeField] private Transform buttonParentObj;
        
        /// <summary>
        /// 選択肢のデータをセットする
        /// </summary>
        /// <param name="choices">選択肢データ</param>
        /// <param name="onChoiceSelected">選択時コールバック</param>
        public void SetData(Choice[] choices, Action<int> onChoiceSelected = null)
        {
            if (buttonParentObj is null)
            {
                Debug.LogError("ChoiceUIView.buttonParentObj が設定されていません。", this);
                return;
            }

            ClearButtons();

            if (choices is null || choices.Length == 0)
                return;

            for (int i = 0; i < choices.Length; i++)
            {
                CreateChoiceButton(choices[i], i, onChoiceSelected).Forget();
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
        private async UniTaskVoid CreateChoiceButton(Choice choice, int choiceIndex, Action<int> onChoiceSelected)
        {
            var buttonObj = new GameObject($"ChoiceButton_{choiceIndex}",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObj.transform.SetParent(buttonParentObj, false);

            var layoutElement = buttonObj.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = 200;
            layoutElement.preferredHeight = 50;

            var image = buttonObj.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.95f);

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
            textRect.offsetMin = new Vector2(12f, 6f);
            textRect.offsetMax = new Vector2(-12f, -6f);

            var textComponent = textObj.GetComponent<TextMeshProUGUI>();
            var fontHandle = Addressables.LoadAssetAsync<TMP_FontAsset>("NotoSans_Regular");
            var fontAsset = await fontHandle.Task;
            textComponent.font = fontAsset;
            textComponent.text = choice.choiceText;
            textComponent.fontSize = 24;
            textComponent.color = Color.black;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.textWrappingMode = TextWrappingModes.Normal;

            Addressables.Release(fontHandle);

            button.onClick.AddListener(() =>
            {
                SetButtonsInteractable(false);
                onChoiceSelected?.Invoke(choiceIndex);
            });
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
    }
}