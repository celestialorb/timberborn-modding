using Timberborn.BaseComponentSystem;
using Timberborn.Buildings;
using Timberborn.ConstructionSites;
using Timberborn.CoreUI;
using Timberborn.EntityPanelSystem;
using Timberborn.Localization;
using Timberborn.ScienceSystem;
using Timberborn.TemplateSystem;

using UnityEngine.UIElements;

namespace Timberborn.Mods.Daxisaurus.GhostPlanner {

    /// <summary>
    ///   Entity panel section for unfinished buildings whose blueprint is still locked: offers an unlock
    ///   action using the same science payment as vanilla pre-place dialogs.
    /// </summary>
    sealed class GhostPlannerConstructionUnlockFragment : IEntityPanelFragment {

        static readonly string SubPanelClass = "entity-sub-panel";
        static readonly string SubBoxClass = "bg-sub-box";

        readonly BuildingUnlockingService _buildingUnlockingService;
        readonly DialogBoxShower _dialogBoxShower;
        readonly ILoc _loc;

        VisualElement _root;
        Label _hintLabel;
        Button _unlockButton;

        ConstructionSite _constructionSite;
        BuildingSpec _buildingSpec;
        bool _panelActive;

        internal GhostPlannerConstructionUnlockFragment(
            BuildingUnlockingService buildingUnlockingService,
            DialogBoxShower dialogBoxShower,
            ILoc loc) {
            _buildingUnlockingService = buildingUnlockingService;
            _dialogBoxShower = dialogBoxShower;
            _loc = loc;
        }

        public VisualElement InitializeFragment() {
            _root = new NineSliceVisualElement();
            _root.AddToClassList(SubPanelClass);
            _root.AddToClassList(SubBoxClass);
            _root.style.flexDirection = FlexDirection.Column;
            _root.style.alignItems = Align.Stretch;
            _root.ToggleDisplayStyle(false);

            _hintLabel = new Label();
            _hintLabel.AddToClassList("entity-fragment__label");
            _hintLabel.style.whiteSpace = WhiteSpace.Normal;
            _root.Add(_hintLabel);

            _unlockButton = new Button();
            _unlockButton.AddToClassList("entity-fragment__button");
            _unlockButton.RegisterCallback<ClickEvent>(_ => OnUnlockClicked());
            _root.Add(_unlockButton);

            return _root;
        }

        public void ShowFragment(BaseComponent entity) {
            _constructionSite = entity.GetComponent<ConstructionSite>();
            if (_constructionSite == null) {
                ClearFragment();
                return;
            }

            _buildingSpec = GhostPlannerBuildingSpecAccessor.FromConstructionSite(_constructionSite);
            if (_buildingSpec == null || _buildingSpec.ScienceCost <= 0) {
                ClearFragment();
                return;
            }

            if (!GhostPlannerUnlockGate.IsConstructionWorkBlocked(_constructionSite)) {
                ClearFragment();
                return;
            }

            _panelActive = true;
            _hintLabel.text = _loc.T("GhostPlanner.EntityPanel.Hint");
            _root.ToggleDisplayStyle(true);
            RefreshUnlockUi();
        }

        public void ClearFragment() {
            _panelActive = false;
            _constructionSite = null;
            _buildingSpec = null;
            _root.ToggleDisplayStyle(false);
        }

        public void UpdateFragment() {
            if (!_panelActive || _buildingSpec == null || _constructionSite == null) {
                return;
            }

            if (!GhostPlannerUnlockGate.IsConstructionWorkBlocked(_constructionSite)) {
                ClearFragment();
                return;
            }

            RefreshUnlockUi();
        }

        void RefreshUnlockUi() {
            if (_buildingSpec == null) {
                return;
            }

            _unlockButton.text = _loc.T("GhostPlanner.EntityPanel.UnlockButton", _buildingSpec.ScienceCost);
            _unlockButton.SetEnabled(_buildingUnlockingService.Unlockable(_buildingSpec));
        }

        void OnUnlockClicked() {
            if (_buildingSpec == null) {
                return;
            }

            var displayName = GetBuildingTemplateLabel(_buildingSpec);

            if (!_buildingUnlockingService.Unlockable(_buildingSpec)) {
                _dialogBoxShower.Create()
                    .SetMessage(_loc.T("BuildingTools.CantUnlock", displayName, _buildingSpec.ScienceCost))
                    .SetConfirmButton(() => { })
                    .Show();
                return;
            }

            _dialogBoxShower.Create()
                .SetMessage(_loc.T("BuildingTools.UnlockPrompt", displayName, _buildingSpec.ScienceCost))
                .SetConfirmButton(ConfirmUnlock)
                .SetDefaultCancelButton()
                .Show();
        }

        void ConfirmUnlock() {
            if (_buildingSpec == null) {
                return;
            }

            try {
                _buildingUnlockingService.Unlock(_buildingSpec);
                ClearFragment();
            }
            catch {
                RefreshUnlockUi();
            }
        }

        static string GetBuildingTemplateLabel(BuildingSpec buildingSpec) {
            var templateSpec = buildingSpec.GetSpec<TemplateSpec>();
            return templateSpec != null ? templateSpec.TemplateName : string.Empty;
        }

    }

}
