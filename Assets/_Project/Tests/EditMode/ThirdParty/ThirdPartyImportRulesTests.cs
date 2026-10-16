using NUnit.Framework;
using UnityEditor;
using FateBastion.Editor.Heroes;

namespace FateBastion.Tests.EditMode.ThirdParty
{
    /// <summary>Tests for the folder → import profile rules of third-party FBX (C-07).</summary>
    public class ThirdPartyImportRulesTests
    {
        [TestCase("Assets/ThirdParty/Quaternius/Characters/Pack/Knight.fbx", ThirdPartyModelKind.HumanoidCharacter)]
        [TestCase("Assets/ThirdParty/Quaternius/Creatures/Pack/Slime.fbx", ThirdPartyModelKind.GenericCreature)]
        [TestCase("Assets/ThirdParty/Quaternius/Props/Pack/Horns.fbx", ThirdPartyModelKind.StaticProp)]
        [TestCase("Assets/ThirdParty/Mixamo/Animations/Bow_Shoot.fbx", ThirdPartyModelKind.HumanoidAnimation)]
        [TestCase("Assets/ThirdParty/Kenney/TowerDefenseKit/tree.fbx", ThirdPartyModelKind.StaticProp)]
        public void Classify_KnownFolder_ReturnsProfile(string path, ThirdPartyModelKind expected)
        {
            Assert.AreEqual(expected, ThirdPartyImportRules.Classify(path));
        }

        [TestCase("Assets/ThirdParty/Quaternius/Characters/Pack/Knight.FBX")]
        [TestCase("assets/thirdparty/quaternius/characters/Pack/Knight.fbx")]
        [TestCase("Assets\\ThirdParty\\Quaternius\\Characters\\Pack\\Knight.fbx")]
        public void Classify_CaseOrBackslash_StillHumanoidCharacter(string path)
        {
            Assert.AreEqual(ThirdPartyModelKind.HumanoidCharacter, ThirdPartyImportRules.Classify(path));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Assets/_Project/Art/Models/Castle.fbx")]
        [TestCase("Assets/ThirdParty/Quaternius/Characters/Pack/Knight.png")]
        [TestCase("Assets/ThirdParty/Quaternius/Characters/Pack/Knight.gltf")]
        [TestCase("Assets/ThirdParty/Quaternius/Other/Knight.fbx")]
        public void Classify_OutsideRulesOrNotFbx_ReturnsNone(string path)
        {
            Assert.AreEqual(ThirdPartyModelKind.None, ThirdPartyImportRules.Classify(path));
        }

        [TestCase(ThirdPartyModelKind.HumanoidCharacter, ModelImporterAnimationType.Human)]
        [TestCase(ThirdPartyModelKind.HumanoidAnimation, ModelImporterAnimationType.Human)]
        [TestCase(ThirdPartyModelKind.GenericCreature, ModelImporterAnimationType.Generic)]
        [TestCase(ThirdPartyModelKind.StaticProp, ModelImporterAnimationType.None)]
        public void GetAnimationType_Profile_ReturnsRig(ThirdPartyModelKind kind, ModelImporterAnimationType expected)
        {
            Assert.AreEqual(expected, ThirdPartyImportRules.GetAnimationType(kind));
        }

        [TestCase(ThirdPartyModelKind.None, false)]
        [TestCase(ThirdPartyModelKind.HumanoidCharacter, true)]
        [TestCase(ThirdPartyModelKind.HumanoidAnimation, true)]
        [TestCase(ThirdPartyModelKind.GenericCreature, true)]
        [TestCase(ThirdPartyModelKind.StaticProp, false)]
        public void ImportsAnimation_Profile_MatchesRule(ThirdPartyModelKind kind, bool expected)
        {
            Assert.AreEqual(expected, ThirdPartyImportRules.ImportsAnimation(kind));
        }

        [Test]
        public void GetMaterialImportMode_AnimationOnly_ReturnsNone()
        {
            Assert.AreEqual(ModelImporterMaterialImportMode.None,
                ThirdPartyImportRules.GetMaterialImportMode(ThirdPartyModelKind.HumanoidAnimation));
        }

        [TestCase(ThirdPartyModelKind.HumanoidCharacter)]
        [TestCase(ThirdPartyModelKind.GenericCreature)]
        [TestCase(ThirdPartyModelKind.StaticProp)]
        public void GetMaterialImportMode_WithMesh_KeepsEmbeddedMaterials(ThirdPartyModelKind kind)
        {
            Assert.AreEqual(ModelImporterMaterialImportMode.ImportViaMaterialDescription,
                ThirdPartyImportRules.GetMaterialImportMode(kind));
        }

        [TestCase("Assets/ThirdParty/Mixamo/Animations/Idle.fbx", true)]
        [TestCase("Assets/ThirdParty/Mixamo/Animations/Standard Walk.fbx", true)]
        [TestCase("Assets/ThirdParty/Mixamo/Animations/Fast_RUN.fbx", true)]
        [TestCase("Assets/ThirdParty/Mixamo/Animations/Bow_Shoot.fbx", false)]
        [TestCase("Assets/ThirdParty/Mixamo/Animations/Spell_Cast.fbx", false)]
        [TestCase(null, false)]
        public void ShouldLoop_FileName_MatchesKeyword(string path, bool expected)
        {
            Assert.AreEqual(expected, ThirdPartyImportRules.ShouldLoop(path));
        }
    }
}
