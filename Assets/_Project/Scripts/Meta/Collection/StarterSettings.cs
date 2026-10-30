using System.Collections.Generic;
using UnityEngine;

namespace FateBastion.Meta
{
    /// <summary>
    /// What a brand-new player starts with (S8, B-11): five starter heroes, gems and the default deck. The starter
    /// heroes are written into ownedEver, so they never give the first-own gems.
    /// </summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Settings/Starter", fileName = "StarterSettings")]
    public class StarterSettings : ScriptableObject
    {
        [Tooltip("Hero ids owned from the start (1 copy each).")]
        [SerializeField] private List<string> _starterHeroes = new List<string>
        {
            "hero_cung_thu", "hero_phu_thuy_lua", "hero_linh_bang", "hero_tu_si", "hero_nguoi_tuyet"
        };

        [SerializeField, Min(0)] private int _startingGems = 1000;

        [Tooltip("Deck of a new player; must be 5 different heroes taken from the starter heroes.")]
        [SerializeField] private List<string> _defaultDeck = new List<string>
        {
            "hero_cung_thu", "hero_phu_thuy_lua", "hero_linh_bang", "hero_tu_si", "hero_nguoi_tuyet"
        };

        public IReadOnlyList<string> StarterHeroes => _starterHeroes;
        public int StartingGems => _startingGems;
        public IReadOnlyList<string> DefaultDeck => _defaultDeck;

        /// <summary>For tests and tools; in the game the values come from the asset.</summary>
        public void Configure(IEnumerable<string> starterHeroes, int startingGems, IEnumerable<string> defaultDeck)
        {
            _starterHeroes = new List<string>(starterHeroes);
            _startingGems = Mathf.Max(0, startingGems);
            _defaultDeck = new List<string>(defaultDeck);
        }
    }
}
