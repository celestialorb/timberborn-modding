using System;
using System.Collections.Generic;
using System.Reflection;

using HarmonyLib;
using Timberborn.ModManagerScene;
using Timberborn.TimeSpeedButtonSystem;
using Timberborn.TimeSystemUI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Timberborn.Mods.Daxisaurus.ConfigurableGameSpeeds {
    /// <summary>
    ///   Reads TimeSpeedKeyFormat via reflection. Direct field access from mod assemblies can throw
    ///   <see cref="FieldAccessException" /> at runtime.
    /// </summary>
    internal static class ConfigurableGameSpeedsKeyFormatAccessor {
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
                    UnityEngine.Debug.LogWarning($"ConfigurableGameSpeeds: could not read TimeSpeedKeyFormat via reflection: {ex.Message}");
                }
            }
            else {
                UnityEngine.Debug.LogWarning("ConfigurableGameSpeeds: TimeSpeedKeyFormat field not found.");
            }

            if (string.IsNullOrEmpty(_template)) {
                UnityEngine.Debug.LogWarning(
                    "ConfigurableGameSpeeds: TimeSpeedKeyFormat unavailable; hotkeys may not match preset slots.");
                _template = "{0}";
            }
        }
    }

    internal class ConfigurableGameSpeedsModStarter : IModStarter {
        public void StartMod(IModEnvironment modEnvironment) {
            UnityEngine.Debug.Log("ConfigurableGameSpeeds mod started!");

            ConfigurableGameSpeedsRuntimePoll.EnsureStarted();

            var harmony = new Harmony("com.daxisaurus.timberborn.configurablegamespeeds");
            harmony.PatchAll();
        }
    }

    /// <summary>
    ///   Refreshes cached multipliers inside <see cref="TimeSpeedButton" /> wrappers when settings change
    ///   mid-session (Vanilla UI compares current speed to these values for preset highlighting).
    /// </summary>
    internal static class ConfigurableGameSpeedsPresetButtonRegistry {
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
                    "ConfigurableGameSpeeds: could not locate int speed field on TimeSpeedButton; preset highlighting may desync until reload.");
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
                    entry.SpeedField.SetValue(tsb, ConfigurableGameSpeedsPresetMap.GetPresetSpeedForIndex(entry.Index));
                }
                catch (Exception ex) {
                    UnityEngine.Debug.LogWarning($"ConfigurableGameSpeeds: failed to refresh TimeSpeedButton multiplier: {ex.Message}");
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

    internal static class ConfigurableGameSpeedsRuntimePoll {
        static GameObject _host;

        internal static void EnsureStarted() {
            if (_host != null) {
                return;
            }

            _host = new GameObject("ConfigurableGameSpeeds.SettingsPoll");
            UnityEngine.Object.DontDestroyOnLoad(_host);
            _host.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInHierarchy;
            _host.AddComponent<ConfigurableGameSpeedsPollBehaviour>();
        }

        sealed class ConfigurableGameSpeedsPollBehaviour : MonoBehaviour {
            void Update() {
                ConfigurableGameSpeedsRuntimeSync.PollModSettingsIfNeeded();
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

            var speed = ConfigurableGameSpeedsPresetMap.GetPresetSpeedForIndex(index);
            button.name = $"Speed{index}";
            button.text = $"{speed}x";
            var capturedIndex = index;
            Traverse.Create(bindableButtonFactory)
                .Method(
                    "CreateAndBind",
                    button,
                    ConfigurableGameSpeedsKeyFormatAccessor.FormatForIndex(index),
                    new Action(() => clickCallback(ConfigurableGameSpeedsPresetMap.GetPresetSpeedForIndex(capturedIndex))))
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
            ConfigurableGameSpeedsPresetButtonRegistry.RegisterFromConstructor(__instance, button, timeSpeed);
        }
    }

    [HarmonyPatch(typeof(SpeedControlPanel), nameof(SpeedControlPanel.Load))]
    public static class SpeedControlPanelLoadPatch {
        private static readonly AccessTools.FieldRef<SpeedControlPanel, VisualElement> RootRef =
            AccessTools.FieldRefAccess<SpeedControlPanel, VisualElement>("_root");

        static void Postfix(SpeedControlPanel __instance) {
            var root = RootRef(__instance);

            if (root == null) {
                UnityEngine.Debug.LogWarning("ConfigurableGameSpeeds could not access SpeedControlPanel root.");
                return;
            }

            ConfigurableGameSpeedsPanelButtons.CacheFrom(root);
        }
    }

    public static class ConfigurableGameSpeedsPresetMap {
        public static int GetPresetSpeedForIndex(int index) {
            return index switch {
                0 => 0,
                1 => ConfigurableGameSpeedsConfig.NormalSpeed,
                2 => ConfigurableGameSpeedsConfig.DoubleSpeed,
                3 => ConfigurableGameSpeedsConfig.TripleSpeed,
                _ => Math.Max(index, 1)
            };
        }
    }

    public static class ConfigurableGameSpeedsPanelButtons {
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
                NormalButton.text = $"{ConfigurableGameSpeedsConfig.NormalSpeed}x";
            }

            if (DoubleButton != null) {
                DoubleButton.text = $"{ConfigurableGameSpeedsConfig.DoubleSpeed}x";
            }

            if (TripleButton != null) {
                TripleButton.text = $"{ConfigurableGameSpeedsConfig.TripleSpeed}x";
            }
        }
    }
}
