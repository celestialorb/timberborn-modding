using Bindito.Core;

using Timberborn.AlertPanelSystem;
using Timberborn.EntityPanelSystem;

namespace Timberborn.Mods.Daxisaurus.ColonyPlanner {

    /// <summary>
    ///   Registers gameplay UI for ColonyPlanner (entity panel unlock controls).
    /// </summary>
    [Context("Game")]
    public sealed class ColonyPlannerConfigurator : Configurator {

        protected override void Configure() {
            Bind<ColonyPlannerGameServices>().AsSingleton();
            Bind<ColonyPlannerConstructionUnlockFragment>().AsSingleton();
            Bind<ColonyPlannerScienceLockAlertFragment>().AsSingleton();
            MultiBind<EntityPanelModule>().ToProvider<EntityPanelModuleProvider>().AsSingleton();
            MultiBind<AlertPanelModule>().ToProvider<AlertPanelModuleProvider>().AsSingleton();
        }

        sealed class EntityPanelModuleProvider : IProvider<EntityPanelModule> {

            readonly ColonyPlannerConstructionUnlockFragment _unlockFragment;

            public EntityPanelModuleProvider(ColonyPlannerConstructionUnlockFragment unlockFragment) {
                _unlockFragment = unlockFragment;
            }

            public EntityPanelModule Get() {
                var builder = new EntityPanelModule.Builder();
                // Footer tier (3000+) sorts last in Fragments — sits above DiagnosticFragments status strip.
                builder.AddFooterFragment(_unlockFragment, 100_000);
                return builder.Build();
            }

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
