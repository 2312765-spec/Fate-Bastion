using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FateBastion.Core;
using FateBastion.Heroes;

namespace FateBastion.UI
{
    /// <summary>
    /// Hero info panel of the HUD (S10, B-14): opens on <see cref="HeroEvents.OnHeroSelected"/>, shows level, stats (next
    /// level while hovering Upgrade), Upgrade / Sell, and for a Legendary the rage bar from
    /// <see cref="HeroController.OnRageChanged"/> with the Ultimate button (TEAM_ASSIGNMENT 3.1 #5). While open it is a
    /// <see cref="CancelStack"/> entry, so Esc or a right tap closes it (3.1 #6).
    /// </summary>
    public sealed class HeroInfoPanelView : ICancelable
    {
        private readonly GameObject _root;
        private readonly Image _rarityFrame;
        private readonly TMP_Text _nameLevel;
        private readonly TMP_Text _tags;
        private readonly StatRow _damage;
        private readonly StatRow _attackSpeed;
        private readonly StatRow _range;
        private readonly StatRow _dps;
        private readonly GameObject _ultimateGroup;
        private readonly Image _rageFill;
        private readonly TMP_Text _rageText;
        private readonly Button _ultimateButton;
        private readonly Button _upgradeButton;
        private readonly TMP_Text _upgradeLabel;
        private readonly Button _sellButton;
        private readonly TMP_Text _sellLabel;
        private readonly Action<float, float> _onRage;
        private readonly Action<string> _toast;

        private PlacementSystem _placement;
        private HeroLevel _hero;
        private HeroController _controller;
        private bool _showNext;

        public HeroInfoPanelView(Transform panel, Action<string> toast)
        {
            _root = panel.gameObject;
            _toast = toast;
            _onRage = ShowRage;

            _rarityFrame = UiFind.Optional<Image>(panel, "RarityFrame");
            _nameLevel = UiFind.Optional<TMP_Text>(panel, "Content/Header/NameLevelText");
            _tags = UiFind.Optional<TMP_Text>(panel, "Content/Header/TagsText");
            _damage = new StatRow(panel, "Content/Stats/DamageRow");
            _attackSpeed = new StatRow(panel, "Content/Stats/AttackSpeedRow");
            _range = new StatRow(panel, "Content/Stats/RangeRow");
            _dps = new StatRow(panel, "Content/Stats/DpsRow");

            Transform ultimate = panel.Find("Content/UltimateGroup");
            _ultimateGroup = ultimate != null ? ultimate.gameObject : null;
            _rageFill = UiFind.Optional<Image>(panel, "Content/UltimateGroup/RageBar/Fill");
            _rageText = UiFind.Optional<TMP_Text>(panel, "Content/UltimateGroup/RageBar/RageText");
            _ultimateButton = UiFind.Optional<Button>(panel, "Content/UltimateGroup/UltimateButton");

            _upgradeButton = UiFind.Get<Button>(panel, "Content/Actions/UpgradeButton", panel);
            _upgradeLabel = UiFind.Optional<TMP_Text>(panel, "Content/Actions/UpgradeButton/Label");
            _sellButton = UiFind.Get<Button>(panel, "Content/Actions/SellButton", panel);
            _sellLabel = UiFind.Optional<TMP_Text>(panel, "Content/Actions/SellButton/Label");

            if (_upgradeButton != null)
            {
                _upgradeButton.onClick.AddListener(Upgrade);
                HoverRelay hover = _upgradeButton.GetComponent<HoverRelay>();
                if (hover == null) hover = _upgradeButton.gameObject.AddComponent<HoverRelay>();
                hover.Changed += HoverUpgrade;
            }

            if (_sellButton != null) _sellButton.onClick.AddListener(Sell);
            if (_ultimateButton != null) _ultimateButton.onClick.AddListener(() => ActivateUltimate());
            _root.SetActive(false);
        }

        public bool IsOpen => _hero != null;
        public HeroLevel Hero => _hero;

        public void Bind(PlacementSystem placement) => _placement = placement;

        /// <summary>HeroEvents.OnHeroSelected: open for a hero, close when the selection was cleared.</summary>
        public void Select(GameObject heroObject)
        {
            HeroLevel hero = heroObject != null ? heroObject.GetComponent<HeroLevel>() : null;
            if (hero == _hero) return;

            Detach();
            if (hero == null || !hero.IsPlaced)
            {
                Close();
                return;
            }

            _hero = hero;
            _controller = hero.Controller;
            if (_controller != null) _controller.OnRageChanged += _onRage;
            _root.SetActive(true);
            CancelStack.Push(this);
            Refresh();
        }

        /// <summary>Redraw (upgrade, gold change).</summary>
        public void Refresh()
        {
            if (_hero == null || _placement == null) return;
            if (!_hero.IsPlaced)
            {
                Close();
                return;
            }

            HeroData data = _hero.Data;
            HeroLevelTable table = _placement.LevelTable;
            PlacementRules rules = _placement.Rules;
            if (data == null || table == null || rules == null) return;

            if (_rarityFrame != null) _rarityFrame.color = UiText.RarityColor(data.rarity);
            string name = string.IsNullOrEmpty(data.displayName) ? data.id : data.displayName;
            if (_nameLevel != null) _nameLevel.text = $"{name} · {HudFormat.LevelText(_hero.Level, table.MaxLevel)}";
            if (_tags != null)
            {
                _tags.text = $"{UiText.RarityName(data.rarity)} · {HudFormat.ElementName(data.element)} · {HudFormat.RoleName(data.role)}";
            }

            ShowStats(table);

            int cost = _hero.NextUpgradeCost(rules);
            if (_upgradeLabel != null) _upgradeLabel.text = HudFormat.Upgrade(cost);
            if (_upgradeButton != null) _upgradeButton.interactable = cost >= 0 && _placement.Wallet != null && _placement.Wallet.CanAfford(cost);
            if (_sellLabel != null) _sellLabel.text = HudFormat.Sell(_hero.State.SellRefund(rules));

            bool legendary = data.rarity == Rarity.Legendary;
            if (_ultimateGroup != null && _ultimateGroup.activeSelf != legendary) _ultimateGroup.SetActive(legendary);
            if (legendary && _controller != null) ShowRage(_controller.CurrentRage, _controller.MaximumRage);
        }

        /// <summary>Ultimate button or key R. False (with a hint) when the rage is not full or nothing is in range.</summary>
        public bool ActivateUltimate()
        {
            if (_controller == null || _hero == null || _hero.Data == null || _hero.Data.rarity != Rarity.Legendary) return false;
            if (_controller.TryActivateUltimate()) return true;

            _toast?.Invoke(_controller.MaximumRage > 0f && _controller.CurrentRage < _controller.MaximumRage
                ? "Nộ chưa đầy"
                : "Không có quái trong tầm");
            return false;
        }

        /// <summary>Called by CancelStack after it removed this entry (Esc, right tap).</summary>
        public void OnCancel()
        {
            HeroLevel hero = _hero;
            Detach();
            _root.SetActive(false);
            if (hero != null && _placement != null && _placement.Selected == hero) _placement.Deselect();
        }

        public void Close()
        {
            Detach();
            CancelStack.Remove(this);
            if (_root.activeSelf) _root.SetActive(false);
        }

        private void Detach()
        {
            if (_controller != null) _controller.OnRageChanged -= _onRage;
            _controller = null;
            _hero = null;
            _showNext = false;
        }

        private void Upgrade()
        {
            if (_hero != null && _placement != null) _placement.TryUpgrade(_hero);
        }

        private void Sell()
        {
            if (_hero != null && _placement != null) _placement.RequestSell(_hero);
        }

        private void HoverUpgrade(bool hovering)
        {
            _showNext = hovering;
            if (_hero != null && _placement != null && _placement.LevelTable != null) ShowStats(_placement.LevelTable);
        }

        private void ShowRage(float current, float max)
        {
            if (_rageFill != null) _rageFill.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            if (_rageText != null) _rageText.text = HudFormat.Rage(current, max);
            if (_ultimateButton != null) _ultimateButton.interactable = max > 0f && current >= max;
        }

        private void ShowStats(HeroLevelTable table)
        {
            HeroStats stats = _controller != null ? _controller.Stats : null;
            if (stats == null) return;

            LevelMultipliers now = table.MultipliersOf(_hero.Level);
            bool hasNext = _showNext && _hero.Level < table.MaxLevel;
            LevelMultipliers next = hasNext ? table.MultipliersOf(_hero.Level + 1) : now;

            float damage = stats.Damage;
            float speed = stats.AttacksPerSecond;
            float range = stats.Range;
            float nextDamage = damage / Safe(now.Damage) * next.Damage;
            float nextSpeed = speed / Safe(now.AttackSpeed) * next.AttackSpeed;
            float nextRange = range / Safe(now.Range) * next.Range;

            _damage.Show(Whole(damage), hasNext ? Whole(nextDamage) : null);
            _attackSpeed.Show(PerSecond(speed), hasNext ? PerSecond(nextSpeed) : null);
            _range.Show(Metres(range), hasNext ? Metres(nextRange) : null);
            _dps.Show(Whole(damage * speed), hasNext ? Whole(nextDamage * nextSpeed) : null);
        }

        private static float Safe(float value) => value > 0f ? value : 1f;
        private static string Whole(float value) => Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture);
        private static string PerSecond(float value) => value.ToString("0.00", CultureInfo.InvariantCulture) + "/s";
        private static string Metres(float value) => value.ToString("0.0", CultureInfo.InvariantCulture) + " m";

        private sealed class StatRow
        {
            private readonly TMP_Text _value;
            private readonly TMP_Text _next;

            public StatRow(Transform panel, string path)
            {
                _value = UiFind.Optional<TMP_Text>(panel, path + "/Value");
                _next = UiFind.Optional<TMP_Text>(panel, path + "/Next");
            }

            public void Show(string value, string next)
            {
                if (_value != null) _value.text = value;
                if (_next == null) return;
                bool show = next != null;
                if (_next.gameObject.activeSelf != show) _next.gameObject.SetActive(show);
                if (show) _next.text = "→ " + next;
            }
        }
    }
}
