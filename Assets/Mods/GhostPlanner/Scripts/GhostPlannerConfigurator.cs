using Bindito.Core;

using Timberborn.EntityPanelSystem;

namespace Timberborn.Mods.Daxisaurus.GhostPlanner {

    /// <summary>
    ///   Registers gameplay UI for GhostPlanner (entity panel unlock controls).
    /// </summary>
    [Context("Game")]
    public sealed class GhostPlannerConfigurator : Configurator {

        protected override void Configure() {
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
                builder.AddMiddleFragment(_unlockFragment, 0);
                return builder.Build();
            }

        }

    }

}
