using UnityEngine;
using UnityEngine.Splines;

namespace FateBastion.Enemies
{
    /// <summary>
    /// The paths of one map (S2): ground path (pathIndex 0) and flying path (pathIndex 1). Lives on the map
    /// prefab Map_Level&lt;n&gt; (C drags the splines in); EnemyManager reads paths through it.
    /// </summary>
    public class LevelPaths : MonoBehaviour, IEnemyPathProvider
    {
        public const int MainPathIndex = 0;
        public const int FlyingPathIndex = 1;

        [Tooltip("Ground path from the Gate to the Castle (pathIndex 0), about 90 m.")]
        [SerializeField] private SplineContainer _mainPath;

        [Tooltip("Shorter path for flying enemies (pathIndex 1).")]
        [SerializeField] private SplineContainer _flyingPath;

        private SplineEnemyPath _main;
        private SplineEnemyPath _flying;

        /// <summary>For tests and Sandbox scenes that build the map in code. Call before the paths are used.</summary>
        public void Configure(SplineContainer mainPath, SplineContainer flyingPath)
        {
            _mainPath = mainPath;
            _flyingPath = flyingPath;
            _main = null;
            _flying = null;
        }

        private void Awake()
        {
            if (_mainPath == null) Debug.LogError($"{name}: kéo SplineContainer đường chính vào LevelPaths.", this);
        }

        public IEnemyPath GetPath(int pathIndex)
        {
            switch (pathIndex)
            {
                case MainPathIndex:
                    if (_mainPath == null) break;
                    return _main ??= new SplineEnemyPath(_mainPath);
                case FlyingPathIndex:
                    if (_flyingPath == null) break;
                    return _flying ??= new SplineEnemyPath(_flyingPath);
            }

            Debug.LogError($"{name}: LevelPaths không có đường cho pathIndex {pathIndex}.", this);
            return null;
        }
    }
}
