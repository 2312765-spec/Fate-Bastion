using System;
using System.Collections.Generic;
using UnityEngine;
using FateBastion.Core;
using FateBastion.Enemies;
using FateBastion.Heroes;
using FateBastion.Meta;
using FateBastion.Player;

namespace FateBastion.Game
{
    /// <summary>
    /// Runs one match (S5, B-06). Pure rules live in <see cref="MatchFlow"/>; this component only wires Unity:
    /// WaveManager.Init/StartWave/StopAndClear and OnWaveFinished (TEAM_ASSIGNMENT 3.1 #2), the Castle (#1), the pause
    /// overlay from InputRouter.PauseRequested (#6, Alt-Tab included), wave-end and skip gold through Wallet (#8),
    /// stars and gems at the end (S5, S9). Only GameManager raises GameEvents.
    /// </summary>
    // Runs first so the hub reset in Awake happens before any other Awake/OnEnable/Start of the scene subscribes.
    [DefaultExecutionOrder(-1000)]
    public sealed class GameManager : MonoBehaviour
    {
        // Esc entries that may be open when the match ends (placement preview, meteor aim, hero panel).
        private const int MaxCancelOnEnd = 8;

        [Header("Scene systems")]
        [SerializeField] private WaveManager _waveManager;
        [SerializeField] private EnemyManager _enemyManager;
        [SerializeField] private Castle _castle;
        [SerializeField] private PlacementSystem _placement;
        [SerializeField] private HeroPool _heroPool;
        [SerializeField] private GhostPreview _ghostPreview;
        [SerializeField] private TimeController _timeController;

        [Tooltip("Summoner's CommandAura (A). Empty = found in the scene; relayed as HeroEnteredAura for the tutorial.")]
        [SerializeField] private CommandAura _commandAura;

        [Tooltip("Spline paths of the map (C's Map_Level<n>). Empty = found in the scene once at load.")]
        [SerializeField] private LevelPaths _levelPaths;

        [Header("Data")]
        [SerializeField] private MatchEconomySettings _economy;
        [SerializeField] private EndlessRewardSettings _endlessRewards;
        [SerializeField] private MatchCatalog _catalog;
        [SerializeField] private HeroCatalog _heroCatalog;

        [Tooltip("With StarterSettings: creates the save session when the Game scene is opened without the lobby.")]
        [SerializeField] private GachaTable _gachaTable;
        [SerializeField] private StarterSettings _starterSettings;

        [Header("Used when the scene starts without a lobby request (Editor, Sandbox)")]
        [SerializeField] private LevelData _defaultLevel;
        [SerializeField] private bool _defaultEndless;

        [Tooltip("Deck when there is no save session or the saved deck is invalid.")]
        [SerializeField] private HeroData[] _defaultDeck = new HeroData[0];

        private InputRouter _router;
        private Action _onPauseRequested;
        private Action _onSkipWave;
        private Action<int> _onWaveFinished;
        private Action _onCastleDestroyed;
        private MetaSession _session;
        private ActionMapId _mapBeforePause = ActionMapId.Summoner;
        private bool _readyPending;

        public static GameManager Instance { get; private set; }

        /// <summary>Pause overlay opened (true) or closed (false). MatchMenus (UI) opens the pause screen from it.</summary>
        public event Action<bool> PausedChanged;

        /// <summary>
        /// Skip needs the shared confirm dialog (S5, S10). The listener shows it and calls <see cref="ConfirmSkip"/>;
        /// with no listener the skip happens at once.
        /// </summary>
        public event Action SkipConfirmRequested;

        /// <summary>
        /// Relay of CommandAura.OnHeroEnteredAura (TEAM_ASSIGNMENT 3.1 #10) for UI listeners such as the level-1
        /// tutorial (B-17), whose assembly does not reference Player.
        /// </summary>
        public event Action<GameObject> HeroEnteredAura;

        public MatchFlow Flow { get; private set; }
        public Wallet Wallet { get; private set; }
        public MatchDeck Deck { get; private set; }
        public LevelData Level { get; private set; }
        public bool IsEndless { get; private set; }
        public bool IsPaused { get; private set; }
        public Castle Castle => _castle;
        public PlacementSystem Placement => _placement;
        public TimeController TimeControl => _timeController;

        /// <summary>Set when the match ends; the result screen reads it with GameEvents.OnMatchEnded.</summary>
        public MatchResult LastResult { get; private set; }

        /// <summary>The Endless run beat the saved record.</summary>
        public bool LastResultIsNewRecord { get; private set; }

        public bool CanSkip => Flow != null && Flow.CanSkip && !IsPaused;

        /// <summary>Settings "Hỏi khi bỏ qua wave" (S9 skipConfirm); true without a save session.</summary>
        public bool AskBeforeSkip => _session == null || _session.Data.settings == null || _session.Data.settings.skipConfirm;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError($"{nameof(GameManager)}: a second instance on '{name}'; destroying it.", this);
                Destroy(this);
                return;
            }

            Instance = this;

            // Static hubs survive scene loads (Retry, back from the lobby): drop listeners of destroyed objects before
            // anyone in this scene subscribes. Listeners may then use Awake, OnEnable or Start freely.
            GameEvents.ResetAll();
            CombatEvents.ResetAll();
            HeroEvents.ResetAll();
            AbilityEvents.ResetAll();
            CancelStack.ResetAll();

            _onPauseRequested = Pause;
            _onSkipWave = RequestSkip;
            _onWaveFinished = HandleWaveFinished;
            _onCastleDestroyed = HandleCastleDestroyed;
        }

        private void Start()
        {
            SubscribeInput();
            SubscribeAura();
            BeginLoading();
        }

        private void OnDestroy()
        {
            UnsubscribeInput();
            if (_commandAura != null) _commandAura.OnHeroEnteredAura -= RelayHeroEnteredAura;
            if (_waveManager != null) _waveManager.OnWaveFinished -= _onWaveFinished;
            if (_castle != null) _castle.Destroyed -= _onCastleDestroyed;
            Wallet?.StopListeningToKills();
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (_readyPending)
            {
                // One frame after every Start, so late subscribers get the first Castle HP and the Preparing state.
                _readyPending = false;
                _castle.Init(Level.castleHP);
                Flow.MarkReady();
                return;
            }

            if (Flow != null && Flow.CanSkip) Flow.Tick(UnityEngine.Time.deltaTime);
        }

        // ---------- Loading ----------

        private void BeginLoading()
        {
            if (!CheckReferences()) return;

            _session = ResolveSession();
            IsEndless = MatchRequest.HasRequest ? MatchRequest.IsEndless : _defaultEndless;
            Level = MatchRequest.HasRequest ? MatchRequest.Level : _defaultLevel;
            if (IsEndless && _catalog != null && _catalog.EndlessLevel != null) Level = _catalog.EndlessLevel;
            if (Level == null || Level.waves == null || Level.waves.Length == 0)
            {
                Debug.LogError($"{name}: no LevelData to play (lobby request or Default Level).", this);
                return;
            }

            _timeController.ResetForMatch();

            Deck = new MatchDeck(ResolveDeck());
            List<HeroData> heroes = Deck.DistinctHeroes();
            _heroPool.Prewarm(heroes, _placement.Rules, _placement.LevelTable, _enemyManager);
            if (_ghostPreview != null) _ghostPreview.Prewarm(heroes);

            Wallet = new Wallet(Level.startingGold);
            Wallet.ListenToKills();
            _placement.BeginMatch(Deck, Wallet);

            // ASSUMPTION: _GameSystems is a prefab and cannot reference the map, so when the scene has not assigned
            // _levelPaths it is looked up once here, at load (never during play; CLAUDE.md 5 otherwise forbids it).
            if (_levelPaths == null) _levelPaths = FindAnyObjectByType<LevelPaths>();
            LevelPaths paths = _levelPaths;
            if (paths != null) _enemyManager.SetPaths(paths);
            else Debug.LogWarning($"{name}: no LevelPaths in the scene (Map_Level<n>); enemies have no path.", this);

            _waveManager.OnWaveFinished += _onWaveFinished;
            _waveManager.Init(Level, IsEndless && _catalog != null ? _catalog.EndlessSettings : null);
            _castle.Destroyed += _onCastleDestroyed;

            Flow = new MatchFlow(Level.waves.Length, IsEndless, Level.prepareSeconds, Level.restSeconds);
            Flow.StateChanged += GameEvents.RaiseStateChanged;
            Flow.WaveStartRequested += HandleWaveStartRequested;
            Flow.WaveCleared += HandleWaveCleared;
            Flow.Ended += FinishMatch;

            if (_router != null) _router.SetActionMap(ActionMapId.Summoner);
            _readyPending = true;
        }

        private bool CheckReferences()
        {
            bool ok = _waveManager != null && _enemyManager != null && _castle != null && _placement != null && _heroPool != null &&
                      _timeController != null && _economy != null;
            if (!ok)
            {
                Debug.LogError($"{name}: GameManager needs WaveManager, EnemyManager, Castle, PlacementSystem, HeroPool, " +
                               "TimeController and MatchEconomySettings.", this);
            }

            return ok;
        }

        private MetaSession ResolveSession()
        {
            if (MetaRuntime.HasSession) return MetaRuntime.Session;
            if (_gachaTable == null || _starterSettings == null)
            {
                Debug.LogWarning($"{name}: no save session (GachaTable/StarterSettings empty); rewards are not saved.", this);
                return null;
            }

            return MetaRuntime.Ensure(_gachaTable, _starterSettings);
        }

        private IReadOnlyList<HeroData> ResolveDeck()
        {
            if (_session != null && _heroCatalog != null && _session.Deck.CanStartMatch(_session.Data))
            {
                HeroData[] deck = _heroCatalog.Resolve(_session.Data.deck);
                if (Array.TrueForAll(deck, hero => hero != null)) return deck;
            }

            if (_defaultDeck == null || _defaultDeck.Length == 0)
            {
                Debug.LogError($"{name}: no valid saved deck and no Default Deck; nothing can be placed.", this);
            }

            return _defaultDeck;
        }

        // ---------- Flow events ----------

        private void HandleWaveStartRequested(int wave)
        {
            _waveManager.StartWave(wave);
            GameEvents.RaiseWaveStarted(wave);
        }

        private void HandleWaveCleared(int wave)
        {
            Wallet.Add(MatchEconomy.WaveClearGold(_economy, wave));
            GameEvents.RaiseWaveCleared(wave);
        }

        private void HandleWaveFinished(int wave) => Flow?.NotifyWaveFinished(wave, _castle.IsAlive);

        private void HandleCastleDestroyed() => Flow?.NotifyCastleDestroyed();

        private void FinishMatch(bool won)
        {
            // S5: losing while aiming the meteor cancels the aim first; same for the placement preview.
            for (int i = 0; i < MaxCancelOnEnd && CancelStack.TryCancelTop(); i++)
            {
            }

            if (IsPaused)
            {
                IsPaused = false;
                PausedChanged?.Invoke(false);
            }

            _waveManager.StopAndClear();
            _placement.EndMatch();
            Wallet.StopListeningToKills();
            Wallet.Reset(0);
            _timeController.ResetForMatch();

            int castleLeft = _castle.CurrentHP;
            int stars = won ? StarRating.Calculate(castleLeft, Level) : 0;
            int waves = IsEndless ? Flow.WavesCleared : Flow.CurrentWave;
            int gems = 0;
            LastResultIsNewRecord = false;

            if (_session != null)
            {
                SaveData data = _session.Data;
                if (IsEndless)
                {
                    LastResultIsNewRecord = waves > data.endlessBest;
                    if (_endlessRewards != null) gems = MatchRewards.ApplyEndless(data, waves, _endlessRewards);
                    else Debug.LogError($"{name}: EndlessRewardSettings is empty; no Endless gems.", this);
                }
                else
                {
                    gems = MatchRewards.ApplyCampaign(data, Level, stars);
                }

                _session.SaveNow(); // S9: save when the result screen shows.
            }

            if (_router != null) _router.SetActionMap(ActionMapId.UI);
            LastResult = new MatchResult(won, stars, waves, castleLeft, gems, IsEndless);
            GameEvents.RaiseMatchEnded(LastResult);
        }

        // ---------- Pause and skip ----------

        /// <summary>Pause overlay (S5): timeScale 0 and the UI action map; Resume restores both.</summary>
        public void Pause()
        {
            if (IsPaused || Flow == null || !Flow.IsPlaying) return;

            IsPaused = true;
            _mapBeforePause = _router != null ? _router.ActiveMap : ActionMapId.Summoner;
            if (_router != null) _router.SetActionMap(ActionMapId.UI);
            _timeController.Pause();
            PausedChanged?.Invoke(true);
        }

        public void Resume()
        {
            if (!IsPaused) return;

            IsPaused = false;
            _timeController.Resume();
            if (_router != null) _router.SetActionMap(_mapBeforePause);
            PausedChanged?.Invoke(false);
        }

        /// <summary>Skip key (N) or the HUD button: asks first when the setting says so and a dialog listens.</summary>
        public void RequestSkip()
        {
            if (!CanSkip) return;
            if (AskBeforeSkip && SkipConfirmRequested != null) SkipConfirmRequested.Invoke();
            else ConfirmSkip();
        }

        /// <summary>Skips the wait now; +skip gold when the rest between waves was skipped.</summary>
        public void ConfirmSkip()
        {
            if (!CanSkip) return;
            if (Flow.Skip(out bool paysGold) && paysGold) Wallet.Add(_economy.SkipGold);
        }

        /// <summary>The dialog's "Không hỏi lại" box (S9 settings.skipConfirm).</summary>
        public void SetAskBeforeSkip(bool ask)
        {
            if (_session == null || _session.Data.settings == null || _session.Data.settings.skipConfirm == ask) return;
            _session.Data.settings.skipConfirm = ask;
            _session.SaveNow();
        }

        /// <summary>Result screen "Chơi lại".</summary>
        public void Retry()
        {
            if (!MatchRequest.HasRequest) MatchRequest.Set(Level, IsEndless);
            MatchRequest.Retry();
        }

        /// <summary>Pause "Về sảnh" or result "Về sảnh". Leaving mid-match gives nothing and saves nothing (S5).</summary>
        public void ReturnToLobby()
        {
            _timeController.ResetForMatch();
            MatchRequest.ReturnToLobby();
        }

        // ---------- Input ----------

        private void SubscribeAura()
        {
            // ASSUMPTION: same as _levelPaths – the Summoner lives in the scene, so a missing reference is looked up once.
            if (_commandAura == null) _commandAura = FindAnyObjectByType<CommandAura>();
            if (_commandAura != null) _commandAura.OnHeroEnteredAura += RelayHeroEnteredAura;
        }

        private void RelayHeroEnteredAura(GameObject hero) => HeroEnteredAura?.Invoke(hero);

        private void SubscribeInput()
        {
            _router = InputRouter.Instance;
            if (_router == null)
            {
                Debug.LogWarning($"{name}: no InputRouter; pause and skip keys are off.", this);
                return;
            }

            _router.PauseRequested += _onPauseRequested;
            _router.SkipWave += _onSkipWave;
        }

        private void UnsubscribeInput()
        {
            if (_router == null) return;
            _router.PauseRequested -= _onPauseRequested;
            _router.SkipWave -= _onSkipWave;
            _router = null;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // ---------- Debug panel (B-16) ----------

        /// <summary>Clears the field and starts wave <paramref name="wave"/> now.</summary>
        public void DebugJumpToWave(int wave)
        {
            if (Flow == null || !Flow.IsPlaying) return;
            _waveManager.StopAndClear();
            Flow.DebugJumpTo(wave);
        }

        /// <summary>Ends the match now (win keeps the current Castle HP for the stars).</summary>
        public void DebugEnd(bool won)
        {
            if (Flow == null || !Flow.IsPlaying) return;
            Flow.DebugEnd(won);
        }
#endif
    }
}
