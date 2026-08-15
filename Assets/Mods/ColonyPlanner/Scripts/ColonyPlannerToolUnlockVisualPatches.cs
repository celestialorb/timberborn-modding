using System;
using System.Reflection;

using HarmonyLib;

using Timberborn.BlockObjectTools;
using Timberborn.BlueprintSystem;
using Timberborn.Buildings;
using Timberborn.ScienceSystem;
using Timberborn.TemplateSystem;
using Timberborn.ToolButtonSystem;
using Timberborn.ToolSystem;

namespace Timberborn.Mods.Daxisaurus.ColonyPlanner {

    /// <summary>
    ///   Lets ColonyPlanner buildings be placed while still science-locked (queued for later unlock) without
    ///   disturbing vanilla lock tracking, so the bottom-bar <c>button--locked</c> styling and the tool-panel
    ///   "Unlock: N" cost badge (<see cref="Timberborn.BuildingTools.BuildingPlacer" />) keep working normally.
    /// </summary>
    static class ColonyPlannerToolUnlockVisualPatches {

        internal static ToolUnlockingService ToolUnlockingServiceInstance { get; private set; }

        internal static ToolButtonService ToolButtonServiceInstance { get; private set; }

        static MethodInfo _blockObjectToolPlaceMethod;

        [HarmonyPatch]
        static class ToolUnlockingServiceCtorCapturePatch {

            static MethodBase TargetMethod() {
                foreach (var ctor in AccessTools.GetDeclaredConstructors(typeof(ToolUnlockingService))) {
                    if (ctor.GetParameters().Length == 2) {
                        return ctor;
                    }
                }

                throw new InvalidOperationException(
                    "ColonyPlanner: could not find ToolUnlockingService(EventBus, IEnumerable<IToolLocker>)");
            }

            static void Postfix(ToolUnlockingService __instance) {
                ToolUnlockingServiceInstance = __instance;
            }

        }

        [HarmonyPatch]
        static class ToolButtonServiceCtorCapturePatch {

            static MethodBase TargetMethod() =>
                AccessTools.DeclaredConstructor(typeof(ToolButtonService), new[]
                    { typeof(ToolbarButtonRetriever), typeof(ToolGroupService), typeof(ToolUnlockingService), });

            static void Postfix(ToolButtonService __instance) {
                ToolButtonServiceInstance = __instance;
            }

        }

        /// <summary>
        ///   Vanilla <see cref="BlockObjectTool" /> diverts placement into <see cref="ToolUnlockingService.TryToUnlock" />
        ///   (pay-first dialog) whenever the tool is locked. ColonyPlanner buildings should place immediately regardless of
        ///   lock state; the deferred science payment is handled after placement via the construction site's unlock fragment.
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

        /// <summary>
        ///   Unlocking from the entity panel calls <see cref="BuildingUnlockingService.Unlock" />/
        ///   <see cref="BuildingUnlockingService.UnlockIgnoringCost" /> without going through
        ///   <see cref="ToolUnlockingService.Unlock" />, so tool buttons would keep <c>button--locked</c> until we sync visuals.
        /// </summary>
        [HarmonyPatch(typeof(BuildingUnlockingService), nameof(BuildingUnlockingService.UnlockIgnoringCost))]
        static class BuildingUnlockingServiceUnlockIgnoringCostVisualPatch {

            static void Postfix(BuildingSpec buildingSpec) {
                if (buildingSpec.ScienceCost <= 0) {
                    return;
                }

                var unlocking = ToolUnlockingServiceInstance;
                var buttons = ToolButtonServiceInstance;
                if (unlocking == null || buttons == null) {
                    return;
                }

                var templateName = buildingSpec.GetSpec<TemplateSpec>().TemplateName;
                foreach (var toolButton in buttons.ToolButtons) {
                    if (toolButton.Tool is not BlockObjectTool blockTool) {
                        continue;
                    }

                    if (!blockTool.Template.HasSpec<TemplateSpec>()) {
                        continue;
                    }

                    if (blockTool.Template.GetSpec<TemplateSpec>().TemplateName != templateName) {
                        continue;
                    }

                    if (unlocking.IsLocked(blockTool)) {
                        unlocking.Unlock(blockTool);
                    }
                }
            }

        }

    }

}
