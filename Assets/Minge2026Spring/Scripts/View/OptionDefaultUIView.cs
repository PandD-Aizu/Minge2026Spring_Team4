using R3;
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

        [Header("タイトルに戻るボタン")] 
        [SerializeField] public Button backButton;
    }
}