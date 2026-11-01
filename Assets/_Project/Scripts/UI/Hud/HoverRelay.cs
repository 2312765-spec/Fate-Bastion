using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FateBastion.UI
{
    /// <summary>Raises <see cref="Changed"/> when the pointer enters (true) or leaves (false) this UI element.</summary>
    [DisallowMultipleComponent]
    public sealed class HoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public event Action<bool> Changed;

        public void OnPointerEnter(PointerEventData eventData) => Changed?.Invoke(true);

        public void OnPointerExit(PointerEventData eventData) => Changed?.Invoke(false);

        private void OnDisable() => Changed?.Invoke(false);
    }
}
