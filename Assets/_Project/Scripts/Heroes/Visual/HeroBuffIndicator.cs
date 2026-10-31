using UnityEngine;

namespace FateBastion.Heroes
{
    /// <summary>
    /// Buff icons above a hero's head (C-09): red arrow while a Tu sĩ buffs its damage, yellow double chevron while a
    /// Thánh Nữ buffs its attack speed. Only buffs from hero skills show; the Command Aura has its own 6 m ring.
    /// Sits on a "BuffIcons" object under <see cref="HeroVisual.Overhead"/>; changes only when the buffs change.
    /// </summary>
    [DisallowMultipleComponent]
    public class HeroBuffIndicator : MonoBehaviour
    {
        [Tooltip("Child FX_Buff_Damage instance, shown while a hero skill raises damage.")]
        [SerializeField] private GameObject _damageIcon;

        [Tooltip("Child FX_Buff_AttackSpeed instance, shown while a hero skill raises attack speed.")]
        [SerializeField] private GameObject _attackSpeedIcon;

        [Tooltip("Distance in metres between the two icons when both are shown.")]
        [SerializeField, Min(0f)] private float _spacing = 0.45f;

        public bool DamageShown => _damageIcon != null && _damageIcon.activeSelf;
        public bool AttackSpeedShown => _attackSpeedIcon != null && _attackSpeedIcon.activeSelf;

        private void Awake()
        {
            if (_damageIcon == null || _attackSpeedIcon == null)
                Debug.LogError($"{nameof(HeroBuffIndicator)} on '{name}': assign both icons (Tools > FX > Add Buff Indicator).", this);
            Refresh(false, false);
        }

        // STUB: thay bằng HeroStats.BuffsChanged + HasSkillBuff khi A thêm (issue [Cần A] C-09):
        //   OnEnable: _stats = GetComponentInParent<HeroStats>(); _stats.BuffsChanged += HandleBuffsChanged; HandleBuffsChanged();
        //   OnDisable: _stats.BuffsChanged -= HandleBuffsChanged;
        //   HandleBuffsChanged: Refresh(_stats.HasSkillBuff(BuffType.Damage), _stats.HasSkillBuff(BuffType.AttackSpeed));
        // Until then nothing calls Refresh and the icons stay hidden.

        // Pooled hero: hide on return so it does not come back with icons from its last match.
        private void OnDisable() => Refresh(false, false);

        /// <summary>Sets the assets from code (tests and the Editor tool).</summary>
        public void Configure(GameObject damageIcon, GameObject attackSpeedIcon, float spacing)
        {
            _damageIcon = damageIcon;
            _attackSpeedIcon = attackSpeedIcon;
            _spacing = spacing;
        }

        /// <summary>Shows the icon of each buffed stat; one icon is centred, two sit side by side.</summary>
        public void Refresh(bool damage, bool attackSpeed)
        {
            BuffIconLayout.Arrange(damage, attackSpeed, _spacing, out float damageX, out float attackSpeedX);
            Apply(_damageIcon, damage, damageX);
            Apply(_attackSpeedIcon, attackSpeed, attackSpeedX);
        }

        private static void Apply(GameObject icon, bool shown, float x)
        {
            if (icon == null) return;
            if (shown) icon.transform.localPosition = new Vector3(x, 0f, 0f);
            if (icon.activeSelf != shown) icon.SetActive(shown);
        }
    }
}
