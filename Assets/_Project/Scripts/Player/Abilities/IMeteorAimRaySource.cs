using UnityEngine;

namespace FateBastion.Player
{
    /// <summary>Provides the world ray for meteor aiming without letting gameplay read input devices directly.</summary>
    public interface IMeteorAimRaySource
    {
        bool TryGetAimRay(out Ray ray);
    }
}
