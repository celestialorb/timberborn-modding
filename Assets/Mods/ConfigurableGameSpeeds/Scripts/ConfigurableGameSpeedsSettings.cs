using Bindito.Core;
using ModSettings.Core;
using Timberborn.Modding;
using Timberborn.SettingsSystem;

namespace Timberborn.Mods.Daxisaurus.ConfigurableGameSpeeds {
    public static class ConfigurableGameSpeedsConfig {
        public static int NormalSpeed = 1;
        public static int DoubleSpeed = 3;
        public static int TripleSpeed = 7;
    }

    internal class ConfigurableGameSpeedsSettings : ModSettingsOwner {
        internal static ConfigurableGameSpeedsSettings Instance { get; private set; }

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

        public ConfigurableGameSpeedsSettings(
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

        protected override string ModId => "Daxisaurus.Timberborn.ConfigurableGameSpeeds";

        private void UpdateRuntimeValues() {
            ApplyConfiguredSpeeds(NormalSpeed.Value, DoubleSpeed.Value, TripleSpeed.Value);
        }

        internal static void ApplyConfiguredSpeeds(int normal, int doubleSpeed, int tripleSpeed) {
            ConfigurableGameSpeedsConfig.NormalSpeed = NormalizePositive(normal);
            ConfigurableGameSpeedsConfig.DoubleSpeed = NormalizePositive(doubleSpeed);
            ConfigurableGameSpeedsConfig.TripleSpeed = NormalizePositive(tripleSpeed);
            ConfigurableGameSpeedsPanelButtons.Apply();
            ConfigurableGameSpeedsPresetButtonRegistry.RefreshAllInstances();
        }

        internal static int NormalizePositive(int value) => value > 0 ? value : 1;
    }

    /// <summary>
    ///   Mod Settings UI often commits values without raising ValueChanged on each edit; polling catches Apply.
    /// </summary>
    internal static class ConfigurableGameSpeedsRuntimeSync {
        internal static void PollModSettingsIfNeeded() {
            var owner = ConfigurableGameSpeedsSettings.Instance;
            if (owner == null) {
                return;
            }

            var nextNormal = ConfigurableGameSpeedsSettings.NormalizePositive(owner.NormalSpeed.Value);
            var nextDouble = ConfigurableGameSpeedsSettings.NormalizePositive(owner.DoubleSpeed.Value);
            var nextTriple = ConfigurableGameSpeedsSettings.NormalizePositive(owner.TripleSpeed.Value);

            if (nextNormal == ConfigurableGameSpeedsConfig.NormalSpeed
                && nextDouble == ConfigurableGameSpeedsConfig.DoubleSpeed
                && nextTriple == ConfigurableGameSpeedsConfig.TripleSpeed) {
                return;
            }

            ConfigurableGameSpeedsSettings.ApplyConfiguredSpeeds(nextNormal, nextDouble, nextTriple);
        }
    }

    [Context("MainMenu")]
    [Context("Game")]
    internal class ConfigurableGameSpeedsSettingsConfigurator : IConfigurator {
        public void Configure(IContainerDefinition containerDefinition) {
            containerDefinition.Bind<ConfigurableGameSpeedsInitializer>().AsSingleton();
            containerDefinition.Bind<ConfigurableGameSpeedsSettings>().AsSingleton();
        }
    }

    internal class ConfigurableGameSpeedsInitializer {
        public ConfigurableGameSpeedsInitializer(ConfigurableGameSpeedsSettings settings) {
            ConfigurableGameSpeedsSettings.ApplyConfiguredSpeeds(settings.NormalSpeed.Value, settings.DoubleSpeed.Value, settings.TripleSpeed.Value);
        }
    }
}
