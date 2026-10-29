using UnityEngine;

namespace FateBastion.Audio
{
    /// <summary>
    /// Added to the Ultimate state of AC_Hero_Base (S11, C-18): shakes the camera when HeroController fires the
    /// Ultimate trigger (contract 3.1 #11). Core has no Ultimate event, so the animator state is the hook.
    /// </summary>
    public class AnimatorShakeBehaviour : StateMachineBehaviour
    {
        [SerializeField] private ShakeKind _kind = ShakeKind.Ultimate;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            FeedbackService service = FeedbackService.Instance;
            if (service != null) service.Shake(_kind, animator.transform.position);
        }
    }
}
