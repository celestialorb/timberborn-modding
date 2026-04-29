using Timberborn.Localization;
using Timberborn.SingletonSystem;

namespace Timberborn.Mods.Daxisaurus.GhostPlanner {

    /// <summary>
    ///   Loads early so Harmony patches can localize status UI without service location.
    /// </summary>
    public sealed class GhostPlannerGameServices : ILoadableSingleton {

        internal static ILoc Localization { get; private set; }

        internal GhostPlannerGameServices(ILoc loc) {
            Localization = loc;
        }

        public void Load() {
        }

    }

}
