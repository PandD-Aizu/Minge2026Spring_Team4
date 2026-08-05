using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Minge2026Spring.Scripts.View
{
    public class AchievementHoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Action<PointerEventData> _onEnter;
        private Action _onExit;

        public void Configure(Action<PointerEventData> onEnter, Action onExit)
        {
            _onEnter = onEnter;
            _onExit = onExit;
        }

        public void OnPointerEnter(PointerEventData eventData) => _onEnter?.Invoke(eventData);

        public void OnPointerExit(PointerEventData eventData) => _onExit?.Invoke();
    }
}
