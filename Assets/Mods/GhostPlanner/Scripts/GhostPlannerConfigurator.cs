using Bindito.Core;

using Timberborn.EntityPanelSystem;

namespace Timberborn.Mods.Daxisaurus.GhostPlanner {

    /// <summary>
    ///   Registers gameplay UI for GhostPlanner (entity panel unlock controls).
    /// </summary>
    [Context("Game")]
    public sealed class GhostPlannerConfigurator : Configurator {

        protected override void Configure() {
            Bind<GhostPlannerGameServices>().AsSingleton();
            Bind<GhostPlannerConstructionUnlockFragment>().AsSingleton();
            MultiBind<EntityPanelModule>().ToProvider<EntityPanelModuleProvider>().AsSingleton();
        }

        sealed class EntityPanelModuleProvider : IProvider<EntityPanelModule> {

            readonly GhostPlannerConstructionUnlockFragment _unlockFragment;

            public EntityPanelModuleProvider(GhostPlannerConstructionUnlockFragment unlockFragment) {
                _unlockFragment = unlockFragment;
            }

            public EntityPanelModule Get() {
                var builder = new EntityPanelModule.Builder();
                // Footer tier (3000+) sorts last in Fragments — sits above DiagnosticFragments status strip.
                builder.AddFooterFragment(_unlockFragment, 100_000);
                return builder.Build();
            }

        }

    }

}
