using Newtonsoft.Json;

namespace FateBastion.Meta
{
    /// <summary>
    /// Player options persisted with the save (S9). Plain data so it can be JSON round-tripped;
    /// the UI (S10) and Audio (S11) read and write these values.
    /// </summary>
    public class SaveSettings
    {
        [JsonProperty("master")] public float master = 0.8f;
        [JsonProperty("music")] public float music = 0.6f;
        [JsonProperty("sfx")] public float sfx = 0.8f;
        [JsonProperty("cameraShake")] public bool cameraShake = true;
        [JsonProperty("skipConfirm")] public bool skipConfirm = true;
    }
}
