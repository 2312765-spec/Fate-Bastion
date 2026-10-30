using FateBastion.Core;
using FateBastion.Enemies;
using UnityEngine.SceneManagement;

namespace FateBastion.Game
{
    /// <summary>
    /// What the lobby asked to play, carried across the scene load to GameManager. Static on purpose: it only lives
    /// between "Vào trận" and the match start. The deck is read from the save (S8), not passed here.
    /// </summary>
    public static class MatchRequest
    {
        public const string LobbyScene = "Lobby";
        public const string GameScene = "Game";

        public static LevelData Level { get; private set; }
        public static bool IsEndless { get; private set; }
        public static bool HasRequest => Level != null;

        public static void Set(LevelData level, bool isEndless)
        {
            Level = level;
            IsEndless = isEndless;
        }

        /// <summary>Stores the request and loads the Game scene. CancelStack is static, so it is emptied here.</summary>
        public static void Launch(LevelData level, bool isEndless)
        {
            Set(level, isEndless);
            CancelStack.ResetAll();
            SceneManager.LoadScene(GameScene);
        }

        /// <summary>Plays the same level again (result screen "Chơi lại").</summary>
        public static void Retry()
        {
            CancelStack.ResetAll();
            SceneManager.LoadScene(GameScene);
        }

        /// <summary>Back to the lobby; the request is kept so the level select can highlight it.</summary>
        public static void ReturnToLobby()
        {
            CancelStack.ResetAll();
            SceneManager.LoadScene(LobbyScene);
        }

        /// <summary>For tests.</summary>
        public static void ResetAll()
        {
            Level = null;
            IsEndless = false;
        }
    }
}
