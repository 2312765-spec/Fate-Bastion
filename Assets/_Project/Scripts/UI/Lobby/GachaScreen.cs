using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FateBastion.Core;
using FateBastion.Meta;

namespace FateBastion.UI
{
    /// <summary>
    /// Screen_Gacha (S8, S10, B-15): pull ×1 / ×10 with their gem prices, pity counter and bar, the public rate table.
    /// A successful pull opens <see cref="GachaResultScreen"/>, which reveals the heroes.
    /// </summary>
    public sealed class GachaScreen : LobbyScreen
    {
        [Tooltip("Result overlay (Screen_Gacha/ResultPanel). Empty = found under this screen.")]
        [SerializeField] private GachaResultScreen _result;

        private TMP_Text _pityText;
        private Image _pityFill;
        private TMP_Text _pityBarText;
        private Button _pull1;
        private Button _pull10;
        private TMP_Text _pull1Label;
        private TMP_Text _pull10Label;

        public override string Title => "Gacha";

        protected override void OnBind()
        {
            if (_result == null) _result = GetComponentInChildren<GachaResultScreen>(true);

            _pityText = UiFind.Get<TMP_Text>(transform, "BannerPanel/PityText", this);
            _pityFill = UiFind.Get<Image>(transform, "BannerPanel/PityBar/Fill", this);
            _pityBarText = UiFind.Optional<TMP_Text>(transform, "BannerPanel/PityBar/PityBarText");
            _pull1 = UiFind.Get<Button>(transform, "BannerPanel/PullButtons/Pull1Button", this);
            _pull10 = UiFind.Get<Button>(transform, "BannerPanel/PullButtons/Pull10Button", this);
            _pull1Label = UiFind.Optional<TMP_Text>(transform, "BannerPanel/PullButtons/Pull1Button/Label");
            _pull10Label = UiFind.Optional<TMP_Text>(transform, "BannerPanel/PullButtons/Pull10Button/Label");
            if (_pull1 != null) _pull1.onClick.AddListener(() => Pull(1));
            if (_pull10 != null) _pull10.onClick.AddListener(() => Pull(GachaService.TenPullCount));

            FillRates();
        }

        protected override void Refresh()
        {
            SaveData data = Context.Data;
            GachaService gacha = Context.Session.Gacha;
            int threshold = Context.Gacha != null ? Context.Gacha.PityThreshold : 25;
            int pity = Mathf.Clamp(data.pityCounter, 0, threshold);

            if (_pityText != null) _pityText.text = $"Pity {pity}/{threshold} · lượt {threshold} chắc chắn ra Legendary";
            if (_pityFill != null) _pityFill.fillAmount = threshold > 0 ? (float)pity / threshold : 0f;
            if (_pityBarText != null) _pityBarText.text = $"{pity}/{threshold}";

            int one = gacha.CostOf(1);
            int ten = gacha.CostOf(GachaService.TenPullCount);
            if (_pull1Label != null) _pull1Label.text = $"Quay x1 · {UiText.Gems(one)}";
            if (_pull10Label != null) _pull10Label.text = $"Quay x10 · {UiText.Gems(ten)}";

            // Kept clickable when short of gems so the player is told why; only dimmed.
            SetAffordable(_pull1, gacha.CanAfford(data, 1));
            SetAffordable(_pull10, gacha.CanAfford(data, GachaService.TenPullCount));
        }

        private static void SetAffordable(Button button, bool affordable)
        {
            if (button == null) return;
            var group = button.GetComponent<CanvasGroup>();
            if (group == null) group = button.gameObject.AddComponent<CanvasGroup>();
            group.alpha = affordable ? 1f : 0.55f;
        }

        private void FillRates()
        {
            GachaTable table = Context.Gacha;
            if (table == null) return;

            float total = 0f;
            for (int r = 0; r < GachaTable.RarityCount; r++) total += table.WeightOf((Rarity)r);
            if (total <= 0f) return;

            for (int r = 0; r < GachaTable.RarityCount; r++)
            {
                var rarity = (Rarity)r;
                var text = UiFind.Optional<TMP_Text>(transform, $"RatePanel/RateRow_{rarity}/RateText");
                if (text != null) text.text = UiText.Percent(table.WeightOf(rarity) / total);
            }
        }

        private void Pull(int count)
        {
            MetaSession session = Context.Session;
            if (!session.Gacha.CanAfford(session.Data, count))
            {
                int missing = session.Gacha.CostOf(count) - session.Data.gems;
                Context.Message("Không đủ Ngọc", $"Cần thêm {UiText.Gems(missing)} để quay x{count}.");
                return;
            }

            GachaPullResult result = count == 1 ? session.PullOne() : session.PullTen();
            if (!result.Success)
            {
                Context.Message("Không quay được", $"Lỗi: {result.Error}.");
                return;
            }

            if (_result != null) _result.Show(result);
        }
    }
}
