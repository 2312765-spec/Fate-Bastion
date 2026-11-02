using NUnit.Framework;
using FateBastion.Editor.Heroes;

namespace FateBastion.Tests.EditMode.S0b
{
    /// <summary>Naming rules of Setup Hero Models (C-08): FBX takes to clip names, clip names to AC_Hero_Base states.</summary>
    public class HeroModelSetupTests
    {
        [TestCase("CH_CungThu_Rig|CungThu_Idle", "CungThu_Idle")]
        [TestCase("CH_CungThu_Rig|CH_CungThu_Rig|CungThu_Attack", "CungThu_Attack")]
        [TestCase("CungThu_Spawn", "CungThu_Spawn")]
        public void ClipNameFromTake_BlenderTake_DropsRigPrefix(string take, string expected)
        {
            Assert.AreEqual(expected, HeroModelSetup.ClipNameFromTake(take));
        }

        [TestCase("CungThu_Idle", "Idle")]
        [TestCase("CungThu_Attack", "Attack")]
        [TestCase("LongVuong_Spawn", "Spawn")]
        [TestCase("LongVuong_Ultimate", "Ultimate")]
        [TestCase("CungThu_Walk", null)]
        public void StateOf_ClipName_MatchesSuffix(string clip, string expected)
        {
            Assert.AreEqual(expected, HeroModelSetup.StateOf(clip));
        }

        [Test]
        public void Paths_CungThu_FollowFolderConvention()
        {
            const string root = "Assets/_Project/Art/Models/Heroes";
            Assert.AreEqual(root + "/CungThu/CH_CungThu.fbx", HeroModelSetup.GetModelPath(root, "CungThu"));
            Assert.AreEqual(root + "/CungThu/Textures/T_CungThu_BaseColor.png", HeroModelSetup.GetBaseColorPath(root, "CungThu"));
            Assert.AreEqual(root + "/CungThu/M_CungThu.mat", HeroModelSetup.GetMaterialPath(root, "CungThu"));
        }
    }
}
