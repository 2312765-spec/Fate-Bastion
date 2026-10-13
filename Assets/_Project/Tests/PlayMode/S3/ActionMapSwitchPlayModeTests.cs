using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using FateBastion.Core;
using FateBastion.Player;

namespace FateBastion.Tests.PlayMode.S3
{
    /// <summary>SummonerInputRouter keeps exactly one action map enabled and releases held input on switch (S3).</summary>
    public class ActionMapSwitchPlayModeTests
    {
        private GameObject _go;
        private SummonerInputRouter _router;
        private InputActionAsset _asset;

        [SetUp]
        public void SetUp()
        {
            _asset = TestInputAssetFactory.Create();
            _go = new GameObject("SummonerInputRouter (test)");
            _router = _go.AddComponent<SummonerInputRouter>();
            _router.Initialize(_asset);
        }

        [TearDown]
        public void TearDown()
        {
            // Immediate, so InputRouter.Instance is cleared before the next test creates a router.
            if (_go != null) Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_asset);
        }

        private InputActionMap Summoner => _asset.FindActionMap(InputActionNames.SummonerMap, true);
        private InputActionMap Ui => _asset.FindActionMap(InputActionNames.UIMap, true);

        private int EnabledMapCount()
        {
            int count = 0;
            foreach (InputActionMap map in _asset.actionMaps)
            {
                if (map.enabled) count++;
            }

            return count;
        }

        [Test]
        public void Initialize_Default_EnablesOnlySummoner()
        {
            Assert.AreSame(_router, InputRouter.Instance);
            Assert.AreEqual(ActionMapId.Summoner, _router.ActiveMap);
            Assert.IsTrue(Summoner.enabled);
            Assert.IsFalse(Ui.enabled);
        }

        [Test]
        public void SetActionMap_UI_EnablesOnlyUI()
        {
            _router.SetActionMap(ActionMapId.UI);

            Assert.AreEqual(ActionMapId.UI, _router.ActiveMap);
            Assert.IsFalse(Summoner.enabled);
            Assert.IsTrue(Ui.enabled);
        }

        [Test]
        public void SetActionMap_UIThenSummoner_EnablesOnlySummoner()
        {
            _router.SetActionMap(ActionMapId.UI);
            _router.SetActionMap(ActionMapId.Summoner);

            Assert.IsTrue(Summoner.enabled);
            Assert.IsFalse(Ui.enabled);
        }

        [Test]
        public void SetActionMap_AnySequence_ExactlyOneMapEnabled()
        {
            ActionMapId[] sequence =
            {
                ActionMapId.UI, ActionMapId.UI, ActionMapId.Summoner, ActionMapId.Summoner, ActionMapId.UI,
                ActionMapId.Summoner
            };

            foreach (ActionMapId map in sequence)
            {
                _router.SetActionMap(map);
                Assert.AreEqual(1, EnabledMapCount(), $"after switching to {map}");
            }
        }

        [Test]
        public void SetActionMap_SameMapTwice_RaisesChangedOnce()
        {
            int changed = 0;
            _router.ActionMapChanged += _ => changed++;

            _router.SetActionMap(ActionMapId.UI);
            _router.SetActionMap(ActionMapId.UI);

            Assert.AreEqual(1, changed);
        }

        [Test]
        public void SetActionMap_LeaveSummoner_ReleasesMoveSprintAndLook()
        {
            Vector2 lastMove = Vector2.one;
            bool sprint = true;
            int moveCount = 0;
            _router.Move += v => { lastMove = v; moveCount++; };
            _router.SprintChanged += s => sprint = s;

            _router.SetActionMap(ActionMapId.UI);

            Assert.AreEqual(1, moveCount, "a zero move is sent so the Summoner stops while the map is off");
            Assert.AreEqual(Vector2.zero, lastMove);
            Assert.IsFalse(sprint);
            Assert.IsFalse(_router.IsLookHeld);
        }

        [Test]
        public void Initialize_MissingUIMap_LogsError()
        {
            // Replace the SetUp router so the new one becomes InputRouter.Instance.
            Object.DestroyImmediate(_go);
            InputActionAsset broken = TestInputAssetFactory.Create(includeUiMap: false);
            _go = new GameObject("SummonerInputRouter (broken asset)");
            var router = _go.AddComponent<SummonerInputRouter>();
            try
            {
                LogAssert.Expect(LogType.Error, new Regex(InputActionNames.UIMap));
                router.Initialize(broken);
            }
            finally
            {
                Object.DestroyImmediate(broken);
            }
        }
    }
}
