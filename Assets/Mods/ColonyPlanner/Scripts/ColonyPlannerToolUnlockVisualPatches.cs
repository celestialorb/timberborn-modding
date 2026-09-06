using System.Reflection;

using HarmonyLib;

using Timberborn.BlockObjectTools;
using Timberborn.BlueprintSystem;
using Timberborn.Buildings;

namespace Timberborn.Mods.Daxisaurus.ColonyPlanner {

    /// <summary>
    ///   Lets ColonyPlanner buildings be placed while still science-locked (queued for later unlock) without
    ///   disturbing vanilla lock tracking, so the bottom-bar <c>button--locked</c> styling and the tool-panel
    ///   "Unlock: N" cost badge (<see cref="Timberborn.BuildingTools.BuildingPlacer" />) keep working normally.
    /// </summary>
    static class ColonyPlannerToolUnlockVisualPatches {

        static MethodInfo _blockObjectToolPlaceMethod;

        /// <summary>
        ///   Vanilla <see cref="BlockObjectTool" /> diverts placement into <see cref="Timberborn.ToolSystem.ToolUnlockingService.TryToUnlock" />
        ///   (pay-first dialog) whenever the tool is locked. ColonyPlanner buildings should place immediately regardless of
        ///   lock state; the deferred science payment is handled by unlocking the building's tool from the build menu.
        ///   Targeted by method name (rather than a typed parameter list) and invoked via reflection because
        ///   <c>ActionCallback</c>/<c>Place</c> accessibility has varied across game versions.
        /// </summary>
        [HarmonyPatch(typeof(BlockObjectTool), "ActionCallback")]
        static class BlockObjectToolActionCallbackColonyPlannerPatch {

            static bool Prefix(BlockObjectTool __instance, object[] __args) {
                var template = __instance.Template;
                if (!template.HasSpec<BuildingSpec>()) {
                    return true;
                }

                if (template.GetSpec<BuildingSpec>().ScienceCost <= 0) {
                    return true;
                }

                _blockObjectToolPlaceMethod ??= AccessTools.Method(typeof(BlockObjectTool), "Place");
                _blockObjectToolPlaceMethod.Invoke(__instance, __args);
                return false;
            }

        }

    }

}
