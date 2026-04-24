using Cysharp.Threading.Tasks;
using Minge2026Spring.Scripts.Application.DTOs;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Minge2026Spring.Scripts.View
{
    public class ChatUIView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI speakerText;
        [SerializeField] private TextMeshProUGUI chatText;
        
        public async UniTaskVoid SetData(Dialogue dialogue)
        {
            var iconHandle = Addressables.LoadAssetAsync<Sprite>(dialogue.iconId);
            var iconAsset = await iconHandle.Task;
            
            iconImage.sprite = iconAsset;
            speakerText.text = dialogue.speaker;
            chatText.text = dialogue.message;

            chatText.fontSizeMax = 18;

            Addressables.Release(iconHandle);
        }
    }
}