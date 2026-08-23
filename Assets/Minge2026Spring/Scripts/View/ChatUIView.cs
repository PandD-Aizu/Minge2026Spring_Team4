using Minge2026Spring.Scripts.Application.DTOs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minge2026Spring.Scripts.View
{
    public class ChatUIView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI speakerText;
        [SerializeField] private TextMeshProUGUI chatText;

        /// <summary>
        /// キャッシュ済みのアイコンを使って会話データを表示する
        /// </summary>
        public void SetData(Dialogue dialogue, Sprite iconAsset)
        {
            iconImage.sprite = iconAsset;
            UILocalization.Bind(speakerText, "Characters", dialogue.speakerKey);
            UILocalization.Bind(chatText, "Dialogue", dialogue.messageKey);
            chatText.fontSizeMax = 18;
        }
    }
}
