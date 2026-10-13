using UnityEngine;
using UnityEngine.InputSystem;
using FateBastion.Player;

namespace FateBastion.Tests.PlayMode.S3
{
    /// <summary>
    /// Builds an InputActionAsset in code with the same map and action names as FateBastion.inputactions,
    /// so tests do not depend on the project asset. Destroy the result in TearDown.
    /// </summary>
    public static class TestInputAssetFactory
    {
        public static InputActionAsset Create(bool includeUiMap = true)
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();

            InputActionMap summoner = asset.AddActionMap(InputActionNames.SummonerMap);
            summoner.AddAction(InputActionNames.Move, InputActionType.Value, "<Keyboard>/w");
            summoner.AddAction(InputActionNames.Sprint, InputActionType.Button, "<Keyboard>/leftShift");
            summoner.AddAction(InputActionNames.Look, InputActionType.Button, "<Mouse>/rightButton", "Hold(duration=0.2)");
            summoner.AddAction(InputActionNames.LookDelta, InputActionType.Value, "<Mouse>/delta");
            summoner.AddAction(InputActionNames.Zoom, InputActionType.Value, "<Mouse>/scroll/y");
            summoner.AddAction(InputActionNames.Select, InputActionType.Button, "<Mouse>/leftButton");
            summoner.AddAction(InputActionNames.Cancel, InputActionType.Button, "<Keyboard>/escape");
            for (int i = 0; i < InputActionNames.DeckSlots.Length; i++)
            {
                summoner.AddAction(InputActionNames.DeckSlots[i], InputActionType.Button, "<Keyboard>/" + (i + 1));
            }

            summoner.AddAction(InputActionNames.Meteor, InputActionType.Button, "<Keyboard>/q");
            summoner.AddAction(InputActionNames.Upgrade, InputActionType.Button, "<Keyboard>/u");
            summoner.AddAction(InputActionNames.Sell, InputActionType.Button, "<Keyboard>/x");
            summoner.AddAction(InputActionNames.Ultimate, InputActionType.Button, "<Keyboard>/r");
            summoner.AddAction(InputActionNames.SkipWave, InputActionType.Button, "<Keyboard>/n");
            summoner.AddAction(InputActionNames.Debug, InputActionType.Button, "<Keyboard>/f1");

            if (includeUiMap)
            {
                InputActionMap ui = asset.AddActionMap(InputActionNames.UIMap);
                ui.AddAction(InputActionNames.Navigate, InputActionType.Value, "<Keyboard>/upArrow");
                ui.AddAction(InputActionNames.Submit, InputActionType.Button, "<Keyboard>/enter");
                ui.AddAction(InputActionNames.Cancel, InputActionType.Button, "<Keyboard>/escape");
                ui.AddAction(InputActionNames.Point, InputActionType.Value, "<Mouse>/position");
                ui.AddAction(InputActionNames.Click, InputActionType.Button, "<Mouse>/leftButton");
            }

            return asset;
        }
    }
}
