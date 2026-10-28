using UnityEngine;

namespace FateBastion.Audio
{
    /// <summary>
    /// Added to the Attack and Ultimate states of AC_Hero_Base (S11). HeroController fires those triggers on each
    /// hit and on a successful Ultimate (contract 3.1 #11), so the sound follows gameplay without any code in Heroes.
    /// The behaviour lives on the base controller, so every hero's Override Controller gets it too.
    /// </summary>
    public class AnimatorSoundBehaviour : StateMachineBehaviour
    {
        [SerializeField] private HeroSoundSlot _slot = HeroSoundSlot.Attack;

        // Unity makes one behaviour instance per Animator, so caching the lookup here is per hero.
        private HeroSoundSet _soundSet;
        private bool _searched;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!_searched)
            {
                _searched = true;
                _soundSet = animator.GetComponentInParent<HeroSoundSet>();
                if (_soundSet == null)
                {
                    Debug.LogWarning($"{nameof(AnimatorSoundBehaviour)}: no {nameof(HeroSoundSet)} on or above '{animator.name}'; hero is silent.", animator);
                }
            }

            if (_soundSet != null) _soundSet.Play(_slot);
        }
    }
}
