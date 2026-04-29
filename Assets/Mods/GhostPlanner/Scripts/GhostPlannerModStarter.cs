using Timberborn.ModManagerScene;
using UnityEngine;

namespace Timberborn.Mods.Daxisaurus.GhostPlanner {

    internal sealed class GhostPlannerModStarter : IModStarter {

        public void StartMod(IModEnvironment modEnvironment) {
            Debug.Log("[GhostPlanner] Mod loaded.");
        }

    }

}
