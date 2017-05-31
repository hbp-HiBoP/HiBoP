using HBP.Module3D;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;

namespace HBP.UI.Module3D
{
    public class SceneSettingsToolbar : Toolbar
    {
        #region Properties
        /// <summary>
        /// Show / hide left brain mesh
        /// </summary>
        public Toggle LeftBrainHemisphere { get; set; }
        /// <summary>
        /// Show / hide right brain mesh
        /// </summary>
        public Toggle RightBrainHemisphere { get; set; }
        /// <summary>
        /// Change brain type (grey, white, inflated)
        /// </summary>
        public Dropdown BrainTypes { get; set; }
        /// <summary>
        /// Show / hide Mars Atlas
        /// </summary>
        public Toggle MarsAtlas { get; set; }
        /// <summary>
        /// Add a view to the selected scene
        /// </summary>
        public Button AddView { get; set; }
        /// <summary>
        /// Remove the last view from the selected scene
        /// </summary>
        public Button RemoveView { get; set; }
        /// <summary>
        /// Add a FMRI column to the selected scene
        /// </summary>
        public Button AddFMRI { get; set; }
        /// <summary>
        /// Remove the last FMRI column from the selected scene
        /// </summary>
        public Button RemoveFMRI { get; set; }

        [SerializeField, Candlelight.PropertyBackingField]
        private Sprite m_BrainTypeSprite;
        /// <summary>
        /// Sprite for the brain type icon
        /// </summary>
        public Sprite BrainTypeSprite
        {
            get
            {
                return m_BrainTypeSprite;
            }
            set
            {
                m_BrainTypeSprite = value;
            }
        }
        /// <summary>
        /// Dictionary that stores the MeshType info corresponding to the OptionData in the dropdown menu
        /// </summary>
        private Dictionary<Dropdown.OptionData, SceneStatesInfo.MeshType> m_BrainTypeByOptionData = new Dictionary<Dropdown.OptionData, SceneStatesInfo.MeshType>();
        /// <summary>
        /// Dictionary that stores the OptionData info corresponding to the MeshType in the dropdown menu
        /// </summary>
        private Dictionary<SceneStatesInfo.MeshType, Dropdown.OptionData> m_OptionDataByBrainType = new Dictionary<SceneStatesInfo.MeshType, Dropdown.OptionData>();
        #endregion

        #region Private Methods
        private void Awake()
        {
            Initialize();
        }
        /// <summary>
        /// Link elements to the toolbar
        /// </summary>
        /// <param name="parent">Transform of the toolbar</param>
        protected override void FindButtons()
        {
            Transform brainMeshes = transform.Find("Brain Meshes");
            LeftBrainHemisphere = brainMeshes.Find("Left").GetComponent<Toggle>();
            RightBrainHemisphere = brainMeshes.Find("Right").GetComponent<Toggle>();
            
            BrainTypes = transform.Find("Brain Types").GetComponent<Dropdown>();
            
            MarsAtlas = transform.Find("Mars Atlas").GetComponent<Toggle>();
            
            Transform views = transform.Find("Views");
            AddView = views.Find("Add View").GetComponent<Button>();
            RemoveView = views.Find("Remove View").GetComponent<Button>();
            
            Transform fmri = transform.Find("FMRI");
            AddFMRI = fmri.Find("Add FMRI").GetComponent<Button>();
            RemoveFMRI = fmri.Find("Remove FMRI").GetComponent<Button>();
        }
        /// <summary>
        /// Add the listeners to the elements of the toolbar
        /// </summary>
        protected override void AddListeners()
        {
            base.AddListeners();

            LeftBrainHemisphere.onValueChanged.AddListener((display) =>
            {
                if (ChangeSceneLock) return;

                ApplicationState.Module3D.SelectedScene.UpdateMeshPartToDisplay(GetMeshPart(LeftBrainHemisphere.isOn, RightBrainHemisphere.isOn));
            });

            RightBrainHemisphere.onValueChanged.AddListener((display) =>
            {
                if (ChangeSceneLock) return;

                ApplicationState.Module3D.SelectedScene.UpdateMeshPartToDisplay(GetMeshPart(LeftBrainHemisphere.isOn, RightBrainHemisphere.isOn));
            });

            BrainTypes.onValueChanged.AddListener((value) =>
            {
                if (ChangeSceneLock) return;

                SceneStatesInfo.MeshType type = m_BrainTypeByOptionData[BrainTypes.options[value]];
                if (type == SceneStatesInfo.MeshType.Grey && ApplicationState.Module3D.SelectedScene.IsMarsAtlasEnabled)
                {
                    MarsAtlas.isOn = false;
                }
                ApplicationState.Module3D.SelectedScene.UpdateMeshTypeToDisplay(type);
            });

            MarsAtlas.onValueChanged.AddListener((value) =>
            {
                if (ChangeSceneLock) return;

                if (value)
                {
                    Dropdown.OptionData whiteMeshOptionData = m_OptionDataByBrainType[SceneStatesInfo.MeshType.White];
                    BrainTypes.value = BrainTypes.options.FindIndex((o) => o == whiteMeshOptionData);
                }
                ApplicationState.Module3D.SelectedScene.IsMarsAtlasEnabled = value;
                UpdateInteractableButtons();
            });

            AddView.onClick.AddListener(() =>
            {
                if (ChangeSceneLock) return;

                ApplicationState.Module3D.ScenesManager.SelectedScene.ColumnManager.AddViewLine();
                UpdateInteractableButtons();
            });

            RemoveView.onClick.AddListener(() =>
            {
                if (ChangeSceneLock) return;

                ApplicationState.Module3D.ScenesManager.SelectedScene.ColumnManager.RemoveViewLine(ApplicationState.Module3D.ScenesManager.SelectedScene.ColumnManager.ViewNumber - 1);
                UpdateInteractableButtons();
            });

            AddFMRI.onClick.AddListener(() =>
            {
                if (ChangeSceneLock) return;

                if (ApplicationState.Module3D.SelectedScene.AddFMRIColumn()) ApplicationState.Module3D.SelectedScene.DisplayScreenMessage("fMRI successfully loaded. ", 2f, 150, 80);
                else ApplicationState.Module3D.SelectedScene.DisplayScreenMessage("Error during fMRI loading. ", 2f, 170, 80);
                UpdateInteractableButtons();
            });

            RemoveFMRI.onClick.AddListener(() =>
            {
                if (ChangeSceneLock) return;

                ApplicationState.Module3D.SelectedScene.RemoveLastFMRIColumn();
                UpdateInteractableButtons();
            });
        }
        /// <summary>
        /// Set the toolbar elements to their default state
        /// </summary>
        protected override void DefaultState()
        {
            LeftBrainHemisphere.isOn = false;
            RightBrainHemisphere.isOn = false;
            BrainTypes.value = 0;
            MarsAtlas.isOn = false;

            LeftBrainHemisphere.interactable = false;
            RightBrainHemisphere.interactable = false;
            BrainTypes.interactable = false;
            MarsAtlas.interactable = false;
            AddView.interactable = false;
            RemoveView.interactable = false;
            AddFMRI.interactable = false;
            RemoveFMRI.interactable = false;
        }
        /// <summary>
        /// Update the interactable buttons of the toolbar
        /// </summary>
        protected override void UpdateInteractableButtons()
        {
            bool canAddView = ApplicationState.Module3D.SelectedScene.ColumnManager.ViewNumber < HBP3DModule.MAXIMUM_VIEW_NUMBER;
            bool canRemoveView = ApplicationState.Module3D.SelectedScene.ColumnManager.ViewNumber > 1;
            bool canAddfMRI = ApplicationState.Module3D.SelectedScene.ColumnManager.Columns.Count < HBP3DModule.MAXIMUM_COLUMN_NUMBER;
            bool canRemoveFMRI = (ApplicationState.Module3D.SelectedScene.ColumnManager.ColumnsFMRI.Count > 0);
            bool canUseMarsAtlas = ApplicationState.Module3D.SelectedScene.SceneInformation.WhiteMeshesAvailables && ApplicationState.Module3D.SelectedScene.SceneInformation.MarsAtlasParcelsLoaed;

            switch (ApplicationState.Module3D.SelectedScene.ModesManager.CurrentMode.ID)
            {
                case Mode.ModesId.NoPathDefined:
                    LeftBrainHemisphere.interactable = false;
                    RightBrainHemisphere.interactable = false;
                    BrainTypes.interactable = false;
                    MarsAtlas.interactable = false;
                    AddView.interactable = false;
                    RemoveView.interactable = false;
                    AddFMRI.interactable = false;
                    RemoveFMRI.interactable = false;
                    break;

                case Mode.ModesId.MinPathDefined:
                    LeftBrainHemisphere.interactable = true;
                    RightBrainHemisphere.interactable = true;
                    BrainTypes.interactable = true;
                    MarsAtlas.interactable = canUseMarsAtlas;
                    AddView.interactable = canAddView;
                    RemoveView.interactable = canRemoveView;
                    AddFMRI.interactable = canAddfMRI;
                    RemoveFMRI.interactable = canRemoveFMRI;
                    break;

                case Mode.ModesId.AllPathDefined:
                    LeftBrainHemisphere.interactable = true;
                    RightBrainHemisphere.interactable = true;
                    BrainTypes.interactable = true;
                    MarsAtlas.interactable = canUseMarsAtlas;
                    AddView.interactable = canAddView;
                    RemoveView.interactable = canRemoveView;
                    AddFMRI.interactable = canAddfMRI;
                    RemoveFMRI.interactable = canRemoveFMRI;
                    break;

                case Mode.ModesId.ComputingAmplitudes:
                    LeftBrainHemisphere.interactable = false;
                    RightBrainHemisphere.interactable = false;
                    BrainTypes.interactable = false;
                    MarsAtlas.interactable = false;
                    AddView.interactable = canAddView;
                    RemoveView.interactable = canRemoveView;
                    AddFMRI.interactable = false;
                    RemoveFMRI.interactable = false;
                    break;

                case Mode.ModesId.AmplitudesComputed:
                    LeftBrainHemisphere.interactable = true;
                    RightBrainHemisphere.interactable = true;
                    BrainTypes.interactable = true;
                    MarsAtlas.interactable = canUseMarsAtlas;
                    AddView.interactable = canAddView;
                    RemoveView.interactable = canRemoveView;
                    AddFMRI.interactable = canAddfMRI;
                    RemoveFMRI.interactable = canRemoveFMRI;
                    break;

                case Mode.ModesId.AmpNeedUpdate:
                    LeftBrainHemisphere.interactable = true;
                    RightBrainHemisphere.interactable = true;
                    BrainTypes.interactable = true;
                    MarsAtlas.interactable = canUseMarsAtlas;
                    AddView.interactable = canAddView;
                    RemoveView.interactable = canRemoveView;
                    AddFMRI.interactable = canAddfMRI;
                    RemoveFMRI.interactable = canRemoveFMRI;
                    break;

                case Mode.ModesId.Error:
                    LeftBrainHemisphere.interactable = false;
                    RightBrainHemisphere.interactable = false;
                    BrainTypes.interactable = false;
                    MarsAtlas.interactable = false;
                    AddView.interactable = false;
                    RemoveView.interactable = false;
                    AddFMRI.interactable = false;
                    RemoveFMRI.interactable = false;
                    break;

                default:
                    Debug.LogError("Error : setUIActivity :" + ApplicationState.Module3D.SelectedScene.ModesManager.CurrentMode.ID.ToString());
                    break;
            }
        }
        /// <summary>
        /// Change the status of the toolbar elements according to the selected scene parameters
        /// </summary>
        protected override void UpdateButtonsStatus(UpdateToolbarType type)
        {
            if (type == UpdateToolbarType.Scene)
            {
                // Brain part
                switch (ApplicationState.Module3D.SelectedScene.SceneInformation.MeshPartToDisplay)
                {
                    case SceneStatesInfo.MeshPart.Left:
                        LeftBrainHemisphere.isOn = true;
                        RightBrainHemisphere.isOn = false;
                        break;
                    case SceneStatesInfo.MeshPart.Right:
                        LeftBrainHemisphere.isOn = false;
                        RightBrainHemisphere.isOn = true;
                        break;
                    case SceneStatesInfo.MeshPart.Both:
                        LeftBrainHemisphere.isOn = true;
                        RightBrainHemisphere.isOn = true;
                        break;
                    case SceneStatesInfo.MeshPart.None:
                        LeftBrainHemisphere.isOn = false;
                        RightBrainHemisphere.isOn = false;
                        break;
                    default:
                        break;
                }

                // Brain type
                m_BrainTypeByOptionData.Clear();
                m_OptionDataByBrainType.Clear();
                List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
                Dropdown.OptionData greyBrain = new Dropdown.OptionData("G", BrainTypeSprite);
                Dropdown.OptionData whiteBrain = new Dropdown.OptionData("W", BrainTypeSprite);
                Dropdown.OptionData inflatedBrain = new Dropdown.OptionData("I", BrainTypeSprite);
                if (ApplicationState.Module3D.SelectedScene.SceneInformation.GreyMeshesAvailables)
                {
                    options.Add(greyBrain);
                    m_BrainTypeByOptionData.Add(greyBrain, SceneStatesInfo.MeshType.Grey);
                    m_OptionDataByBrainType.Add(SceneStatesInfo.MeshType.Grey, greyBrain);
                }
                if (ApplicationState.Module3D.SelectedScene.SceneInformation.WhiteMeshesAvailables)
                {
                    options.Add(whiteBrain);
                    m_BrainTypeByOptionData.Add(whiteBrain, SceneStatesInfo.MeshType.White);
                    m_OptionDataByBrainType.Add(SceneStatesInfo.MeshType.White, whiteBrain);
                }
                if (ApplicationState.Module3D.SelectedScene.SceneInformation.WhiteInflatedMeshesAvailables)
                {
                    options.Add(inflatedBrain);
                    m_BrainTypeByOptionData.Add(inflatedBrain, SceneStatesInfo.MeshType.Inflated);
                    m_OptionDataByBrainType.Add(SceneStatesInfo.MeshType.Inflated, inflatedBrain);
                }
                BrainTypes.options = options;

                int dropDownIndex = 0;
                switch (ApplicationState.Module3D.SelectedScene.SceneInformation.MeshTypeToDisplay)
                {
                    case SceneStatesInfo.MeshType.Grey:
                        dropDownIndex = BrainTypes.options.FindIndex((o) => o == greyBrain);
                        break;
                    case SceneStatesInfo.MeshType.White:
                        dropDownIndex = BrainTypes.options.FindIndex((o) => o == whiteBrain);
                        break;
                    case SceneStatesInfo.MeshType.Inflated:
                        dropDownIndex = BrainTypes.options.FindIndex((o) => o == inflatedBrain);
                        break;
                    default:
                        dropDownIndex = 0;
                        break;
                }
                BrainTypes.value = dropDownIndex;

                // Mars Atlas
                MarsAtlas.isOn = ApplicationState.Module3D.SelectedScene.IsMarsAtlasEnabled;
            }
        }
        /// <summary>
        /// Return the mesh part according to which mesh is displayed
        /// </summary>
        /// <param name="left">Is the left mesh displayed ?</param>
        /// <param name="right">Is the right mesh displayed ?</param>
        /// <returns>Mesh part enum identifier</returns>
        private SceneStatesInfo.MeshPart GetMeshPart(bool left, bool right)
        {
            if (left && right) return SceneStatesInfo.MeshPart.Both;
            if (left && !right) return SceneStatesInfo.MeshPart.Left;
            if (!left && right) return SceneStatesInfo.MeshPart.Right;
            return SceneStatesInfo.MeshPart.None;
        }
        #endregion
    }
}