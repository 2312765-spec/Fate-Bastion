using Unity.Cinemachine;
using UnityEngine;
using Shapes = Unity.Cinemachine.CinemachineImpulseDefinition.ImpulseShapes;
using Types = Unity.Cinemachine.CinemachineImpulseDefinition.ImpulseTypes;

namespace FateBastion.Audio
{
    /// <summary>
    /// Camera shake per trigger (S11). One asset at Data/Settings/FeedbackSettings, owned by C. Values are
    /// presentation, tuned by feel; the Settings toggle turns every shake off (FeedbackService.SetShakeEnabled).
    /// </summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Audio/Feedback Settings", fileName = "FeedbackSettings")]
    public class FeedbackSettings : ScriptableObject
    {
        public ShakeProfile meteorExplosion = new ShakeProfile(Shapes.Explosion, 0.6f, Types.Dissipating, 40f, 1f, 0f);
        public ShakeProfile bossAppear = new ShakeProfile(Shapes.Rumble, 1.5f, Types.Uniform, 100f, 0.6f, 0f);
        public ShakeProfile castleHit = new ShakeProfile(Shapes.Bump, 0.25f, Types.Uniform, 100f, 0.4f, 0.5f);
        public ShakeProfile ultimate = new ShakeProfile(Shapes.Explosion, 0.5f, Types.Dissipating, 40f, 0.7f, 0.3f);

        public ShakeProfile Get(ShakeKind kind)
        {
            switch (kind)
            {
                case ShakeKind.MeteorExplosion: return meteorExplosion;
                case ShakeKind.BossAppear: return bossAppear;
                case ShakeKind.CastleHit: return castleHit;
                case ShakeKind.Ultimate: return ultimate;
                default: return null;
            }
        }

        private void OnValidate()
        {
            Validate(meteorExplosion);
            Validate(bossAppear);
            Validate(castleHit);
            Validate(ultimate);
        }

        private static void Validate(ShakeProfile profile)
        {
            if (profile?.definition == null) return;
            profile.definition.OnValidate();
            // Legacy impulses ignore the shape fields above; force a modern type so the profile does what it shows.
            if (profile.definition.ImpulseType == CinemachineImpulseDefinition.ImpulseTypes.Legacy)
            {
                profile.definition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            }
        }
    }
}
