using System;
using System.Collections.Generic;
using System.Reflection;

using HarmonyLib;

using Timberborn.BlockObjectTools;
using Timberborn.BlueprintSystem;
using Timberborn.Buildings;
using Timberborn.ScienceSystem;
using Timberborn.TemplateSystem;
using Timberborn.ToolButtonSystem;
using Timberborn.ToolSystem;

namespace Timberborn.Mods.Daxisaurus.GhostPlanner {

    /// <summary>
    ///   Keeps building tools <strong>functionally</strong> unlocked (GhostPlanner placement) while restoring the
    ///   bottom-bar <c>button--locked</c> styling for blueprints that still need science payment.
    /// </summary>
    static class GhostPlannerToolUnlockVisualPatches {

        internal static ToolUnlockingService ToolUnlockingServiceInstance { get; private set; }

        internal static ToolButtonService ToolButtonServiceInstance { get; private set; }

        static readonly FieldInfo ToolUnlockingEventBusField =
            AccessTools.Field(typeof(ToolUnlockingService), "_eventBus");

        static Type _cachedPostObjectEventBusType;

        static MethodInfo _cachedPostObjectMethod;

        [HarmonyPatch]
        static class ToolUnlockingServiceCtorCapturePatch {

            static MethodBase TargetMethod() {
                foreach (var ctor in AccessTools.GetDeclaredConstructors(typeof(ToolUnlockingService))) {
                    if (ctor.GetParameters().Length == 2) {
                        return ctor;
                    }
                }

                throw new InvalidOperationException(
                    "GhostPlanner: could not find ToolUnlockingService(EventBus, IEnumerable<IToolLocker>)");
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
        ///   Vanilla adds tools to <see cref="ToolUnlockingService" /> active lockers and posts <see cref="ToolLockedEvent" />.
        ///   GhostPlanner skips the dictionary entry so <see cref="ToolUnlockingService.IsLocked" /> stays false (placement works),
        ///   but still raises <see cref="ToolLockedEvent" /> so tool buttons get <c>button--locked</c>.
        /// </summary>
        [HarmonyPatch(typeof(ToolUnlockingService), nameof(ToolUnlockingService.LockIfNeeded))]
        static class ToolUnlockingServiceLockIfNeededGhostPlannerPatch {

            static bool Prefix(ITool tool, ToolUnlockingService __instance) {
                if (tool is not BlockObjectTool blockTool) {
                    return true;
                }

                if (!blockTool.Template.HasSpec<BuildingSpec>()) {
                    return true;
                }

                var buildingSpec = blockTool.Template.GetSpec<BuildingSpec>();
                if (buildingSpec.ScienceCost <= 0) {
                    return true;
                }

                var unlockService = GhostPlannerUnlockServiceHolder.Instance;
                if (unlockService == null) {
                    return true;
                }

                if (unlockService.Unlocked(buildingSpec)) {
                    return true;
                }

                var eventBus = ToolUnlockingEventBusField.GetValue(__instance);
                PostToEventBus(eventBus, new ToolLockedEvent(tool));
                return false;
            }

        }

        /// <summary>
        ///   Unlocking from the entity panel calls <see cref="BuildingUnlockingService.UnlockIgnoringCost" /> without going through
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

                var eventBus = ToolUnlockingEventBusField.GetValue(unlocking);
                if (eventBus == null) {
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

                    PostToEventBus(eventBus, new ToolUnlockedEvent(blockTool));
                }
            }

        }

        /// <summary>
        ///   Game <see cref="Timberborn.SingletonSystem.EventBus" /> exposes <c>Post(object)</c>, not a generic <c>Post&lt;T&gt;</c>.
        /// </summary>
        static void PostToEventBus(object eventBus, object evt) {
            var runtimeType = eventBus.GetType();
            if (_cachedPostObjectMethod != null && _cachedPostObjectEventBusType == runtimeType) {
                _cachedPostObjectMethod.Invoke(eventBus, new[] { evt });
                return;
            }

            var post = runtimeType.GetMethod(
                "Post",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(object) },
                null);
            if (post == null) {
                throw new InvalidOperationException(
                    $"GhostPlanner: EventBus.Post(object) not found on {runtimeType.FullName}");
            }

            _cachedPostObjectEventBusType = runtimeType;
            _cachedPostObjectMethod = post;
            post.Invoke(eventBus, new[] { evt });
        }

    }

}
