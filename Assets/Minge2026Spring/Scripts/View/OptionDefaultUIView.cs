using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minge2026Spring.Scripts.View
{
    public class OptionDefaultUIView : MonoBehaviour
    {
        [Header("音量スライダー")]
        [SerializeField] public Slider mainVolumeSlider;
        [SerializeField] public Slider bgmVolumeSlider;
        [SerializeField] public Slider seVolumeSlider;
        [SerializeField] public Slider voiceVolumeSlider;

        [Header("タイトルに戻るボタン")] 
        [SerializeField] public Button backButton;

        private void Awake()
        {
            if (voiceVolumeSlider is not null || seVolumeSlider is null)
                return;

            var sliderObject = Instantiate(seVolumeSlider.gameObject, seVolumeSlider.transform.parent);
            sliderObject.name = "VoiceVolumeSlider";
            var sliderRect = sliderObject.GetComponent<RectTransform>();
            if (sliderRect is not null)
                sliderRect.anchoredPosition += Vector2.down * 100f;
            voiceVolumeSlider = sliderObject.GetComponent<Slider>();

            var seLabel = GameObject.Find("SEVolumeText");
            if (seLabel is not null)
            {
                var voiceLabel = Instantiate(seLabel, seLabel.transform.parent);
                voiceLabel.name = "VoiceVolumeText";
                var labelRect = voiceLabel.GetComponent<RectTransform>();
                if (labelRect is not null)
                    labelRect.anchoredPosition += Vector2.down * 100f;
                var label = voiceLabel.GetComponent<TextMeshProUGUI>();
                if (label is not null)
                    label.text = "Voice Volume";
            }
        }
    }
}
