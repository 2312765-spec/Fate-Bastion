using FateBastion.Core;
using UnityEngine;

namespace FateBastion.Audio
{
    /// <summary>
    /// Every game sound the Audio module plays on its own, by event (S11 list). Empty slots are skipped silently,
    /// so sounds can be added one by one. Assigned by C at Data/Settings/SoundLibrary.
    /// </summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Audio/Sound Library", fileName = "SoundLibrary")]
    public class SoundLibrary : ScriptableObject
    {
        [Header("Heroes")]
        public SoundData heroPlace;
        public SoundData heroUpgrade;
        public SoundData heroLevel3;
        public SoundData heroSell;

        [Tooltip("Attack sound for a hero prefab whose HeroSoundSet leaves its own attack empty.")]
        public SoundData defaultAttack;

        [Tooltip("Ultimate sound for a hero prefab whose HeroSoundSet leaves its own ultimate empty.")]
        public SoundData defaultUltimate;

        [Header("Summoner")]
        public SoundData footstep;
        public SoundData meteorFall;
        public SoundData meteorExplode;
        public SoundData meteorReady;

        [Header("Enemies")]
        public SoundData enemyHit;
        public SoundData enemyDeath;
        public SoundData bossDeath;
        public SoundData castleHit;
        public SoundData bossAppear;

        [Header("Match")]
        public SoundData waveStart;
        public SoundData waveClear;
        public SoundData victory;
        public SoundData defeat;

        [Header("Lobby")]
        [Tooltip("Gacha reveal per rarity, indexed by Rarity (Common, Rare, Epic, Legendary).")]
        public SoundData[] gachaReveal = new SoundData[4];

        [Header("UI")]
        public SoundData uiClick;
        public SoundData panelOpen;
        public SoundData panelClose;
        public SoundData error;
        public SoundData merge;

        public SoundData Get(UiSound cue)
        {
            switch (cue)
            {
                case UiSound.Click: return uiClick;
                case UiSound.PanelOpen: return panelOpen;
                case UiSound.PanelClose: return panelClose;
                case UiSound.Error: return error;
                case UiSound.Merge: return merge;
                default: return null;
            }
        }

        public SoundData GetGachaReveal(Rarity rarity)
        {
            int index = (int)rarity;
            if (gachaReveal == null || index < 0 || index >= gachaReveal.Length) return null;
            return gachaReveal[index];
        }
    }
}
