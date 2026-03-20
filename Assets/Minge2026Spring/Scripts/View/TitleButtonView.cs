using UnityEngine;
using UnityEngine.UI;

namespace Minge2026Spring.Scripts.View
{
    public class TitleButtonView : MonoBehaviour
    {
        [SerializeField] public Button StartButton;
        [SerializeField] public Button OptionButton;
        [SerializeField] public Button ExitButton;
        
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
            if (ExitButton != null)
                ExitButton.interactable = interactable;
        }
    }
}