using System;
using System.Collections.Generic;
using System.Reflection;

using HarmonyLib;
using Timberborn.ModManagerScene;
using Timberborn.TimeSpeedButtonSystem;
using Timberborn.TimeSystemUI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Timberborn.Mods.Daxisaurus.ConfigurableGameSpeed {
    /// <summary>
    ///   Reads TimeSpeedKeyFormat via reflection. Direct field access from mod assemblies can throw
    ///   <see cref="FieldAccessException" /> at runtime.
    /// </summary>
    internal static class ConfigurableGameSpeedKeyFormatAccessor {
        static string _template;
        static bool _resolved;

        internal static string FormatForIndex(int index) {
            EnsureTemplate();
            return string.Format(_template, index);
        }

        static void EnsureTemplate() {
            if (_resolved) {
                return;
            }

            _resolved = true;
            var fieldInfo = AccessTools.Field(typeof(TimeSpeedButtonFactory), "TimeSpeedKeyFormat")
                            ?? AccessTools.DeclaredField(typeof(TimeSpeedButtonFactory), "TimeSpeedKeyFormat");
            if (fieldInfo != null) {
                try {
                    _template = fieldInfo.GetValue(null) as string;
                }
                catch (Exception ex) {
                    UnityEngine.Debug.LogWarning($"ConfigurableGameSpeed: could not read TimeSpeedKeyFormat via reflection: {ex.Message}");
                }
            }
            else {
                UnityEngine.Debug.LogWarning("ConfigurableGameSpeed: TimeSpeedKeyFormat field not found.");
            }

            if (string.IsNullOrEmpty(_template)) {
                UnityEngine.Debug.LogWarning(
                    "ConfigurableGameSpeed: TimeSpeedKeyFormat unavailable; hotkeys may not match preset slots.");
                _template = "{0}";
            }
        }
    }

    internal class ConfigurableGameSpeedModStarter : IModStarter {
        public void StartMod(IModEnvironment modEnvironment) {
            UnityEngine.Debug.Log("ConfigurableGameSpeed mod started!");

            ConfigurableGameSpeedRuntimePoll.EnsureStarted();

            var harmony = new Harmony("com.daxisaurus.timberborn.configurablegamespeed");
            harmony.PatchAll();
        }
    }

    /// <summary>
    ///   Refreshes cached multipliers inside <see cref="TimeSpeedButton" /> wrappers when settings change
    ///   mid-session (Vanilla UI compares current speed to these values for preset highlighting).
    /// </summary>
    internal static class GameSpeedPresetButtonRegistry {
        sealed class Entry {
            internal WeakReference<TimeSpeedButton> ButtonRef;
            internal FieldInfo SpeedField;
            internal int Index;
        }

        static readonly List<Entry> Entries = new();

        internal static void RegisterFromConstructor(TimeSpeedButton instance, Button button, int speedParameter) {
            Entries.RemoveAll(static e => !e.ButtonRef.TryGetTarget(out _));

            if (!TryParseSlotIndex(button.name, out var index) || index < 0) {
                return;
            }

            FieldInfo speedField = null;
            foreach (var field in instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                if (field.FieldType != typeof(int)) {
                    continue;
                }

                try {
                    if ((int)field.GetValue(instance) == speedParameter) {
                        speedField = field;
                        break;
                    }
                }
                catch {
                    // Field unreadable; skip.
                }
            }

            if (speedField == null) {
                UnityEngine.Debug.LogWarning(
                    "ConfigurableGameSpeed: could not locate int speed field on TimeSpeedButton; preset highlighting may desync until reload.");
                return;
            }

            Entries.Add(new Entry {
                ButtonRef = new WeakReference<TimeSpeedButton>(instance),
                SpeedField = speedField,
                Index = index,
            });
        }

        internal static void RefreshAllInstances() {
            Entries.RemoveAll(static e => !e.ButtonRef.TryGetTarget(out _));

            foreach (var entry in Entries) {
                if (!entry.ButtonRef.TryGetTarget(out var tsb)) {
                    continue;
                }

                try {
                    entry.SpeedField.SetValue(tsb, ConfigurableGameSpeedPresetMap.GetPresetSpeedForIndex(entry.Index));
                }
                catch (Exception ex) {
                    UnityEngine.Debug.LogWarning($"ConfigurableGameSpeed: failed to refresh TimeSpeedButton multiplier: {ex.Message}");
                }
            }
        }

        static bool TryParseSlotIndex(string buttonName, out int index) {
            index = -1;
            const string prefix = "Speed";
            if (string.IsNullOrEmpty(buttonName) || !buttonName.StartsWith(prefix, StringComparison.Ordinal)) {
                return false;
            }

            return int.TryParse(buttonName.Substring(prefix.Length), out index);
        }
    }

    internal static class ConfigurableGameSpeedRuntimePoll {
        static GameObject _host;

        internal static void EnsureStarted() {
            if (_host != null) {
                return;
            }

            _host = new GameObject("ConfigurableGameSpeed.SettingsPoll");
            UnityEngine.Object.DontDestroyOnLoad(_host);
            _host.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInHierarchy;
            _host.AddComponent<ConfigurableGameSpeedPollBehaviour>();
        }

        sealed class ConfigurableGameSpeedPollBehaviour : MonoBehaviour {
            void Update() {
                ConfigurableGameSpeedRuntimeSync.PollModSettingsIfNeeded();
            }
        }
    }

    [HarmonyPatch(typeof(TimeSpeedButtonFactory), nameof(TimeSpeedButtonFactory.Create))]
    public static class TimeSpeedButtonFactoryCreatePatch {
        static bool Prefix(
            TimeSpeedButtonFactory __instance,
            Button button,
            int index,
            Action<int> clickCallback,
            ref TimeSpeedButton __result) {
            var bindableButtonFactory =
                Traverse.Create(__instance).Field<object>("_bindableButtonFactory").Value;
            if (bindableButtonFactory == null) {
                return true;
            }

            var speed = ConfigurableGameSpeedPresetMap.GetPresetSpeedForIndex(index);
            button.name = $"Speed{index}";
            button.text = $"{speed}x";
            var capturedIndex = index;
            Traverse.Create(bindableButtonFactory)
                .Method(
                    "CreateAndBind",
                    button,
                    ConfigurableGameSpeedKeyFormatAccessor.FormatForIndex(index),
                    new Action(() => clickCallback(ConfigurableGameSpeedPresetMap.GetPresetSpeedForIndex(capturedIndex))))
                .GetValue();
            __result = new TimeSpeedButton(button, speed);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class TimeSpeedButtonCtorPatch {
        static MethodBase TargetMethod() =>
            AccessTools.Constructor(typeof(TimeSpeedButton), new[] { typeof(Button), typeof(int) });

        static void Postfix(TimeSpeedButton __instance, Button button, int timeSpeed) {
            GameSpeedPresetButtonRegistry.RegisterFromConstructor(__instance, button, timeSpeed);
        }
    }

    [HarmonyPatch(typeof(SpeedControlPanel), nameof(SpeedControlPanel.Load))]
    public static class SpeedControlPanelLoadPatch {
        private static readonly AccessTools.FieldRef<SpeedControlPanel, VisualElement> RootRef =
            AccessTools.FieldRefAccess<SpeedControlPanel, VisualElement>("_root");

        static void Postfix(SpeedControlPanel __instance) {
            var root = RootRef(__instance);

            if (root == null) {
                UnityEngine.Debug.LogWarning("ConfigurableGameSpeed could not access SpeedControlPanel root.");
                return;
            }

            ConfigurableGameSpeedPanelButtons.CacheFrom(root);
        }
    }

    public static class ConfigurableGameSpeedPresetMap {
        public static int GetPresetSpeedForIndex(int index) {
            return index switch {
                0 => 0,
                1 => ConfigurableGameSpeedConfig.NormalSpeed,
                2 => ConfigurableGameSpeedConfig.DoubleSpeed,
                3 => ConfigurableGameSpeedConfig.TripleSpeed,
                _ => Math.Max(index, 1)
            };
        }
    }

    public static class ConfigurableGameSpeedPanelButtons {
        public static Button NormalButton;
        public static Button DoubleButton;
        public static Button TripleButton;

        public static void CacheFrom(VisualElement root) {
            NormalButton = root.Q<Button>("Speed1");
            DoubleButton = root.Q<Button>("Speed2");
            TripleButton = root.Q<Button>("Speed3");

            Apply();
        }

        public static void Apply() {
            if (NormalButton != null) {
                NormalButton.text = $"{ConfigurableGameSpeedConfig.NormalSpeed}x";
            }

            if (DoubleButton != null) {
                DoubleButton.text = $"{ConfigurableGameSpeedConfig.DoubleSpeed}x";
            }

            if (TripleButton != null) {
                TripleButton.text = $"{ConfigurableGameSpeedConfig.TripleSpeed}x";
            }
        }
    }
}
