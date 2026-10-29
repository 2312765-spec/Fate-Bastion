using System;
using System.Reflection;
using UnityEngine;

namespace FateBastion.Heroes
{
    /// <summary>
    /// STUB: thay bằng SummonerInputRouter cài IPointerSource (C, S3). Reads Mouse.current.position by reflection,
    /// because FateBastion.Heroes does not reference the Input System assembly and asmdef files are a shared zone.
    /// Read-only: it never consumes or raises input. Costs one small boxing allocation per call (preview mode only).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InputSystemPointerStub : MonoBehaviour, IPointerSource
    {
        private const string MouseTypeName = "UnityEngine.InputSystem.Mouse, Unity.InputSystem";

        private static bool _resolved;
        private static PropertyInfo _current;
        private static PropertyInfo _position;
        private static MethodInfo _readValue;

        public bool TryGetPointerPosition(out Vector2 screenPosition)
        {
            screenPosition = default;
            if (!Resolve()) return false;

            object mouse = _current.GetValue(null);
            if (mouse == null) return false;

            object control = _position.GetValue(mouse);
            if (control == null) return false;

            screenPosition = (Vector2)_readValue.Invoke(control, null);
            return true;
        }

        private static bool Resolve()
        {
            if (_resolved) return _readValue != null;
            _resolved = true;

            try
            {
                Type mouseType = Type.GetType(MouseTypeName);
                _current = mouseType?.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
                _position = mouseType?.GetProperty("position", BindingFlags.Public | BindingFlags.Instance);
                _readValue = _position?.PropertyType.GetMethod("ReadValue", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            }
            catch (Exception exception)
            {
                Debug.LogError($"{nameof(InputSystemPointerStub)}: cannot read the mouse position ({exception.Message}).");
                _readValue = null;
            }

            if (_current == null || _position == null || _readValue == null)
            {
                Debug.LogError($"{nameof(InputSystemPointerStub)}: Input System Mouse API not found; placement cannot follow the cursor.");
                _readValue = null;
            }

            return _readValue != null;
        }
    }
}
