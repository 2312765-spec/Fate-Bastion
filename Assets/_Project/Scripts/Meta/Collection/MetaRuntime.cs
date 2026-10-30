using System;
using System.Collections.Generic;
using UnityEngine;

namespace FateBastion.Meta
{
    /// <summary>
    /// The one <see cref="MetaSession"/> of the running game, shared by the lobby, the match (rewards) and the F1 debug
    /// panel. Created on first use from the GachaTable and StarterSettings assets: SaveService at
    /// Application.persistentDataPath, Load (save.json → save.bak → new save), then SaveSanitizer with the real roster.
    /// Static state survives scene loads; tests call <see cref="ResetAll"/>.
    /// </summary>
    public static class MetaRuntime
    {
        public static MetaSession Session { get; private set; }

        /// <summary>Warnings of the last load (file recovered, deck repaired…) for the lobby to show once.</summary>
        public static IReadOnlyList<string> LoadWarnings { get; private set; } = Array.Empty<string>();

        /// <summary>Raised when the session is created or rebuilt (debug "delete save").</summary>
        public static event Action<MetaSession> SessionChanged;

        private static GachaTable _table;
        private static StarterSettings _starter;
        private static string _directory;

        public static bool HasSession => Session != null;

        /// <summary>Returns the session, creating it the first time. <paramref name="directory"/> null = persistentDataPath.</summary>
        public static MetaSession Ensure(GachaTable table, StarterSettings starter, string directory = null)
        {
            if (Session != null) return Session;
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (starter == null) throw new ArgumentNullException(nameof(starter));

            _table = table;
            _starter = starter;
            _directory = string.IsNullOrEmpty(directory) ? Application.persistentDataPath : directory;
            Build();
            return Session;
        }

        /// <summary>Debug panel (B-16): deletes save.json/.bak/.tmp and starts again from StarterSettings.</summary>
        public static void DeleteSaveAndRestart()
        {
            if (Session == null) return;
            Session.SaveService.Delete();
            Build();
        }

        /// <summary>The lobby showed the warnings; do not show them again after a match.</summary>
        public static void ClearLoadWarnings() => LoadWarnings = Array.Empty<string>();

        /// <summary>Static state survives tests and scene loads; call in [TearDown].</summary>
        public static void ResetAll()
        {
            Session = null;
            LoadWarnings = Array.Empty<string>();
            SessionChanged = null;
            _table = null;
            _starter = null;
            _directory = null;
        }

        private static void Build()
        {
            var save = new SaveService(_directory, new StarterSaveFactory(_starter));
            SaveLoadResult loaded = save.Load();

            var roster = new List<string>(_table.Heroes.Count);
            foreach (GachaTable.HeroEntry entry in _table.Heroes) roster.Add(entry.heroId);

            var warnings = new List<string>();
            if (loaded.Recovered && !string.IsNullOrEmpty(loaded.Error)) warnings.Add(loaded.Error);
            List<string> fixes = SaveSanitizer.Sanitize(save.Data, roster, _starter.StarterHeroes);
            warnings.AddRange(fixes);
            if (fixes.Count > 0 || loaded.Kind == SaveLoadKind.CreatedNew) save.Save();

            var rng = new System.Random();
            Session = new MetaSession(save, new GachaService(_table, rng), new CollectionService(_table, rng), new DeckService());
            LoadWarnings = warnings;
            SessionChanged?.Invoke(Session);
        }
    }
}
