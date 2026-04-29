using HarmonyLib;

using Timberborn.ModManagerScene;
using UnityEngine;

namespace Timberborn.Mods.Daxisaurus.GhostPlanner {

    internal sealed class GhostPlannerModStarter : IModStarter {

        public void StartMod(IModEnvironment modEnvironment) {
            var harmony = new Harmony("com.daxisaurus.timberborn.ghostplanner");
            harmony.PatchAll(typeof(GhostPlannerModStarter).Assembly);
            Debug.Log("[GhostPlanner] Mod loaded; Harmony patches applied.");
        }

    }

}

