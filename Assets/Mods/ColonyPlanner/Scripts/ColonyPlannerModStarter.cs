using HarmonyLib;

using Timberborn.ModManagerScene;
using UnityEngine;

namespace Timberborn.Mods.Daxisaurus.ColonyPlanner {

    internal sealed class ColonyPlannerModStarter : IModStarter {

        public void StartMod(IModEnvironment modEnvironment) {
            var harmony = new Harmony("com.daxisaurus.timberborn.colonyplanner");
            harmony.PatchAll(typeof(ColonyPlannerModStarter).Assembly);
            Debug.Log("[Colony Planner] Mod loaded; Harmony patches applied.");
        }

    }

}

