using System.Collections.Generic;
using System.Reflection;

using HarmonyLib;

using Timberborn.Buildings;
using Timberborn.ConstructionSites;
using Timberborn.Goods;
using Timberborn.ScienceSystem;

namespace Timberborn.Mods.Daxisaurus.GhostPlanner {

    /// <summary>
    ///   Resolves <see cref="BuildingSpec" /> from construction-site-related components via vanilla fields.
    /// </summary>
    internal static class GhostPlannerBuildingSpecAccessor {

        static readonly FieldInfo ConstructionSiteBuildingSpecField =
            AccessTools.Field(typeof(ConstructionSite), "_buildingSpec");

        internal static BuildingSpec FromConstructionSite(ConstructionSite constructionSite) {
            return ConstructionSiteBuildingSpecField?.GetValue(constructionSite) as BuildingSpec;
        }

    }

    /// <summary>
    ///   Shared rule: block construction work when vanilla treats this blueprint as not science-unlocked.
    /// </summary>
    internal static class GhostPlannerUnlockGate {

        internal static bool IsConstructionWorkBlocked(ConstructionSite constructionSite) {
            var unlockService = GhostPlannerUnlockServiceHolder.Instance;
            if (unlockService == null) {
                return false;
            }

            var buildingSpec = GhostPlannerBuildingSpecAccessor.FromConstructionSite(constructionSite);
            if (buildingSpec == null) {
                return false;
            }

            return !unlockService.Unlocked(buildingSpec);
        }

    }

    /// <summary>
    ///   Cached <see cref="BuildingUnlockingService" /> instance for construction patches.
    /// </summary>
    internal static class GhostPlannerUnlockServiceHolder {

        internal static BuildingUnlockingService Instance { get; set; }

    }

    /// <summary>
    ///   Caches <see cref="BuildingUnlockingService" /> after singleton load (Harmony constructor
    ///   patches failed with an undefined target method at runtime).
    /// </summary>
    [HarmonyPatch(typeof(BuildingUnlockingService), nameof(BuildingUnlockingService.Load))]
    internal static class BuildingUnlockingServiceLoadPatch {

        static void Postfix(BuildingUnlockingService __instance) {
            GhostPlannerUnlockServiceHolder.Instance = __instance;
        }

    }

    /// <summary>
    ///   Blocks construction time progress until the building template is unlocked via science.
    /// </summary>
    [HarmonyPatch(typeof(ConstructionSite), nameof(ConstructionSite.IncreaseBuildTime))]
    internal static class ConstructionSiteIncreaseBuildTimePatch {

        static bool Prefix(ConstructionSite __instance, float hours) {
            _ = hours;
            return !GhostPlannerUnlockGate.IsConstructionWorkBlocked(__instance);
        }

    }

    /// <summary>
    ///   Clears the material-needed query used by <see cref="ConstructionJob" /> so haul jobs are not
    ///   scheduled for ghost sites. This avoids relying on Harmony tuple prefixes on
    ///   <c>StartConstructionJob</c> (they often fail to bind) and avoids the
    ///   <see cref="Timberborn.BehaviorSystem.Decision.ReleaseNextTick" /> retry loop that left builders stuck.
    /// </summary>
    [HarmonyPatch(typeof(ConstructionSite), nameof(ConstructionSite.RemainingRequiredGoods))]
    internal static class ConstructionSiteRemainingRequiredGoodsPatch {

        static bool Prefix(ConstructionSite __instance, SortedSet<GoodAmount> remainingGoods) {
            if (!GhostPlannerUnlockGate.IsConstructionWorkBlocked(__instance)) {
                return true;
            }

            remainingGoods.Clear();
            return false;
        }

    }

}
