using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Minge2026Spring.Scripts.Presenter
{
    public class SkipButtonHoldNotifier : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public event Action Pressed;
        public event Action Released;

        public void OnPointerDown(PointerEventData eventData) => Pressed?.Invoke();

        public void OnPointerUp(PointerEventData eventData) => Released?.Invoke();

        public void OnPointerExit(PointerEventData eventData) => Released?.Invoke();

        private void OnDisable() => Released?.Invoke();
    }
}
