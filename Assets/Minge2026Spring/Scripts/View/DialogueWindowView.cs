using TMPro;
using UnityEngine;

namespace Minge2026Spring.Scripts.View
{
    public class DialogueWindowView : MonoBehaviour
    {
        [SerializeField] public TextMeshProUGUI mainText;

        public void SetText(string text)
        {
            mainText.SetText(text);
        }
    }
}