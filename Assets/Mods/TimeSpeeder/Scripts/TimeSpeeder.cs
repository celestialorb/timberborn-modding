using System;
using System.Collections.Generic;
using System.Linq;

using HarmonyLib;
using Timberborn.ModManagerScene;
using Timberborn.TimeSpeedButtonSystem;
using Timberborn.TimeSystemUI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Timberborn.Mods.Daxisaurus.TimeSpeeder {
    internal class TimeSpeederPlaceholder : IModStarter {
        public void StartMod(IModEnvironment modEnvironment) {
            UnityEngine.Debug.Log("TimeSpeeder mod started!");

            var harmony = new Harmony("com.daxisaurus.timberborn.timespeeder");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(TimeSpeedButtonFactory), nameof(TimeSpeedButtonFactory.Create))]
    public static class TimeSpeedButtonFactoryCreatePatch
    {
        // public static void Prefix(

        // )
        // static bool Prefix(
        // TimeSpeedButtonFactory __instance,
        // Button button,
        // int index,
        // Action<int> clickCallback,
        // ref TimeSpeedButton __result)
        // {
        //     // Map index to your config speeds
        //     int speed = index switch
        //     {
        //         0 => 0,
        //         1 => TimeSpeederConfig.NormalSpeed,
        //         2 => TimeSpeederConfig.DoubleSpeed,
        //         3 => TimeSpeederConfig.TripleSpeed,
        //         _ => 1
        //     };

        //     button.name = $"Speed{speed}";
        //     button.text = $"{speed}x";

        //     // Bind the button using the original factory
        //     __instance._bindableButtonFactory.CreateAndBind(button, string.Format(TimeSpeedButtonFactory.TimeSpeedKeyFormat, index), delegate
        //     {
        //         clickCallback(speed);
        //     });

        //     __result = new TimeSpeedButton(button, speed);
        //     return false; // Skip original method
        // }
    }

    // [HarmonyPatch(typeof(SpeedControlPanel), nameof(SpeedControlPanel.Load))]
    // public static class SpeedControlPanelLoadPatch {
    //     private static readonly AccessTools.FieldRef<SpeedControlPanel, VisualElement> RootRef =
    //         AccessTools.FieldRefAccess<SpeedControlPanel, VisualElement>("_root");

    //     static void Postfix(SpeedControlPanel __instance) {
    //         var root = RootRef(__instance);

    //         if(root == null) {
    //             UnityEngine.Debug.LogWarning("TimeSpeeder could not access SpeedControlPanel root.");
    //             return;
    //         }

    //         UnityEngine.Debug.Log("caching speed buttons");
    //         TimeSpeederSpeedButtons.CacheFrom(root);
    //     }
    // }
}

public static class TimeSpeederSpeedButtons
{
    public static Button NormalButton;
    public static Button DoubleButton;
    public static Button TripleButton;

    public static void CacheFrom(VisualElement root) {
        NormalButton = root.Q<Button>("Speed1");
        DoubleButton = root.Q<Button>("Speed3");
        TripleButton = root.Q<Button>("Speed7");

        Apply();
    }

    public static void Apply() {
        if (NormalButton != null) {
            NormalButton.name = $"Speed{TimeSpeederConfig.NormalSpeed}";
            Debug.Log($"setting factor for normal speed to {TimeSpeederConfig.NormalSpeed}");
        }

        if (DoubleButton != null) {
            DoubleButton.name = $"Speed{TimeSpeederConfig.DoubleSpeed}";
            Debug.Log($"setting factor for double speed to {TimeSpeederConfig.DoubleSpeed}");
        }

        if (TripleButton != null) {
            TripleButton.name = $"Speed{TimeSpeederConfig.TripleSpeed}";
            Debug.Log($"setting factor for triple speed to {TimeSpeederConfig.TripleSpeed}");
        }
    }

    public static int GetSpeedForButton(Button button) {
        if (button == NormalButton) return TimeSpeederConfig.NormalSpeed;
        if (button == DoubleButton) return TimeSpeederConfig.DoubleSpeed;
        if (button == TripleButton) return TimeSpeederConfig.TripleSpeed;

        return int.TryParse(button.name.Replace("Speed", ""), out var speed)
            ? speed
            : 1;
    }
}