using System.Collections.Generic;
using UnityEngine;

namespace FateBastion.Heroes
{
    /// <summary>
    /// Every HeroData of the game by id, so the saved deck (hero ids, S9) becomes the match deck and the lobby can show
    /// names and icons. Filled by Tools > Meta > Build Hero Catalog from Data/Heroes; never edited by hand.
    /// </summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Settings/Hero Catalog", fileName = "HeroCatalog")]
    public class HeroCatalog : ScriptableObject
    {
        [SerializeField] private List<HeroData> _heroes = new List<HeroData>();

        private Dictionary<string, HeroData> _byId;

        public IReadOnlyList<HeroData> Heroes => _heroes;

        public bool TryGet(string heroId, out HeroData data)
        {
            data = null;
            if (string.IsNullOrEmpty(heroId)) return false;

            if (_byId == null) BuildIndex();
            return _byId.TryGetValue(heroId, out data);
        }

        /// <summary>The HeroData of each id in order; unknown ids become null (an empty deck slot).</summary>
        public HeroData[] Resolve(IReadOnlyList<string> heroIds)
        {
            int count = heroIds != null ? heroIds.Count : 0;
            var result = new HeroData[count];
            for (int i = 0; i < count; i++)
            {
                if (!TryGet(heroIds[i], out result[i]))
                {
                    Debug.LogError($"{name}: hero id '{heroIds[i]}' is not in the catalog.", this);
                }
            }

            return result;
        }

        /// <summary>For tests and tools.</summary>
        public void SetHeroes(IEnumerable<HeroData> heroes)
        {
            _heroes = new List<HeroData>(heroes);
            _byId = null;
        }

        private void BuildIndex()
        {
            _byId = new Dictionary<string, HeroData>(_heroes.Count);
            foreach (HeroData hero in _heroes)
            {
                if (hero == null || string.IsNullOrEmpty(hero.id)) continue;
                if (_byId.ContainsKey(hero.id)) Debug.LogError($"{name}: duplicate hero id '{hero.id}'.", this);
                else _byId.Add(hero.id, hero);
            }
        }

        private void OnValidate() => _byId = null;
    }
}
