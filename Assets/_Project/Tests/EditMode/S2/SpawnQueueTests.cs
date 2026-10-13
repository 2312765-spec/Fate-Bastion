using NUnit.Framework;
using UnityEngine;
using FateBastion.Enemies;

namespace FateBastion.Tests.EditMode.S2
{
    /// <summary>At most N enemies alive at once; the rest wait in FIFO order (S2).</summary>
    public class SpawnQueueTests
    {
        private EnemyData _a;
        private EnemyData _b;
        private SpawnQueue _queue;

        [SetUp]
        public void SetUp()
        {
            _a = ScriptableObject.CreateInstance<EnemyData>();
            _b = ScriptableObject.CreateInstance<EnemyData>();
            _queue = new SpawnQueue(3);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_a);
            Object.DestroyImmediate(_b);
        }

        [Test]
        public void Admit_BelowCap_SpawnNowAndNotQueued()
        {
            Assert.IsTrue(_queue.Admit(new SpawnRequest(_a, 0, 1), activeCount: 2));
            Assert.AreEqual(0, _queue.Count);
        }

        [Test]
        public void Admit_AtCap_QueuedAndNotSpawned()
        {
            Assert.IsFalse(_queue.Admit(new SpawnRequest(_a, 0, 1), activeCount: 3));
            Assert.AreEqual(1, _queue.Count);
        }

        [Test]
        public void Admit_QueueNotEmpty_QueuesEvenBelowCap()
        {
            // Keeps spawn order: a new request must not overtake the ones already waiting.
            _queue.Admit(new SpawnRequest(_a, 0, 1), activeCount: 3);
            Assert.IsFalse(_queue.Admit(new SpawnRequest(_b, 0, 1), activeCount: 2));
            Assert.AreEqual(2, _queue.Count);
        }

        [Test]
        public void TryRelease_FreeSlot_ReturnsOldestFirst()
        {
            _queue.Admit(new SpawnRequest(_a, 0, 1), activeCount: 3);
            _queue.Admit(new SpawnRequest(_b, 1, 2), activeCount: 3);

            Assert.IsTrue(_queue.TryRelease(activeCount: 2, out SpawnRequest first));
            Assert.AreEqual(_a, first.Enemy);
            Assert.IsTrue(_queue.TryRelease(activeCount: 2, out SpawnRequest second));
            Assert.AreEqual(_b, second.Enemy);
            Assert.AreEqual(1, second.PathIndex);
            Assert.AreEqual(2, second.Wave);
            Assert.AreEqual(0, _queue.Count);
        }

        [Test]
        public void TryRelease_AtCap_ReturnsFalse()
        {
            _queue.Admit(new SpawnRequest(_a, 0, 1), activeCount: 3);
            Assert.IsFalse(_queue.TryRelease(activeCount: 3, out _));
            Assert.AreEqual(1, _queue.Count);
        }

        [Test]
        public void TryRelease_Empty_ReturnsFalse()
        {
            Assert.IsFalse(_queue.TryRelease(activeCount: 0, out _));
        }

        [Test]
        public void Clear_EmptiesQueue()
        {
            _queue.Admit(new SpawnRequest(_a, 0, 1), activeCount: 3);
            _queue.Admit(new SpawnRequest(_b, 0, 1), activeCount: 3);
            _queue.Clear();
            Assert.AreEqual(0, _queue.Count);
            Assert.IsTrue(_queue.Admit(new SpawnRequest(_a, 0, 1), activeCount: 0));
        }

        [Test]
        public void Constructor_CapBelowOne_ClampedToOne()
        {
            var queue = new SpawnQueue(0);
            Assert.AreEqual(1, queue.MaxActive);
            Assert.IsTrue(queue.Admit(new SpawnRequest(_a, 0, 1), activeCount: 0));
            Assert.IsFalse(queue.Admit(new SpawnRequest(_a, 0, 1), activeCount: 1));
        }
    }
}
