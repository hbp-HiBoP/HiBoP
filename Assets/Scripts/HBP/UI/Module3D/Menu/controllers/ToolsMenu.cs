/**
 * \file    ToolsMenu.cs
 * \author  Adrien  Gannerie
 * \date    2017
 * \brief   Define ToolsMenu class
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HBP.Module3D;
using System.Linq;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// A class for managing the tools menu in the UI
    /// </summary>
    public class ToolsMenu : MonoBehaviour, UICameraOverlay
    {
        #region Properties
        // Information Scene
        Text m_SceneInformations;
        // Brain Hemispheres
        Toggle m_LeftBrainHemisphere = null;
        Toggle m_RightBrainHemisphere = null;
        // Brain Types
        Dropdown m_BrainTypes = null;
        // MarsAtlas
        Toggle m_MarsAtlas = null;
        // Views
        Button m_AddView = null;
        Button m_RemoveView = null;
        // fMRI
        Button m_AddfMRI = null;
        Button m_RemovefMRI = null;

        // Brain Types Dropdown
        public Sprite BrainTypeSprite;
        private Dictionary<Dropdown.OptionData, SceneStatesInfo.MeshType> m_BrainTypesData = new Dictionary<Dropdown.OptionData, SceneStatesInfo.MeshType>();
        #endregion

        #region Public Methods
        public void UpdateByMode(Mode mode)
        {
            UpdateInteractableButtons();
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            FindButtons();
            AddListeners();
            SetToolbarDefaultState();
        }
        void FindButtons()
        {
            // Scene Informations.
            m_SceneInformations = transform.Find("Scene Informations").GetComponentInChildren<Text>();

            // Tools.
            Transform toolsTransform = transform.Find("Tools Buttons");

            // Brain Hemispheres.
            Transform brainHemispheres = toolsTransform.Find("Brain hemispheres");
            m_LeftBrainHemisphere = brainHemispheres.Find("Left hemisphere toggle").GetComponent<Toggle>();
            m_RightBrainHemisphere = brainHemispheres.Find("Right hemisphere toggle").GetComponent<Toggle>();

            // Brain Types.
            m_BrainTypes = toolsTransform.Find("Brain Types").GetComponent<Dropdown>();

            // MarsAtlas.
            m_MarsAtlas = toolsTransform.Find("MarsAtlas").GetComponent<Toggle>();

            // Views
            Transform views = toolsTransform.Find("Views");
            m_AddView = views.Find("Add view").GetComponent<Button>();
            m_RemoveView = views.Find("Remove view").GetComponent<Button>();

            // fMRI.
            Transform fMRI = toolsTransform.Find("fMRI");
            m_AddfMRI = fMRI.Find("Add fMRI").GetComponent<Button>();      
            m_RemovefMRI = fMRI.Find("Remove fMRI").GetComponent<Button>();
        }
        void AddListeners()
        {
            // On Change Scene.
            ApplicationState.Module3D.OnSelectScene.AddListener((scene) => OnChangeScene());

            // Brain Hemispheres.
            m_LeftBrainHemisphere.onValueChanged.AddListener((display) => ApplicationState.Module3D.SelectedScene.UpdateMeshPartToDisplay(GetMeshPart(m_LeftBrainHemisphere.isOn, m_RightBrainHemisphere.isOn)));
            m_RightBrainHemisphere.onValueChanged.AddListener((display) => ApplicationState.Module3D.SelectedScene.UpdateMeshPartToDisplay(GetMeshPart(m_LeftBrainHemisphere.isOn, m_RightBrainHemisphere.isOn)));

            // Brain Types.
            m_BrainTypes.onValueChanged.AddListener((value) =>
            {
                SceneStatesInfo.MeshType type = m_BrainTypesData[m_BrainTypes.options[value]];
                if (type == SceneStatesInfo.MeshType.Hemi && ApplicationState.Module3D.SelectedScene.IsMarsAtlasEnabled)
                {
                    m_MarsAtlas.isOn = false;
                }
                ApplicationState.Module3D.SelectedScene.UpdateMeshTypeToDisplay(type);
            });
            // MarsAtlas.
            m_MarsAtlas.onValueChanged.AddListener((value) =>
            {
                if (value)
                {
                    Dropdown.OptionData whiteMeshOptionData = m_BrainTypesData.Where(item => item.Value == SceneStatesInfo.MeshType.White).Select(item => item.Key).ToArray()[0];
                    m_BrainTypes.value = m_BrainTypes.options.FindIndex((o) => o == whiteMeshOptionData);
                }
                ApplicationState.Module3D.SelectedScene.IsMarsAtlasEnabled = value;
                UpdateInteractableButtons();
            });
            // Views
            m_AddView.onClick.AddListener(() =>
            {
                ApplicationState.Module3D.ScenesManager.SelectedScene.ColumnManager.AddViewLine();
                UpdateInteractableButtons();
            });
            m_RemoveView.onClick.AddListener(() =>
            {
                ApplicationState.Module3D.ScenesManager.SelectedScene.ColumnManager.RemoveViewLine(ApplicationState.Module3D.ScenesManager.SelectedScene.ColumnManager.ViewNumber - 1);
                UpdateInteractableButtons();
            });
            // fMRI.
            m_AddfMRI.onClick.AddListener(() =>
            {
                if (ApplicationState.Module3D.SelectedScene.AddFMRIColumn()) ApplicationState.Module3D.SelectedScene.DisplayScreenMessage("fMRI successfully loaded. ", 2f, 150, 80);
                else ApplicationState.Module3D.SelectedScene.DisplayScreenMessage("Error during fMRI loading. ", 2f, 170, 80);
                UpdateInteractableButtons();
            });
            m_RemovefMRI.onClick.AddListener(() =>
            {
                ApplicationState.Module3D.SelectedScene.RemoveLastFMRIColumn();
                UpdateInteractableButtons();
            });

            //m_iEegButton.onClick.AddListener(() =>
            //{
            //    ApplicationState.Module3D.SPScene.set_CCEP_display_mode(false);
            //    UpdateUI();
            //});
            //m_CcepButton.onClick.AddListener(() =>
            //{
            //    ApplicationState.Module3D.SPScene.set_CCEP_display_mode(true);
            //    UpdateUI();
            //});



            //m_enableTriErasingButton.onClick.AddListener(() =>
            //{
            //    GetCurrentScene().set_tri_erasing(true);
            //    UpdateUI();
            //    ApplicationState.Module3D.DisplayMessageInScene(m_IsSinglePatientScene, "Triangles erasing mode enabled. ", 2f, 200, 80);
            //});
            //m_disableTriErasingButton.onClick.AddListener(() =>
            //{
            //    GetCurrentScene().set_tri_erasing(false);
            //    UpdateUI();
            //    ApplicationState.Module3D.DisplayMessageInScene(m_IsSinglePatientScene, "Triangles erasing mode disabled. ", 2f, 200, 80);
            //});
            //m_resetTriErasingButton.onClick.AddListener(() =>
            //{
            //    GetCurrentScene().reset_tri_erasing();
            //    UpdateUI();
            //});
            //m_cancelTriErasingButton.onClick.AddListener(() =>
            //{
            //    GetCurrentScene().cancel_last_tri_erasing();
            //    UpdateUI();
            //});
            //m_pointTriErasingButton.onClick.AddListener(() =>
            //{
            //    GetCurrentScene().set_tri_erasing_mode(TriEraser.Mode.OneTri);
            //    UpdateUI();
            //    ApplicationState.Module3D.DisplayMessageInScene(m_IsSinglePatientScene, "Triangle pencil. ", 1f, 120, 50);
            //});
            //m_cylinderTriErasingButton.onClick.AddListener(() =>
            //{
            //    GetCurrentScene().set_tri_erasing_mode(TriEraser.Mode.Cylinder);
            //    UpdateUI();
            //    ApplicationState.Module3D.DisplayMessageInScene(m_IsSinglePatientScene, "Cylinder pencil. ", 1f, 120, 50);
            //});
            //m_zoneTriErasingButton.onClick.AddListener(() =>
            //{
            //    GetCurrentScene().set_tri_erasing_mode(TriEraser.Mode.Zone);
            //    UpdateUI();
            //    ApplicationState.Module3D.DisplayMessageInScene(m_IsSinglePatientScene, "Zone pencil. ", 1f, 120, 50);
            //});
            //m_invertTriErasingButton.onClick.AddListener(() =>
            //{
            //    GetCurrentScene().set_tri_erasing_mode(TriEraser.Mode.Invert);
            //    UpdateUI();
            //});
            //m_expandTriErasingButton.onClick.AddListener(() =>
            //{
            //    GetCurrentScene().set_tri_erasing_mode(TriEraser.Mode.Expand);                
            //    UpdateUI();
            //});

            //m_zoneDegreesInputField.onEndEdit.AddListener((value) =>
            //{
            //    int degrees = 0;
            //    if (value.Length > 0)
            //    {
            //        degrees = int.Parse(value);
            //        if (degrees > 180)
            //            degrees = 180;
            //        if (degrees < 0)
            //            degrees = 0;
            //    }

            //    m_zoneDegreesInputField.text = "" + degrees;
            //    ApplicationState.Module3D.SPScene.set_tri_erasing_zone_degrees(1f * degrees);
            //    ApplicationState.Module3D.MPScene.set_tri_erasing_zone_degrees(1f * degrees);
            //});
        }
        void SetToolbarDefaultState()
        {
            SetToggleState(m_LeftBrainHemisphere, true, false);
            SetToggleState(m_RightBrainHemisphere, true, false);
            SetDropDown(m_BrainTypes, true, false);
            SetToggleState(m_MarsAtlas, true, false);
            SetButtonState(m_AddView, true, false);
            SetButtonState(m_RemoveView, true, false);
            SetButtonState(m_AddfMRI, true, false);
            SetButtonState(m_RemovefMRI, true, false);
        }
        void OnChangeScene()
        {
            ApplicationState.Module3D.SelectedScene.ModesManager.OnChangeMode.AddListener((mode) => UpdateInteractableButtons());
            m_SceneInformations.text = ApplicationState.Module3D.SelectedScene.Name;
            UpdateInteractableButtons();
            UpdateButtonsStatus();
        }
        /// <summary>
        /// Update the menu UI activity with the current parameters
        /// </summary>
        void UpdateInteractableButtons()
        {
            bool canAddView = ApplicationState.Module3D.SelectedScene.ColumnManager.ViewNumber < HBP3DModule.MAXIMUM_VIEW_NUMBER;
            bool canRemoveView = ApplicationState.Module3D.SelectedScene.ColumnManager.ViewNumber > 1;
            bool canAddfMRI = ApplicationState.Module3D.SelectedScene.ColumnManager.Columns.Count < HBP3DModule.MAXIMUM_COLUMN_NUMBER;
            bool canRemoveFMRI = (ApplicationState.Module3D.SelectedScene.ColumnManager.ColumnsFMRI.Count > 0);
            bool canUseMarsAtlas = ApplicationState.Module3D.SelectedScene.SceneInformation.WhiteMeshesAvailables && ApplicationState.Module3D.SelectedScene.SceneInformation.MarsAtlasParcelsLoaed;
            //bool CcepVisible = m_IsSinglePatientScene && !ApplicationState.Module3D.SPScene.is_latency_mode_enabled();
            //bool iEegVisible = (!CcepVisible && m_IsSinglePatientScene);
            //bool isTriErasingEnabled = scene.is_tri_erasing_enabled();

            switch (ApplicationState.Module3D.SelectedScene.ModesManager.CurrentMode.ID)
            {
                case Mode.ModesId.NoPathDefined:
                    SetToggleState(m_LeftBrainHemisphere, true, false);
                    SetToggleState(m_RightBrainHemisphere, true, false);
                    SetDropDown(m_BrainTypes, true, false);
                    SetToggleState(m_MarsAtlas, true, false);
                    SetButtonState(m_AddView, true, false);
                    SetButtonState(m_RemoveView, true, false);
                    SetButtonState(m_AddfMRI, true, false);
                    SetButtonState(m_RemovefMRI, true, false);
                    //set_state_button(m_iEegButton, iEegVisible, false);
                    //set_state_button(m_CcepButton, CcepVisible, false);
                    //set_state_button(m_enableTriErasingButton, true, false);
                    //set_state_button(m_disableTriErasingButton, false, false);
                    //set_state_button(m_resetTriErasingButton, false, false);
                    //set_state_button(m_cancelTriErasingButton, false, false);
                    //set_state_button(m_pointTriErasingButton, false, false);
                    //set_state_button(m_cylinderTriErasingButton, false, false);
                    //set_state_button(m_zoneTriErasingButton, false, false);
                    //set_state_button(m_invertTriErasingButton, false, false);
                    //set_state_button(m_expandTriErasingButton, false, false);
                    //m_zoneDegreesInputField.gameObject.SetActive(false);
                    //m_zoneDegreesInputField.interactable = false;
                    break;

                case Mode.ModesId.MinPathDefined:
                    SetToggleState(m_LeftBrainHemisphere, true, true);
                    SetToggleState(m_RightBrainHemisphere, true, true);
                    SetDropDown(m_BrainTypes, true, true);
                    SetToggleState(m_MarsAtlas, true, canUseMarsAtlas);
                    SetButtonState(m_AddView, true, canAddView);
                    SetButtonState(m_RemoveView, true, canRemoveView);               
                    SetButtonState(m_AddfMRI, true, canAddfMRI);
                    SetButtonState(m_RemovefMRI, true, canRemoveFMRI);
                    //set_state_button(m_enableTriErasingButton, !isTriErasingEnabled, true);
                    //set_state_button(m_disableTriErasingButton, isTriErasingEnabled, true);
                    //set_state_button(m_resetTriErasingButton, isTriErasingEnabled, true);
                    //set_state_button(m_cancelTriErasingButton, isTriErasingEnabled, true);
                    //set_state_button(m_pointTriErasingButton, isTriErasingEnabled, true);
                    //set_state_button(m_cylinderTriErasingButton, isTriErasingEnabled, true);
                    //set_state_button(m_zoneTriErasingButton, isTriErasingEnabled, true);
                    //set_state_button(m_invertTriErasingButton, isTriErasingEnabled, true);
                    //set_state_button(m_expandTriErasingButton, isTriErasingEnabled, true);
                    //m_zoneDegreesInputField.gameObject.SetActive(isTriErasingEnabled);
                    //m_zoneDegreesInputField.interactable = true;

                    break;
                case Mode.ModesId.AllPathDefined:
                    SetToggleState(m_LeftBrainHemisphere, true, true);
                    SetToggleState(m_RightBrainHemisphere, true, true);
                    SetDropDown(m_BrainTypes, true, true);
                    SetToggleState(m_MarsAtlas, true, canUseMarsAtlas);
                    SetButtonState(m_AddView, true, canAddView);
                    SetButtonState(m_RemoveView, true, canRemoveView);
                    SetButtonState(m_AddfMRI, true, canAddfMRI);
                    SetButtonState(m_RemovefMRI, true, canRemoveFMRI);
                    //set_state_button(m_iEegButton, iEegVisible, true);
                    //set_state_button(m_CcepButton, CcepVisible, true);
                    //set_state_button(m_enableTriErasingButton, !isTriErasingEnabled, true);
                    //set_state_button(m_disableTriErasingButton, isTriErasingEnabled, true);
                    //set_state_button(m_resetTriErasingButton, isTriErasingEnabled, true);
                    //set_state_button(m_cancelTriErasingButton, isTriErasingEnabled, true);
                    //set_state_button(m_pointTriErasingButton, isTriErasingEnabled, true);
                    //set_state_button(m_cylinderTriErasingButton, isTriErasingEnabled, true);
                    //set_state_button(m_zoneTriErasingButton, isTriErasingEnabled, true);
                    //set_state_button(m_invertTriErasingButton, isTriErasingEnabled, true);
                    //set_state_button(m_expandTriErasingButton, isTriErasingEnabled, true);
                    //m_zoneDegreesInputField.gameObject.SetActive(isTriErasingEnabled);
                    //m_zoneDegreesInputField.interactable = true;
                    break;
                case Mode.ModesId.ComputingAmplitudes:
                    SetToggleState(m_LeftBrainHemisphere, true, false);
                    SetToggleState(m_RightBrainHemisphere, true, false);
                    SetDropDown(m_BrainTypes, true, false);
                    SetToggleState(m_MarsAtlas, true, false);
                    SetButtonState(m_AddView, true, canAddView);
                    SetButtonState(m_RemoveView, true, canRemoveView);
                    SetButtonState(m_AddfMRI, true, false);
                    SetButtonState(m_RemovefMRI, true, false);
                    //set_state_button(m_iEegButton, iEegVisible, false);
                    //set_state_button(m_CcepButton, CcepVisible, false);
                    //set_state_button(m_enableTriErasingButton, !isTriErasingEnabled, false);
                    //set_state_button(m_disableTriErasingButton, isTriErasingEnabled, false);
                    //set_state_button(m_resetTriErasingButton, isTriErasingEnabled, false);
                    //set_state_button(m_cancelTriErasingButton, isTriErasingEnabled, false);
                    //set_state_button(m_pointTriErasingButton, isTriErasingEnabled, false);
                    //set_state_button(m_cylinderTriErasingButton, isTriErasingEnabled, false);
                    //set_state_button(m_zoneTriErasingButton, isTriErasingEnabled, false);
                    //set_state_button(m_invertTriErasingButton, isTriErasingEnabled, false);
                    //set_state_button(m_expandTriErasingButton, isTriErasingEnabled, false);
                    //m_zoneDegreesInputField.gameObject.SetActive(isTriErasingEnabled);
                    //m_zoneDegreesInputField.interactable = false;

                    break;
                case Mode.ModesId.AmplitudesComputed:
                    SetToggleState(m_LeftBrainHemisphere, true, true);
                    SetToggleState(m_RightBrainHemisphere, true, true);
                    SetDropDown(m_BrainTypes, true, true);
                    SetToggleState(m_MarsAtlas, true, canUseMarsAtlas);
                    SetButtonState(m_AddView, true, canAddView);
                    SetButtonState(m_RemoveView, true, canRemoveView);
                    SetButtonState(m_AddfMRI, true, canAddfMRI);
                    SetButtonState(m_RemovefMRI, true, canRemoveFMRI);
                    //set_state_button(m_iEegButton, iEegVisible, true);
                    //set_state_button(m_CcepButton, CcepVisible, true);
                    //set_state_button(m_enableTriErasingButton, !isTriErasingEnabled, true);
                    //set_state_button(m_disableTriErasingButton, isTriErasingEnabled, true);
                    //set_state_button(m_resetTriErasingButton, isTriErasingEnabled, true);
                    //set_state_button(m_cancelTriErasingButton, isTriErasingEnabled, true);
                    //set_state_button(m_pointTriErasingButton, isTriErasingEnabled, true);
                    //set_state_button(m_cylinderTriErasingButton, isTriErasingEnabled, true);
                    //set_state_button(m_zoneTriErasingButton, isTriErasingEnabled, true);
                    //set_state_button(m_invertTriErasingButton, isTriErasingEnabled, true);
                    //set_state_button(m_expandTriErasingButton, isTriErasingEnabled, true);
                    //m_zoneDegreesInputField.gameObject.SetActive(isTriErasingEnabled);
                    //m_zoneDegreesInputField.interactable = true;
                    break;
                case Mode.ModesId.AmpNeedUpdate:
                    SetToggleState(m_LeftBrainHemisphere, true, true);
                    SetToggleState(m_RightBrainHemisphere, true, true);
                    SetDropDown(m_BrainTypes, true, true);
                    SetToggleState(m_MarsAtlas, true, canUseMarsAtlas);
                    SetButtonState(m_AddView, true, canAddView);
                    SetButtonState(m_RemoveView, true, canRemoveView);
                    SetButtonState(m_AddfMRI, true, canAddfMRI);
                    SetButtonState(m_RemovefMRI, true, canRemoveFMRI);
                    //set_state_button(m_iEegButton, iEegVisible, true);
                    //set_state_button(m_CcepButton, CcepVisible, true);
                    //set_state_button(m_enableTriErasingButton, !isTriErasingEnabled, true);
                    //set_state_button(m_disableTriErasingButton, isTriErasingEnabled, true);
                    //set_state_button(m_resetTriErasingButton, isTriErasingEnabled, true);
                    //set_state_button(m_cancelTriErasingButton, isTriErasingEnabled, true);
                    //set_state_button(m_pointTriErasingButton, isTriErasingEnabled, true);
                    //set_state_button(m_cylinderTriErasingButton, isTriErasingEnabled, true);
                    //set_state_button(m_zoneTriErasingButton, isTriErasingEnabled, true);
                    //set_state_button(m_invertTriErasingButton, isTriErasingEnabled, true);
                    //set_state_button(m_expandTriErasingButton, isTriErasingEnabled, true);
                    //m_zoneDegreesInputField.gameObject.SetActive(isTriErasingEnabled);
                    //m_zoneDegreesInputField.interactable = true;
                    break;
                case Mode.ModesId.Error:
                    SetToggleState(m_LeftBrainHemisphere, true, false);
                    SetToggleState(m_RightBrainHemisphere, true, false);
                    SetDropDown(m_BrainTypes, true, false);
                    SetToggleState(m_MarsAtlas, true, false);
                    SetButtonState(m_AddView, true, false);
                    SetButtonState(m_RemoveView, true, false);
                    SetButtonState(m_AddfMRI, true, false);
                    SetButtonState(m_RemovefMRI, true, false);
                    //set_state_button(m_iEegButton, iEegVisible, false);
                    //set_state_button(m_CcepButton, CcepVisible, false);
                    //set_state_button(m_enableTriErasingButton, !isTriErasingEnabled, false);
                    //set_state_button(m_disableTriErasingButton, isTriErasingEnabled, false);
                    //set_state_button(m_resetTriErasingButton, isTriErasingEnabled, false);
                    //set_state_button(m_cancelTriErasingButton, isTriErasingEnabled, false);
                    //set_state_button(m_pointTriErasingButton, isTriErasingEnabled, false);
                    //set_state_button(m_cylinderTriErasingButton, isTriErasingEnabled, false);
                    //set_state_button(m_zoneTriErasingButton, isTriErasingEnabled, false);
                    //set_state_button(m_invertTriErasingButton, isTriErasingEnabled, false);
                    //set_state_button(m_expandTriErasingButton, isTriErasingEnabled, false);
                    //m_zoneDegreesInputField.gameObject.SetActive(isTriErasingEnabled);
                    //m_zoneDegreesInputField.interactable = false;
                    break;
                default:
                    Debug.LogError("Error : setUIActivity :" + ApplicationState.Module3D.SelectedScene.ModesManager.CurrentMode.ID.ToString());
                    break;
            }

            //switch (scene.current_tri_erasing_mode())
            //{
            //    case TriEraser.Mode.OneTri:
            //        m_pointTriErasingButton.interactable = false;
            //        break;
            //    case TriEraser.Mode.Cylinder:
            //        m_cylinderTriErasingButton.interactable = false;
            //        break;
            //    case TriEraser.Mode.Zone:
            //        m_zoneTriErasingButton.interactable = false;
            //        break;
            //}
        }
        void UpdateButtonsStatus()
        {
            switch (ApplicationState.Module3D.SelectedScene.SceneInformation.MeshPartToDisplay)
            {
                case SceneStatesInfo.MeshPart.Left:
                    m_LeftBrainHemisphere.isOn = true;
                    m_RightBrainHemisphere.isOn = false;
                    break;
                case SceneStatesInfo.MeshPart.Right:
                    m_LeftBrainHemisphere.isOn = false;
                    m_RightBrainHemisphere.isOn = true;
                    break;
                case SceneStatesInfo.MeshPart.Both:
                    m_LeftBrainHemisphere.isOn = true;
                    m_RightBrainHemisphere.isOn = true;
                    break;
                case SceneStatesInfo.MeshPart.None:
                    m_LeftBrainHemisphere.isOn = false;
                    m_RightBrainHemisphere.isOn = false;
                    break;
                default:
                    break;
            }
            
            m_BrainTypesData.Clear();
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            if (ApplicationState.Module3D.SelectedScene.SceneInformation.HemiMeshesAvailables)
            {
                Dropdown.OptionData greyBrain = new Dropdown.OptionData("G", BrainTypeSprite);
                options.Add(greyBrain);
                m_BrainTypesData.Add(greyBrain, SceneStatesInfo.MeshType.Hemi);
            }
            if (ApplicationState.Module3D.SelectedScene.SceneInformation.WhiteMeshesAvailables)
            {
                Dropdown.OptionData whiteBrain = new Dropdown.OptionData("W", BrainTypeSprite);
                options.Add(whiteBrain);
                m_BrainTypesData.Add(whiteBrain, SceneStatesInfo.MeshType.White);
            }
            if (ApplicationState.Module3D.SelectedScene.SceneInformation.WhiteInflatedMeshesAvailables)
            {
                Dropdown.OptionData inflatedBrain = new Dropdown.OptionData("I", BrainTypeSprite);
                options.Add(inflatedBrain);
                m_BrainTypesData.Add(inflatedBrain, SceneStatesInfo.MeshType.Inflated);
            }
            m_BrainTypes.options = options;
        }
        void SetButtonState(Button button, bool active, bool interactable)
        {
            button.interactable = interactable;
            button.gameObject.SetActive(active);
        }
        void SetToggleState(Toggle toggle, bool active, bool interactable)
        {
            toggle.interactable = interactable;
            toggle.gameObject.SetActive(active);
        }
        void SetDropDown(Dropdown dropDown, bool active, bool interactable)
        {
            dropDown.interactable = interactable;
            dropDown.gameObject.SetActive(active);
        }
        SceneStatesInfo.MeshPart GetMeshPart(bool displayLeftPart, bool displayRightPart)
        {
            if (displayLeftPart && displayRightPart) return SceneStatesInfo.MeshPart.Both;
            else if (displayLeftPart && !displayRightPart) return SceneStatesInfo.MeshPart.Left;
            else if (!displayLeftPart && displayRightPart) return SceneStatesInfo.MeshPart.Right;
            else return SceneStatesInfo.MeshPart.None;
        }
        #endregion
    }
}