using UnityEngine;
using FateBastion.Core;

namespace FateBastion.Player
{
    /// <summary>
    /// The cursor is always visible, except while the right mouse is held to rotate the camera (S3).
    /// It is given back when the action map changes or the window loses focus (Alt-Tab).
    /// </summary>
    [DisallowMultipleComponent]
    public class CursorController : MonoBehaviour
    {
        private SummonerInputRouter _router;

        private void Start()
        {
            _router = InputRouter.Instance as SummonerInputRouter;
            if (_router == null)
            {
                UnityEngine.Debug.LogError($"{nameof(CursorController)} on '{name}': no {nameof(SummonerInputRouter)} in the scene.", this);
                return;
            }

            _router.LookHeldChanged += OnLookHeldChanged;
            _router.ActionMapChanged += OnActionMapChanged;
            Release();
        }

        private void OnDestroy()
        {
            if (_router != null)
            {
                _router.LookHeldChanged -= OnLookHeldChanged;
                _router.ActionMapChanged -= OnActionMapChanged;
            }

            Release();
        }

        private void OnLookHeldChanged(bool held)
        {
            if (held)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Release();
            }
        }

        private void OnActionMapChanged(ActionMapId map) => Release();

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                Release();
            }
        }

        private static void Release()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
