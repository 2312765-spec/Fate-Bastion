using FateBastion.Core;
using FateBastion.Game;
using FateBastion.Heroes;
using FateBastion.Meta;

namespace FateBastion.UI
{
    /// <summary>What every lobby screen needs: the save session, the catalogs, navigation and the shared dialog.</summary>
    public sealed class LobbyContext
    {
        public LobbyContext(MetaSession session, HeroCatalog heroes, MatchCatalog levels, GachaTable gacha, UIManager manager,
            ConfirmDialog confirm)
        {
            Session = session;
            Heroes = heroes;
            Levels = levels;
            Gacha = gacha;
            Manager = manager;
            Confirm = confirm;
        }

        public MetaSession Session { get; }
        public HeroCatalog Heroes { get; }
        public MatchCatalog Levels { get; }
        public GachaTable Gacha { get; }
        public UIManager Manager { get; }
        public ConfirmDialog Confirm { get; }

        public SaveData Data => Session.Data;

        public LevelSelectScreen LevelSelect { get; internal set; }
        public GachaScreen GachaScreen { get; internal set; }
        public CollectionScreen Collection { get; internal set; }
        public SettingsScreen Settings { get; internal set; }

        public void Open(UIScreen screen)
        {
            if (screen != null) Manager.Push(screen);
        }

        public HeroData HeroData(string heroId) => Heroes != null && Heroes.TryGet(heroId, out HeroData data) ? data : null;

        /// <summary>Rarity from the hero asset, else from the gacha table, else Common.</summary>
        public Rarity RarityOf(string heroId)
        {
            HeroData data = HeroData(heroId);
            if (data != null) return data.rarity;
            return Gacha != null && Gacha.TryGetRarity(heroId, out Rarity rarity) ? rarity : Rarity.Common;
        }

        public string HeroName(string heroId)
        {
            HeroData data = HeroData(heroId);
            return data != null && !string.IsNullOrEmpty(data.displayName) ? data.displayName : heroId;
        }

        public void Message(string title, string message) => Confirm?.ShowMessage(title, message);
    }
}
