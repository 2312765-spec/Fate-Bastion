#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;
using FateBastion.Core;

namespace FateBastion.Heroes.Sandbox
{
    /// <summary>
    /// Sandbox/B_Placement driver for B-02/B-04: starts a match deck and a wallet without GameManager so placement,
    /// upgrade and sell can be tried with keys 1–5, left click, Esc / right tap, U / X (through InputRouter) or the
    /// buttons below. Shows the live validation reason and the HeroEvents it sees. Built by Tools > Sandbox.
    /// </summary>
    public class PlacementSandboxDemo : MonoBehaviour
    {
        private const int GoldStep = 500;
        private const int MaxLog = 6;

        [SerializeField] private PlacementSystem _placement;
        [SerializeField] private HeroPool _pool;
        [SerializeField] private GhostPreview _ghost;
        [SerializeField] private HeroData[] _deck = new HeroData[0];
        [SerializeField, Min(0)] private int _startingGold = 1000;

        private readonly List<string> _log = new List<string>(MaxLog);
        private Wallet _wallet;
        private MatchDeck _matchDeck;
        private PlacementError _previewError;
        private string _lastFailure = "-";

        private void Start()
        {
            CancelStack.ResetAll();
            HeroEvents.ResetAll();

            _matchDeck = new MatchDeck(_deck);
            List<HeroData> heroes = _matchDeck.DistinctHeroes();
            _pool.Prewarm(heroes, _placement.Rules, _placement.LevelTable, null);
            _ghost.Prewarm(heroes);

            _wallet = new Wallet(_startingGold);
            _placement.BeginMatch(_matchDeck, _wallet);
            _placement.PreviewErrorChanged += e => _previewError = e;
            _placement.PlaceFailed += e => _lastFailure = e.ToString();
            _placement.UpgradeFailed += (_, e) => _lastFailure = "Upgrade: " + e;

            HeroEvents.OnHeroPlaced += info => Log($"Placed {info.HeroId} (-{info.Gold})");
            HeroEvents.OnHeroUpgraded += info => Log($"Upgraded {info.HeroId} → L{info.Level} (-{info.Gold})");
            HeroEvents.OnHeroSold += info => Log($"Sold {info.HeroId} L{info.Level} (+{info.Gold})");
            HeroEvents.OnHeroSelected += info => Log(info.Hero != null ? $"Selected {info.HeroId} L{info.Level}" : "Selection cleared");
        }

        private void OnDestroy()
        {
            HeroEvents.ResetAll();
            CancelStack.ResetAll();
        }

        private void Log(string line)
        {
            if (_log.Count == MaxLog) _log.RemoveAt(0);
            _log.Add(line);
        }

        private void OnGUI()
        {
            if (_wallet == null) return;

            GUILayout.BeginArea(new Rect(12f, 12f, 380f, 520f), GUI.skin.box);
            GUILayout.Label($"Vàng: {_wallet.Gold}   Quân số: {_matchDeck.TotalPlaced}/{_placement.Rules.MaxHeroes}");
            GUILayout.Label(_placement.IsPreviewing
                ? $"Xem trước ô {_placement.PreviewSlot + 1}: {_previewError}"
                : "Rảnh (phím 1–5 hoặc nút bên dưới)");
            GUILayout.Label($"Lần đặt lỗi gần nhất: {_lastFailure}");

            for (int i = 0; i < _matchDeck.Count; i++)
            {
                DeckSlot slot = _matchDeck[i];
                if (slot.IsEmpty) continue;
                int cost = HeroEconomy.PlaceCost(_placement.Rules, slot.HeroData.rarity);
                if (GUILayout.Button($"{i + 1}. {slot.HeroData.id} ({slot.HeroData.rarity}) {cost}g · {slot.PlacedCount}/{_placement.Rules.MaxPerHeroOf(slot.HeroData.rarity)}"))
                {
                    _placement.EnterPreview(i);
                }
            }

            HeroLevel selected = _placement.Selected;
            if (selected != null)
            {
                int next = selected.NextUpgradeCost(_placement.Rules);
                GUILayout.Label($"Đang chọn: {selected.State.HeroId} cấp {selected.Level}, đã tiêu {selected.GoldSpent}");
                GUILayout.BeginHorizontal();
                if (next >= 0 && GUILayout.Button($"Nâng cấp ({next}g)")) _placement.TryUpgrade(selected);
                if (GUILayout.Button($"Bán (+{selected.State.SellRefund(_placement.Rules)}g)")) _placement.RequestSell(selected);
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"+{GoldStep} Vàng")) _wallet.Add(GoldStep);
            if (GUILayout.Button("Trả hết tướng")) _placement.EndMatch();
            if (GUILayout.Button("Bắt đầu lại")) _placement.BeginMatch(_matchDeck, _wallet);
            GUILayout.EndHorizontal();

            foreach (string line in _log) GUILayout.Label(line);
            GUILayout.EndArea();
        }
    }
}
#endif
