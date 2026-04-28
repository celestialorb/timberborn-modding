using Bindito.Core;
using ModSettings.Core;
using Timberborn.Modding;
using Timberborn.Mods.Daxisaurus.TimeSpeeder;
using Timberborn.SettingsSystem;

public static class TimeSpeederConfig {
    public static int NormalSpeed = 1;
    public static int DoubleSpeed = 3;
    public static int TripleSpeed = 7;
}

internal class TimeSpeederSettings : ModSettingsOwner {
    internal static TimeSpeederSettings Instance { get; private set; }

    public ModSetting<int> NormalSpeed { get; } =
        new(1,
            ModSettingDescriptor.Create("Speed Factor 1 [>]")
            .SetTooltip("The speed factor for the first speed button.")
        );
    public ModSetting<int> DoubleSpeed { get; } =
        new(3,
            ModSettingDescriptor.Create("Speed Factor 2 [>>]")
            .SetTooltip("The speed factor for the second speed button.")
        );
    public ModSetting<int> TripleSpeed { get; } =
        new(7,
            ModSettingDescriptor.Create("Speed Factor 3 [>>>]")
            .SetTooltip("The speed factor for the third speed button.")
        );

    public override ModSettingsContext ChangeableOn => ModSettingsContext.All;

    public TimeSpeederSettings(
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

    protected override string ModId => "Daxisaurus.TimeSpeeder";

    private void UpdateRuntimeValues() {
        ApplyConfiguredSpeeds(NormalSpeed.Value, DoubleSpeed.Value, TripleSpeed.Value);
    }

    internal static void ApplyConfiguredSpeeds(int normal, int doubleSpeed, int tripleSpeed) {
        TimeSpeederConfig.NormalSpeed = NormalizePositive(normal);
        TimeSpeederConfig.DoubleSpeed = NormalizePositive(doubleSpeed);
        TimeSpeederConfig.TripleSpeed = NormalizePositive(tripleSpeed);
        TimeSpeederSpeedButtons.Apply();
        TimeSpeedButtonMultiplierRegistry.RefreshAllInstances();
    }

    internal static int NormalizePositive(int value) => value > 0 ? value : 1;
}

/// <summary>
///   Mod Settings UI often commits values without raising ValueChanged on each edit; polling catches Apply.
/// </summary>
internal static class TimeSpeederRuntimeSync {
    internal static void PollModSettingsIfNeeded() {
        var owner = TimeSpeederSettings.Instance;
        if (owner == null) {
            return;
        }

        var nextNormal = TimeSpeederSettings.NormalizePositive(owner.NormalSpeed.Value);
        var nextDouble = TimeSpeederSettings.NormalizePositive(owner.DoubleSpeed.Value);
        var nextTriple = TimeSpeederSettings.NormalizePositive(owner.TripleSpeed.Value);

        if (nextNormal == TimeSpeederConfig.NormalSpeed
            && nextDouble == TimeSpeederConfig.DoubleSpeed
            && nextTriple == TimeSpeederConfig.TripleSpeed) {
            return;
        }

        TimeSpeederSettings.ApplyConfiguredSpeeds(nextNormal, nextDouble, nextTriple);
    }
}

[Context("MainMenu")]
[Context("Game")]
internal class TimeSpeederSettingsConfigurator : IConfigurator {
    public void Configure(IContainerDefinition containerDefinition) {
        containerDefinition.Bind<TimeSpeederInitializer>().AsSingleton();
        containerDefinition.Bind<TimeSpeederSettings>().AsSingleton();
    }
}

internal class TimeSpeederInitializer {
    public TimeSpeederInitializer(TimeSpeederSettings settings) {
        TimeSpeederSettings.ApplyConfiguredSpeeds(settings.NormalSpeed.Value, settings.DoubleSpeed.Value, settings.TripleSpeed.Value);
    }
}