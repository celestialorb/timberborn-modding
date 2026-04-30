using System.Collections.Generic;

using Timberborn.AlertPanelSystem;
using Timberborn.ConstructionSites;
using Timberborn.CoreUI;
using Timberborn.EntitySystem;
using Timberborn.Localization;
using Timberborn.SelectionSystem;
using Timberborn.StatusSystem;

using UnityEngine.UIElements;

namespace Timberborn.Mods.Daxisaurus.ColonyPlanner {

    /// <summary>
    ///   Persistent lower-left alert row (vanilla <c>AlertPanel</c>) listing ghost construction sites that still
    ///   require science unlock — same strip as e.g. unstaffed building warnings, not quick toasts.
    ///   Click cycles <see cref="EntitySelectionService.SelectAndFocusOn"/> across blocked sites.
    /// </summary>
    public sealed class ColonyPlannerScienceLockAlertFragment : IAlertFragment {

        const string ScienceStatusIconName = "NotEnoughScience";

        readonly EntityRegistry _entityRegistry;
        readonly EntitySelectionService _entitySelection;
        readonly AlertPanelRowFactory _rowFactory;
        readonly StatusSpriteLoader _spriteLoader;
        readonly ILoc _loc;

        readonly List<ConstructionSite> _blockedSites = new();

        VisualElement _row;
        int _cycleIndex = -1;
        int _lastBlockedCount = -1;

        public ColonyPlannerScienceLockAlertFragment(
            EntityRegistry entityRegistry,
            EntitySelectionService entitySelection,
            AlertPanelRowFactory rowFactory,
            StatusSpriteLoader spriteLoader,
            ILoc loc) {
            _entityRegistry = entityRegistry;
            _entitySelection = entitySelection;
            _rowFactory = rowFactory;
            _spriteLoader = spriteLoader;
            _loc = loc;
        }

        public void InitializeAlertFragment(VisualElement root) {
            var sprite = _spriteLoader.LoadSprite(ScienceStatusIconName);
            _row = _rowFactory.Create(sprite);
            _row.Q<Button>("Button").RegisterCallback<ClickEvent>(OnAlertRowClicked, TrickleDown.NoTrickleDown);
            root.Add(_row);
        }

        public void UpdateAlertFragment() {
            if (_row == null || ColonyPlannerUnlockServiceHolder.Instance == null) {
                return;
            }

            RebuildBlockedSites();
            var blocked = _blockedSites.Count;

            if (_lastBlockedCount != blocked) {
                _cycleIndex = -1;
                _lastBlockedCount = blocked;
            }

            if (blocked <= 0) {
                _row.ToggleDisplayStyle(false);
                return;
            }

            _row.ToggleDisplayStyle(true);
            _row.Q<Button>("Button").text =
                _loc.T("ColonyPlanner.Alert.ScienceLockedBuildings", blocked);
        }

        void OnAlertRowClicked(ClickEvent evt) {
            evt.StopPropagation();
            RebuildBlockedSites();
            if (_blockedSites.Count == 0) {
                return;
            }

            _cycleIndex = (_cycleIndex + 1) % _blockedSites.Count;
            var site = _blockedSites[_cycleIndex];
            _entitySelection.SelectAndFocusOn(site);
        }

        void RebuildBlockedSites() {
            _blockedSites.Clear();
            foreach (var entity in _entityRegistry.Entities) {
                if (entity.Deleted || !entity.Initialized) {
                    continue;
                }

                var site = entity.GetComponentInChildren<ConstructionSite>(true);
                if (site == null) {
                    continue;
                }

                if (!ColonyPlannerUnlockGate.IsConstructionWorkBlocked(site)) {
                    continue;
                }

                _blockedSites.Add(site);
            }
        }

    }

}
