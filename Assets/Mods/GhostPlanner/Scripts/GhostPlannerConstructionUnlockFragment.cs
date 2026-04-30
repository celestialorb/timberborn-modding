using Timberborn.BaseComponentSystem;
using Timberborn.AssetSystem;
using Timberborn.Buildings;
using Timberborn.ConstructionSites;
using Timberborn.CoreUI;
using Timberborn.EntitySystem;
using Timberborn.EntityPanelSystem;
using Timberborn.Localization;
using Timberborn.ScienceSystem;
using Timberborn.TemplateSystem;

using UnityEngine.UIElements;

namespace Timberborn.Mods.Daxisaurus.GhostPlanner {

    /// <summary>
    ///   Entity panel section for unfinished buildings whose blueprint is still locked: panel chrome matches
    ///   vanilla sections (<c>bg-sub-box--green</c>); inner button opens the vanilla unlock flow.
    /// </summary>
    sealed class GhostPlannerConstructionUnlockFragment : IEntityPanelFragment {

        /// <summary>Path passed to <see cref="IAssetLoader"/> for USS under AssetBundles/Resources (matches HelloWorld style).</summary>
        const string UnlockStylesheetAssetPath = "UI/Styles/GhostPlannerEntityPanel";

        static readonly string SubPanelClass = "entity-sub-panel";
        static readonly string SubBoxGreenClass = "bg-sub-box--green";

        readonly BuildingUnlockingService _buildingUnlockingService;
        readonly DialogBoxShower _dialogBoxShower;
        readonly IAssetLoader _assetLoader;
        readonly ILoc _loc;

        VisualElement _root;
        Button _unlockButton;

        ConstructionSite _constructionSite;
        BuildingSpec _buildingSpec;
        bool _panelActive;

        internal GhostPlannerConstructionUnlockFragment(
            BuildingUnlockingService buildingUnlockingService,
            DialogBoxShower dialogBoxShower,
            IAssetLoader assetLoader,
            ILoc loc) {
            _buildingUnlockingService = buildingUnlockingService;
            _dialogBoxShower = dialogBoxShower;
            _assetLoader = assetLoader;
            _loc = loc;
        }

        public VisualElement InitializeFragment() {
            _root = new NineSliceVisualElement();
            _root.AddToClassList(SubPanelClass);
            _root.AddToClassList(SubBoxGreenClass);
            _root.AddToClassList("ghost-planner-unlock-panel");
            AttachGhostPlannerStylesheet();
            _root.style.flexDirection = FlexDirection.Column;
            _root.style.alignItems = Align.Stretch;
            _root.ToggleDisplayStyle(false);

            _unlockButton = new Button();
            _unlockButton.AddToClassList("ghost-planner-unlock__action");
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

            _unlockButton.text = _loc.T("GhostPlanner.EntityPanel.UnlockAction");
            _unlockButton.SetEnabled(_buildingUnlockingService.Unlockable(_buildingSpec));
        }

        void OnUnlockClicked() {
            if (_buildingSpec == null) {
                return;
            }

            var displayName = GetBuildingDisplayName();

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

        string GetBuildingDisplayName() {
            if (_buildingSpec == null) {
                return string.Empty;
            }

            var labeled = _buildingSpec.GetSpec<LabeledEntitySpec>();
            if (labeled != null && !string.IsNullOrEmpty(labeled.DisplayNameLocKey)) {
                return _loc.T(labeled.DisplayNameLocKey);
            }

            var templateSpec = _buildingSpec.GetSpec<TemplateSpec>();
            return templateSpec?.TemplateName ?? string.Empty;
        }

        void AttachGhostPlannerStylesheet() {
            var sheet = _assetLoader.LoadSafe<StyleSheet>(UnlockStylesheetAssetPath);
            if (sheet != null) {
                _root.styleSheets.Add(sheet);
            }
        }

    }

}
