using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FateBastion.Enemies;

namespace FateBastion.UI
{
    /// <summary>One LevelCard of C's layout: title, 3 stars, Endless record, enemy preview rows, lock overlay, selection.</summary>
    public sealed class LevelCardView
    {
        private const int MaxStars = 3;

        private readonly TMP_Text _title;
        private readonly Transform[] _starsOn = new Transform[MaxStars];
        private readonly Transform _stars;
        private readonly TMP_Text _record;
        private readonly Transform _enemies;
        private readonly Transform _lock;
        private readonly TMP_Text _lockText;
        private readonly Transform _selected;

        public LevelCardView(Transform root)
        {
            Root = root;
            Button = root.GetComponent<Button>();
            _title = UiFind.Optional<TMP_Text>(root, "Content/TitleText");
            _stars = root.Find("Content/Stars");
            for (int i = 0; i < MaxStars; i++) _starsOn[i] = root.Find($"Content/Stars/Star_{i + 1}/On");
            _record = UiFind.Optional<TMP_Text>(root, "Content/RecordText");
            _enemies = root.Find("Content/Enemies");
            _lock = root.Find("LockOverlay");
            _lockText = UiFind.Optional<TMP_Text>(root, "LockOverlay/LockText");
            _selected = root.Find("SelectedFrame");
        }

        public Transform Root { get; }
        public Button Button { get; }

        public void SetTitle(string title)
        {
            if (_title != null) _title.text = title;
        }

        /// <summary>Campaign card: stars shown, record hidden.</summary>
        public void SetStars(int stars)
        {
            UiFind.SetActive(_stars, true);
            UiFind.SetActive(_record, false);
            for (int i = 0; i < MaxStars; i++) UiFind.SetActive(_starsOn[i], i < stars);
        }

        /// <summary>Endless card: record shown, stars hidden.</summary>
        public void SetRecord(int bestWave)
        {
            UiFind.SetActive(_stars, false);
            UiFind.SetActive(_record, true);
            if (_record != null) _record.text = bestWave > 0 ? $"Kỷ lục: wave {bestWave}" : "Chưa có kỷ lục";
        }

        /// <summary>Fills the enemy rows from LevelData.enemyTypesPreview; extra rows are hidden.</summary>
        public void SetEnemies(EnemyData[] enemies)
        {
            if (_enemies == null) return;
            for (int i = 0; i < _enemies.childCount; i++)
            {
                Transform row = _enemies.GetChild(i);
                EnemyData enemy = enemies != null && i < enemies.Length ? enemies[i] : null;
                row.gameObject.SetActive(enemy != null);
                if (enemy == null) continue;

                var name = UiFind.Optional<TMP_Text>(row, "NameText");
                if (name != null) name.text = string.IsNullOrEmpty(enemy.displayName) ? enemy.id : enemy.displayName;
                var icon = UiFind.Optional<Image>(row, "Icon");
                if (icon != null && enemy.icon != null)
                {
                    icon.sprite = enemy.icon;
                    icon.color = Color.white;
                }
            }
        }

        public void SetLocked(bool locked, string reason)
        {
            UiFind.SetActive(_lock, locked);
            if (locked && _lockText != null) _lockText.text = reason;
            if (Button != null) Button.interactable = !locked;
        }

        public void SetSelected(bool selected) => UiFind.SetActive(_selected, selected);
    }
}
