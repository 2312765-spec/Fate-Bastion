using UnityEngine;
using UnityEngine.SceneManagement;
using FateBastion.Meta;

namespace FateBastion.Game
{
    /// <summary>
    /// Boot scene (S9): opens the save once (save.json → save.bak → new from StarterSettings, then sanitized) and loads
    /// the lobby. The lobby shows any load warnings.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BootLoader : MonoBehaviour
    {
        [SerializeField] private GachaTable _gachaTable;
        [SerializeField] private StarterSettings _starterSettings;

        private void Start()
        {
            if (_gachaTable != null && _starterSettings != null) MetaRuntime.Ensure(_gachaTable, _starterSettings);
            else Debug.LogError($"{name}: BootLoader needs GachaTable and StarterSettings.", this);

            SceneManager.LoadScene(MatchRequest.LobbyScene);
        }
    }
}
