using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FateBastion.Audio;

namespace FateBastion.Tests.EditMode.S11
{
    /// <summary>When a pooled one-shot effect may return to its pool (C-09).</summary>
    public class FxLifetimeTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [Test]
        public void Of_ChildOutlivesRoot_UsesLongestLayer()
        {
            _root = new GameObject("Fx");
            Configure(_root.AddComponent<ParticleSystem>(), duration: 0.1f, lifetime: 0.2f, delay: 0f);
            var child = new GameObject("Smoke");
            child.transform.SetParent(_root.transform);
            Configure(child.AddComponent<ParticleSystem>(), duration: 0.5f, lifetime: 1f, delay: 0.25f);

            Assert.AreEqual(1.75f, FxLifetime.Of(_root.GetComponent<ParticleSystem>()), 1e-4f);
        }

        [Test]
        public void Of_LoopingLayer_LogsError()
        {
            _root = new GameObject("Fx");
            var system = _root.AddComponent<ParticleSystem>();
            Configure(system, duration: 1f, lifetime: 1f, delay: 0f);
            ParticleSystem.MainModule main = system.main;
            main.loop = true;

            LogAssert.Expect(LogType.Error, new Regex("loops"));
            FxLifetime.Of(system);
        }

        private static void Configure(ParticleSystem system, float duration, float lifetime, float delay)
        {
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.duration = duration;
            main.startLifetime = lifetime;
            main.startDelay = delay;
        }
    }
}
