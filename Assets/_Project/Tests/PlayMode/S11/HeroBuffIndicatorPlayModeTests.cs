using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FateBastion.Core;
using FateBastion.Heroes;
using FateBastion.Skills;

namespace FateBastion.Tests.PlayMode.S11
{
    /// <summary>Buff icons follow HeroStats: only hero-skill buffs show, the Command Aura does not (C-09).</summary>
    public class HeroBuffIndicatorPlayModeTests
    {
        private GameObject _hero;
        private HeroStats _stats;
        private HeroBuffIndicator _indicator;
        private HeroStatsSettings _settings;
        private HeroData _data;
        private SkillData _tuSiSkill;
        private SkillData _thanhNuSkill;
        private ScriptableObject _auraType;
        private GameObject _tuSi;
        private GameObject _thanhNu;
        private GameObject _summoner;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _settings = ScriptableObject.CreateInstance<HeroStatsSettings>();
            foreach (string field in new[] { "_commonDamageMultiplier", "_rareDamageMultiplier", "_epicDamageMultiplier", "_legendaryDamageMultiplier" })
                SetField(_settings, field, 1f);
            _data = ScriptableObject.CreateInstance<HeroData>();
            _data.damage = 10f;
            _data.attacksPerSecond = 1f;
            _data.range = 5f;
            _tuSiSkill = ScriptableObject.CreateInstance<SkillData>();
            _thanhNuSkill = ScriptableObject.CreateInstance<SkillData>();
            // Any non-SkillData source type stands in for CommandAuraSettings (Player asmdef is not needed here).
            _auraType = ScriptableObject.CreateInstance<HeroData>();
            _tuSi = new GameObject("TuSi");
            _thanhNu = new GameObject("ThanhNu");
            _summoner = new GameObject("Summoner");

            // Same shape as a hero Variant: HeroStats on the root, icons under Model/Overhead/BuffIcons.
            _hero = new GameObject("Hero");
            _hero.SetActive(false);
            _stats = _hero.AddComponent<HeroStats>();
            SetField(_stats, "_settings", _settings);
            Transform overhead = Child(Child(_hero.transform, "Model"), "Overhead");
            Transform icons = Child(overhead, "BuffIcons");
            GameObject damageIcon = Child(icons, "FX_Buff_Damage").gameObject;
            GameObject attackSpeedIcon = Child(icons, "FX_Buff_AttackSpeed").gameObject;
            _indicator = icons.gameObject.AddComponent<HeroBuffIndicator>();
            _indicator.Configure(damageIcon, attackSpeedIcon, 0.45f);
            _hero.SetActive(true);
            _stats.Initialize(_data);

            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_hero);
            Object.Destroy(_tuSi);
            Object.Destroy(_thanhNu);
            Object.Destroy(_summoner);
            Object.Destroy(_settings);
            Object.Destroy(_data);
            Object.Destroy(_tuSiSkill);
            Object.Destroy(_thanhNuSkill);
            Object.Destroy(_auraType);
        }

        [Test]
        public void SkillDamageBuff_ShowsDamageIconOnly()
        {
            _stats.SetBuff(_tuSi, _tuSiSkill, BuffType.Damage, 0.2f);

            Assert.IsTrue(_indicator.DamageShown);
            Assert.IsFalse(_indicator.AttackSpeedShown);
        }

        [Test]
        public void CommandAuraBuff_ShowsNoIcon()
        {
            _stats.SetBuff(_summoner, _auraType, BuffType.AttackSpeed, 0.25f);

            Assert.IsFalse(_indicator.DamageShown);
            Assert.IsFalse(_indicator.AttackSpeedShown, "Hào quang Chỉ huy đã có vòng 6 m, không hiện ký hiệu.");
        }

        [Test]
        public void BothSkillBuffs_ShowBothIcons_ThenRemoveOneHidesIt()
        {
            _stats.SetBuff(_tuSi, _tuSiSkill, BuffType.Damage, 0.2f);
            _stats.SetBuff(_thanhNu, _thanhNuSkill, BuffType.AttackSpeed, 0.2f);
            Assert.IsTrue(_indicator.DamageShown && _indicator.AttackSpeedShown);

            _stats.RemoveBuff(_tuSi);

            Assert.IsFalse(_indicator.DamageShown);
            Assert.IsTrue(_indicator.AttackSpeedShown);
        }

        [Test]
        public void HeroReturnsToPool_HidesIconsAndStopsListening()
        {
            _stats.SetBuff(_tuSi, _tuSiSkill, BuffType.Damage, 0.2f);

            _hero.SetActive(false);
            Assert.IsFalse(_indicator.DamageShown);

            // Changes while pooled are ignored; enabling again reads the current buffs.
            _stats.RemoveBuff(_tuSi);
            _stats.SetBuff(_thanhNu, _thanhNuSkill, BuffType.AttackSpeed, 0.2f);
            Assert.IsFalse(_indicator.AttackSpeedShown);

            _hero.SetActive(true);
            Assert.IsFalse(_indicator.DamageShown);
            Assert.IsTrue(_indicator.AttackSpeedShown);
        }

        private static Transform Child(Transform parent, string childName)
        {
            var child = new GameObject(childName).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static void SetField(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
