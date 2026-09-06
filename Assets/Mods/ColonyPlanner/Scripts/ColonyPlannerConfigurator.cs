using Bindito.Core;

using Timberborn.AlertPanelSystem;

namespace Timberborn.Mods.Daxisaurus.ColonyPlanner {

    /// <summary>
    ///   Registers gameplay UI for ColonyPlanner (science-locked alert row).
    /// </summary>
    [Context("Game")]
    public sealed class ColonyPlannerConfigurator : Configurator {

        protected override void Configure() {
            Bind<ColonyPlannerGameServices>().AsSingleton();
            Bind<ColonyPlannerScienceLockAlertFragment>().AsSingleton();
            MultiBind<AlertPanelModule>().ToProvider<AlertPanelModuleProvider>().AsSingleton();
        }

        sealed class AlertPanelModuleProvider : IProvider<AlertPanelModule> {

            readonly ColonyPlannerScienceLockAlertFragment _scienceLockAlert;

            public AlertPanelModuleProvider(ColonyPlannerScienceLockAlertFragment scienceLockAlert) {
                _scienceLockAlert = scienceLockAlert;
            }

            public AlertPanelModule Get() {
                var builder = new AlertPanelModule.Builder();
                builder.AddAlertFragment(_scienceLockAlert, 80);
                return builder.Build();
            }

        }

    }

}
