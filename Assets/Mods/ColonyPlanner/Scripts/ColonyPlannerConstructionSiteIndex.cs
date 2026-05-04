using System.Collections.Generic;

using Timberborn.BaseComponentSystem;
using Timberborn.ConstructionSites;

using UnityEngine;

namespace Timberborn.Mods.Daxisaurus.ColonyPlanner {

    /// <summary>
    ///   Tracks construction sites that have entered tickable simulation (<see cref="ConstructionSite.StartTickable" />)
    ///   so alert UI can refresh without scanning <see cref="Timberborn.EntitySystem.EntityRegistry.Entities" />.
    /// </summary>
    internal static class ColonyPlannerConstructionSiteIndex {

        static readonly HashSet<ConstructionSite> Sites = new();

        static readonly List<ConstructionSite> PruneScratch = new();

        internal static void Register(ConstructionSite site) {
            Sites.Add(site);
        }

        /// <summary>
        ///   Drops sites whose backing <see cref="GameObject" /> is gone (finished / destroyed construction).
        /// </summary>
        internal static void PruneDestroyed() {
            if (Sites.Count == 0) {
                return;
            }

            PruneScratch.Clear();
            foreach (var site in Sites) {
                if (site is BaseComponent component && component.GameObject != null) {
                    continue;
                }

                PruneScratch.Add(site);
            }

            foreach (var site in PruneScratch) {
                Sites.Remove(site);
            }

            PruneScratch.Clear();
        }

        internal static IEnumerable<ConstructionSite> EnumerateAlive() {
            PruneDestroyed();
            foreach (var site in Sites) {
                yield return site;
            }
        }

    }

}
