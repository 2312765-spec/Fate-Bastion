using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FateBastion.Tests.EditMode.S11
{
    /// <summary>
    /// Scenes without the Player prefab must carry their own AudioListener, or nothing is heard (lobby music, buttons,
    /// gacha). Regression: Build Systems Prefabs and Scenes once rebuilt Lobby/Boot cameras without one.
    /// </summary>
    public class SceneAudioListenerTests
    {
        [TestCase("Assets/_Project/Scenes/Lobby.unity")]
        [TestCase("Assets/_Project/Scenes/Boot.unity")]
        public void Scene_WithoutPlayer_HasExactlyOneAudioListener(string scenePath)
        {
            if (!File.Exists(scenePath)) Assert.Ignore($"{scenePath} chưa có.");

            // Force Text scenes list each component as a YAML document headed by its type name.
            int listeners = Regex.Matches(File.ReadAllText(scenePath), @"^AudioListener:", RegexOptions.Multiline).Count;
            Assert.AreEqual(1, listeners, $"{scenePath} cần đúng 1 AudioListener (trên Main Camera).");
        }
    }
}
