using System;

namespace FateBastion.Meta
{
    /// <summary>
    /// Builds a new player's SaveData from StarterSettings (S8, B-11). Implements the IStarterSaveFactory that A's
    /// SaveService already asks for (A-15 wires it: new SaveService(dir, new StarterSaveFactory(settings))).
    /// </summary>
    public sealed class StarterSaveFactory : IStarterSaveFactory
    {
        private readonly StarterSettings _settings;

        public StarterSaveFactory(StarterSettings settings)
        {
            _settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
        }

        public SaveData CreateNew()
        {
            var data = new SaveData { gems = _settings.StartingGems, pityCounter = 0 };
            foreach (string heroId in _settings.StarterHeroes)
            {
                if (string.IsNullOrEmpty(heroId) || data.collection.ContainsKey(heroId))
                {
                    continue;
                }

                data.collection[heroId] = 1;
                data.ownedEver.Add(heroId);
            }

            foreach (string heroId in _settings.DefaultDeck)
            {
                data.deck.Add(heroId);
            }

            return data;
        }
    }
}
