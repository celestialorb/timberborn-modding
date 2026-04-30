using Timberborn.Localization;
using Timberborn.SingletonSystem;

namespace Timberborn.Mods.Daxisaurus.ColonyPlanner {

    /// <summary>
    ///   Loads early so Harmony patches can localize status UI without service location.
    /// </summary>
    public sealed class ColonyPlannerGameServices : ILoadableSingleton {

        internal static ILoc Localization { get; private set; }

        internal ColonyPlannerGameServices(ILoc loc) {
            Localization = loc;
        }

        public void Load() {
        }

    }

}
