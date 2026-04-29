using Bindito.Core;
using ModSettings.Core;
using Timberborn.Modding;
using Timberborn.SettingsSystem;

namespace Timberborn.Mods.Daxisaurus.ConfigurableGameSpeed {
    public static class ConfigurableGameSpeedConfig {
        public static int NormalSpeed = 1;
        public static int DoubleSpeed = 3;
        public static int TripleSpeed = 7;
    }

    internal class ConfigurableGameSpeedSettings : ModSettingsOwner {
        internal static ConfigurableGameSpeedSettings Instance { get; private set; }

        public ModSetting<int> NormalSpeed { get; } =
            new(1,
                ModSettingDescriptor.Create("Speed Factor 1 [>]")
                    .SetTooltip("The speed factor for the first speed button, the default value for this is 1.")
            );
        public ModSetting<int> DoubleSpeed { get; } =
            new(3,
                ModSettingDescriptor.Create("Speed Factor 2 [>>]")
                    .SetTooltip("The speed factor for the second speed button, the default value for this is 3.")
            );
        public ModSetting<int> TripleSpeed { get; } =
            new(7,
                ModSettingDescriptor.Create("Speed Factor 3 [>>>]")
                    .SetTooltip("The speed factor for the third speed button, the default value for this is 7.")
            );

        public override ModSettingsContext ChangeableOn => ModSettingsContext.All;

        public ConfigurableGameSpeedSettings(
            ISettings settings,
            ModSettingsOwnerRegistry registry,
            ModRepository modRepository
        ) : base(settings, registry, modRepository)
        {
            Instance = this;
            NormalSpeed.ValueChanged += (_, _) => UpdateRuntimeValues();
            DoubleSpeed.ValueChanged += (_, _) => UpdateRuntimeValues();
            TripleSpeed.ValueChanged += (_, _) => UpdateRuntimeValues();
        }

        protected override string ModId => "Daxisaurus.Timberborn.ConfigurableGameSpeed";

        private void UpdateRuntimeValues() {
            ApplyConfiguredSpeeds(NormalSpeed.Value, DoubleSpeed.Value, TripleSpeed.Value);
        }

        internal static void ApplyConfiguredSpeeds(int normal, int doubleSpeed, int tripleSpeed) {
            ConfigurableGameSpeedConfig.NormalSpeed = NormalizePositive(normal);
            ConfigurableGameSpeedConfig.DoubleSpeed = NormalizePositive(doubleSpeed);
            ConfigurableGameSpeedConfig.TripleSpeed = NormalizePositive(tripleSpeed);
            ConfigurableGameSpeedPanelButtons.Apply();
            GameSpeedPresetButtonRegistry.RefreshAllInstances();
        }

        internal static int NormalizePositive(int value) => value > 0 ? value : 1;
    }

    /// <summary>
    ///   Mod Settings UI often commits values without raising ValueChanged on each edit; polling catches Apply.
    /// </summary>
    internal static class ConfigurableGameSpeedRuntimeSync {
        internal static void PollModSettingsIfNeeded() {
            var owner = ConfigurableGameSpeedSettings.Instance;
            if (owner == null) {
                return;
            }

            var nextNormal = ConfigurableGameSpeedSettings.NormalizePositive(owner.NormalSpeed.Value);
            var nextDouble = ConfigurableGameSpeedSettings.NormalizePositive(owner.DoubleSpeed.Value);
            var nextTriple = ConfigurableGameSpeedSettings.NormalizePositive(owner.TripleSpeed.Value);

            if (nextNormal == ConfigurableGameSpeedConfig.NormalSpeed
                && nextDouble == ConfigurableGameSpeedConfig.DoubleSpeed
                && nextTriple == ConfigurableGameSpeedConfig.TripleSpeed) {
                return;
            }

            ConfigurableGameSpeedSettings.ApplyConfiguredSpeeds(nextNormal, nextDouble, nextTriple);
        }
    }

    [Context("MainMenu")]
    [Context("Game")]
    internal class ConfigurableGameSpeedSettingsConfigurator : IConfigurator {
        public void Configure(IContainerDefinition containerDefinition) {
            containerDefinition.Bind<ConfigurableGameSpeedInitializer>().AsSingleton();
            containerDefinition.Bind<ConfigurableGameSpeedSettings>().AsSingleton();
        }
    }

    internal class ConfigurableGameSpeedInitializer {
        public ConfigurableGameSpeedInitializer(ConfigurableGameSpeedSettings settings) {
            ConfigurableGameSpeedSettings.ApplyConfiguredSpeeds(settings.NormalSpeed.Value, settings.DoubleSpeed.Value, settings.TripleSpeed.Value);
        }
    }
}
