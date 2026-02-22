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
        
        public void SetData(Dialogue dialogue)
        {
            speakerText.text = dialogue.speaker;
            chatText.text = dialogue.message;
        }
    }
}