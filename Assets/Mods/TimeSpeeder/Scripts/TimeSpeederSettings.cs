using Bindito.Core;
using ModSettings.Core;
using Timberborn.Modding;
using Timberborn.SettingsSystem;
using UnityEngine;

public static class TimeSpeederConfig {
    public static int NormalSpeed = 1;
    public static int DoubleSpeed = 3;
    public static int TripleSpeed = 7;
}

internal class TimeSpeederSettings : ModSettingsOwner {
    public ModSetting<int> NormalSpeed { get; } =
        new(01,
            ModSettingDescriptor.Create("  > Speed Factor").SetTooltip("TODO1")
        );
    public ModSetting<int> DoubleSpeed { get; } =
        new(10, ModSettingDescriptor.Create(" >> Speed Factor").SetTooltip("TODO2"));
    public ModSetting<int> TripleSpeed { get; } =
        new(20, ModSettingDescriptor.Create(">>> Speed Factor").SetTooltip("TODO3"));

    public override ModSettingsContext ChangeableOn => ModSettingsContext.All;

    public TimeSpeederSettings(
        ISettings settings,
        ModSettingsOwnerRegistry registry,
        ModRepository modRepository
    ) : base(settings, registry, modRepository)
    {
        NormalSpeed.ValueChanged += (_, _) => UpdateRuntimeValues();
        DoubleSpeed.ValueChanged += (_, _) => UpdateRuntimeValues();
        TripleSpeed.ValueChanged += (_, _) => UpdateRuntimeValues();
    }

    protected override string ModId => "Daxisaurus.TimeSpeeder";

    private void UpdateRuntimeValues() {
        TimeSpeederConfig.NormalSpeed = NormalSpeed.Value;
        TimeSpeederConfig.DoubleSpeed = DoubleSpeed.Value;
        TimeSpeederConfig.TripleSpeed = TripleSpeed.Value;
        TimeSpeederSpeedButtons.Apply();
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
        TimeSpeederConfig.NormalSpeed = settings.NormalSpeed.Value;
        TimeSpeederConfig.DoubleSpeed = settings.DoubleSpeed.Value;
        TimeSpeederConfig.TripleSpeed = settings.TripleSpeed.Value;
        UnityEngine.Debug.Log($"TimeSpeeder normal speed: {TimeSpeederConfig.NormalSpeed}");
        UnityEngine.Debug.Log($"TimeSpeeder double speed: {TimeSpeederConfig.DoubleSpeed}");
        UnityEngine.Debug.Log($"TimeSpeeder triple speed: {TimeSpeederConfig.TripleSpeed}");
    }
}