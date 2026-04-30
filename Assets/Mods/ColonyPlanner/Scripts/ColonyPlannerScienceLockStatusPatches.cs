using System.Runtime.CompilerServices;

using HarmonyLib;

using Timberborn.ConstructionSites;
using Timberborn.Localization;
using Timberborn.StatusSystem;

namespace Timberborn.Mods.Daxisaurus.ColonyPlanner {

    /// <summary>
    ///   Floating status icon for construction sites whose blueprint is not science-unlocked (ColonyPlanner).
    ///   Reuses vanilla sprite name <c>NotEnoughScience</c> (same asset as science shortage on producers),
    ///   with priority placement similar to pause.
    /// </summary>
    static class ColonyPlannerScienceLockStatusPatches {

        static readonly ConditionalWeakTable<ConstructionSite, StatusToggle> ScienceLockToggles = new();

        /// <summary>
        ///   Registers once per site after vanilla registers lack-of-resources status.
        /// </summary>
        [HarmonyPatch(typeof(ConstructionSite), nameof(ConstructionSite.StartTickable))]
        static class ConstructionSiteStartTickableScienceLockPatch {

            static void Postfix(ConstructionSite __instance) {
                TryRegisterScienceLockToggle(__instance);
            }

        }

        /// <summary>
        ///   Lazily registers when <see cref="ColonyPlannerGameServices.Localization" /> was not ready at
        ///   <see cref="ConstructionSite.StartTickable" />; keeps toggle active state in sync.
        /// </summary>
        [HarmonyPatch(typeof(ConstructionSite), nameof(ConstructionSite.Tick))]
        static class ConstructionSiteTickScienceLockPatch {

            static void Postfix(ConstructionSite __instance) {
                TryRegisterScienceLockToggle(__instance);
                SyncScienceLockToggle(__instance);
            }

        }

        static void TryRegisterScienceLockToggle(ConstructionSite site) {
            if (ScienceLockToggles.TryGetValue(site, out _)) {
                return;
            }

            var spec = ColonyPlannerBuildingSpecAccessor.FromConstructionSite(site);
            if (spec == null || spec.ScienceCost <= 0) {
                return;
            }

            var loc = ColonyPlannerGameServices.Localization;
            if (loc == null) {
                return;
            }

            var toggle = StatusToggle.CreatePriorityStatusWithFloatingIcon(
                "NotEnoughScience",
                DescribeScienceLock(loc, spec.ScienceCost),
                delayInHours: 0f);
            site.GetComponent<StatusSubject>().RegisterStatus(toggle);
            ScienceLockToggles.Add(site, toggle);

            if (ColonyPlannerUnlockGate.IsConstructionWorkBlocked(site)) {
                toggle.Activate();
            }
            else {
                toggle.Deactivate();
            }
        }

        static string DescribeScienceLock(ILoc loc, int scienceCost) {
            return loc.T("ColonyPlanner.Status.ScienceLocked", scienceCost);
        }

        static void SyncScienceLockToggle(ConstructionSite site) {
            if (!ScienceLockToggles.TryGetValue(site, out var toggle)) {
                return;
            }

            var blocked = ColonyPlannerUnlockGate.IsConstructionWorkBlocked(site);
            if (toggle.IsActive == blocked) {
                return;
            }

            if (blocked) {
                toggle.Activate();
            }
            else {
                toggle.Deactivate();
            }
        }

    }

}
