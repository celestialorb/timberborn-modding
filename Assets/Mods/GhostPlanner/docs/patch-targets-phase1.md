# Phase 1 — Patch targets (GhostPlanner / pre-unlock placement)

Analysis used **`dotnet ilspycmd`** against assemblies under  
`/steamapps/common/Timberborn/Timberborn_Data/Managed`  
(game install path may differ on your machine).

## Central unlock API

| Type | Assembly | Role |
|------|----------|------|
| `Timberborn.ScienceSystem.BuildingUnlockingService` | `Timberborn.ScienceSystem.dll` | Tracks unlocked buildings; **`Unlocked(BuildingSpec)`** returns whether a science-cost building may be used as **unlocked**. |

Key logic (decompiled):

- If `buildingSpec.ScienceCost == 0` → **always unlocked**.
- Else → unlocked iff template name is in an internal `_unlockedBuildings` set (paid unlock via science points or `UnlockIgnoringCost`).

Related: `Unlock`, `UnlockIgnoringCost`, `Unlockable`, save/load via `UnlockedBuildings` key.

## Why locked buildings cannot be placed today

Placement goes through **`Timberborn.BlockObjectTools.BlockObjectTool`**. On confirm (`ActionCallback`), if **`ToolUnlockingService.IsLocked(this)`**, the game calls **`TryToUnlock`** (unlock dialog / instant-unlock hotkey) instead of **`Place`**.

**`Timberborn.ToolSystem.ToolUnlockingService`** does **not** embed science rules. It asks **`IToolLocker`** implementations whether to lock a tool (`ShouldLock` → `LockIfNeeded`).

The relevant locker is:

| Type | Assembly | Role |
|------|----------|------|
| `Timberborn.BuildingTools.BuildingToolLocker` | `Timberborn.BuildingTools.dll` | **`ShouldLock(ITool)`** → for **`BlockObjectTool`**, resolves **`BuildingSpec`** from **`tool.Template`** and returns **`!_buildingUnlockingService.Unlocked(buildingSpec)`**. |

So **science/placement gating is entirely this locker + unlock service**, not understructures.

**Note:** `Timberborn.BuildingAvailability.BuildingAvailabilityValidator.IsAvailableForPlacement` only handles **understructure** constraints (`UnderstructureConstraintSpec`), not science.

## Construction does **not** consult unlock (confirmed)

A repo-wide string scan of managed DLLs shows **`BuildingUnlockingService` appears only in:**

- `Timberborn.ScienceSystem.dll` (definition + saves)
- `Timberborn.BuildingTools.dll` (building tool locker / UI)
- `Timberborn.PlantingUI.dll` (planting unlock registry — separate concern)
- `Timberborn.TutorialSteps.dll`

**Not** referenced from **`Timberborn.ConstructionSites.dll`**.

Construction progress uses **`Timberborn.ConstructionSites.ConstructionSite.IncreaseBuildTime`**, **`ReadyToBuild`**, **`IsOn`**, validators, **`BlockableObject`**, etc., without checking **`BuildingUnlockingService`**.

So:

- Letting the player **place** before paying science requires touching **tool lock / unlock checks** (e.g. **`BuildingToolLocker.ShouldLock`** or equivalent behavior).
- Forcing **no beaver construction labor until unlocked** requires **new** Harmony logic on construction paths (e.g. **`ConstructionSite.IncreaseBuildTime`** Prefix returning **`false`** when **`!BuildingUnlockingService.Unlocked(buildingSpec)`**, and/or blocking **`ReserveForBuild`** / builder assignment — pick one consistent approach after testing UX).

## Suggested Harmony anchors for phase 2

1. **Placement (allow selecting/placing locked templates)**  
   - **`BuildingToolLocker.ShouldLock`**: Prefix returning **`false`** so **`BlockObjectTool`** is never treated as locked for science *(narrow further if you must not affect non-building tools — today only `BlockObjectTool` with **`BuildingSpec`** returns true from **`TryGetBuildingFromTool`**)*.

2. **Labor (no progress until unlocked)**  
   - **`ConstructionSite.IncreaseBuildTime`**: Prefix **`false`** when building spec is not **`BuildingUnlockingService.Unlocked`** (resolve **`BuildingSpec`** from the site’s **`BuildingSpec`** component or equivalent field already on **`ConstructionSite`** — visible in decompilation as **`_buildingSpec`**).
   - Optionally also gate **`ReserveForBuild`** so builders do not occupy slots until unlocked (better UX; verify **`Builder`** / hub behavior).

3. **Avoid** globally patching **`BuildingUnlockingService.Unlocked`** to always **`true`** unless you audit **every** caller — it would fix placement but could have unintended effects where unlock state matters for UI or other systems (planting registry uses the service separately).

Re-verify types and line-level behavior after game patches by re-running **`dotnet ilspycmd -t ...`** on your **`MinimumGameVersion`**.
