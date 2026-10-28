using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

namespace FateBastion.UI.Juice
{
    /// <summary>
    /// Plays an entrance on the active children each time this object is enabled: they fade in and pop up one after
    /// another (menus, card rows). Uses scale and CanvasGroup alpha only, so layout groups are never disturbed.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Fate Bastion/UI/Stagger In")]
    public class UiStaggerIn : MonoBehaviour
    {
        [Tooltip("Real seconds before the first child starts.")]
        [SerializeField, Min(0f)] private float _initialDelay = 0.1f;

        [Tooltip("Real seconds between two children.")]
        [SerializeField, Min(0f)] private float _step = 0.06f;

        [SerializeField, Min(0.01f)] private float _duration = 0.38f;
        [SerializeField, Range(0f, 1f)] private float _fromScale = 0.7f;

        private readonly List<CanvasGroup> _groups = new List<CanvasGroup>(8);

        private void OnEnable()
        {
            CollectChildren();
            for (int i = 0; i < _groups.Count; i++)
            {
                CanvasGroup group = _groups[i];
                Transform child = group.transform;
                float delay = _initialDelay + i * _step;

                group.alpha = 0f;
                child.localScale = Vector3.one * _fromScale;
                Tween.Alpha(group, 1f, _duration * 0.6f, Ease.OutQuad, startDelay: delay, useUnscaledTime: true);
                Tween.Scale(child, 1f, _duration, Ease.OutBack, startDelay: delay, useUnscaledTime: true);
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < _groups.Count; i++)
            {
                CanvasGroup group = _groups[i];
                if (group == null)
                {
                    continue;
                }

                Tween.StopAll(group);
                Tween.StopAll(group.transform);
                group.alpha = 1f;
                group.transform.localScale = Vector3.one;
            }
        }

        private void CollectChildren()
        {
            _groups.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (!child.gameObject.activeSelf)
                {
                    continue;
                }

                // A CanvasGroup per child fades the child with all its graphics in one value.
                if (!child.TryGetComponent(out CanvasGroup group))
                {
                    group = child.gameObject.AddComponent<CanvasGroup>();
                }

                _groups.Add(group);
            }
        }
    }
}
