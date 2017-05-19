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

namespace HBP.UI.Module3D
{
    /// <summary>
    /// A class for managing the tools menu in the UI
    /// </summary>
    public class ToolsMenu : MonoBehaviour, UICameraOverlay
    {
        #region Properties
        ScenesManager m_ScenesManager = null;

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
        //Button m_iEegButton = null;
        //Button m_CcepButton = null;
        //Button m_enableTriErasingButton = null;
        //Button m_disableTriErasingButton = null;
        //Button m_resetTriErasingButton = null;
        //Button m_cancelTriErasingButton = null;
        //Button m_pointTriErasingButton = null;
        //Button m_zoneTriErasingButton = null;
        //Button m_invertTriErasingButton = null;
        //Button m_expandTriErasingButton = null;
        //Button m_cylinderTriErasingButton = null;
        //InputField m_zoneDegreesInputField = null;    
        #endregion

        #region Public Methods
        /// <summary>
        /// Init the menu
        /// </summary>
        /// <param name="scenesManager"></param>
        public void Initialize(ScenesManager scenesManager)
        {
            m_ScenesManager = scenesManager;
            FindButtons();
            AddListeners();
        }
        public void UpdateByMode(Mode mode)
        {
            UpdateUI();
        }
        #endregion

        #region Private Methods
        void FindButtons()
        {
            // Scene Informations.
            m_SceneInformations = transform.FindChild("Scene Informations").GetComponentInChildren<Text>();

            // Tools.
            Transform toolsTransform = transform.FindChild("Tools Buttons");
            // Brain Hemispheres.
            Transform brainHemispheres = toolsTransform.FindChild("Brain hemispheres");
            m_LeftBrainHemisphere = brainHemispheres.FindChild("Left hemisphere toggle").GetComponent<Toggle>();
            m_RightBrainHemisphere = brainHemispheres.FindChild("Right hemisphere toggle").GetComponent<Toggle>();
            // Brain Types.
            m_BrainTypes = toolsTransform.FindChild("Brain Types").GetComponent<Dropdown>();   
            // MarsAtlas.
            m_MarsAtlas = toolsTransform.FindChild("MarsAtlas").GetComponent<Toggle>();
            // Views
            Transform views = toolsTransform.FindChild("Views");
            m_AddView = views.FindChild("Add view").GetComponent<Button>();
            m_RemoveView = views.FindChild("Remove view").GetComponent<Button>();
            // fMRI.
            Transform fMRI = toolsTransform.FindChild("fMRI");
            m_AddfMRI = fMRI.FindChild("Add fMRI").GetComponent<Button>();      
            m_RemovefMRI = fMRI.FindChild("Remove fMRI").GetComponent<Button>();

            // m_iEegButton = transform.Find("iEEG button").GetComponent<Button>();
            //m_CcepButton = transform.Find("CCEP button").GetComponent<Button>();
            //m_enableTriErasingButton = transform.Find("tri erasing button").GetComponent<Button>();
            //m_disableTriErasingButton = transform.Find("escape tri erasing button").GetComponent<Button>();
            //m_resetTriErasingButton = transform.Find("reset tri erasing button").GetComponent<Button>();
            //m_cancelTriErasingButton = transform.Find("cancel tri erasing button").GetComponent<Button>();
            //m_pointTriErasingButton = transform.Find("onePoint tri erasing button").GetComponent<Button>();
            //m_cylinderTriErasingButton = transform.Find("cylinder tri erasing button").GetComponent<Button>();
            //m_zoneTriErasingButton = transform.Find("zone tri erasing button").GetComponent<Button>();
            //m_invertTriErasingButton = transform.Find("invert tri erasing button").GetComponent<Button>();
            //m_expandTriErasingButton = transform.Find("expand tri erasing button").GetComponent<Button>();
            //m_zoneDegreesInputField = transform.Find("zone degree input field").GetComponent<InputField>();
        }
        void AddListeners()
        {
            // On Change Scene.
            m_ScenesManager.OnChangeSelectedScene.AddListener((scene) => OnChangeScene());

            // Brain Hemispheres.
            m_LeftBrainHemisphere.onValueChanged.AddListener((display) => m_ScenesManager.SelectedScene.UpdateMeshPartToDisplay(GetMeshPart(m_LeftBrainHemisphere.isOn, m_RightBrainHemisphere.isOn)));
            m_LeftBrainHemisphere.onValueChanged.AddListener((display) => m_ScenesManager.SelectedScene.UpdateMeshPartToDisplay(GetMeshPart(m_LeftBrainHemisphere.isOn, m_RightBrainHemisphere.isOn)));
            // Brain Types.
            m_BrainTypes.onValueChanged.AddListener((value) =>
            {
                if (value == 0 && m_ScenesManager.SelectedScene.IsMarsAtlasEnabled) m_ScenesManager.SelectedScene.IsMarsAtlasEnabled = false;
                m_ScenesManager.SelectedScene.UpdateMeshTypeToDisplay((SceneStatesInfo.MeshType)value);
            });
            // MarsAtlas.
            m_MarsAtlas.onValueChanged.AddListener((value) =>
            {
                if (value) m_ScenesManager.SelectedScene.UpdateMeshTypeToDisplay(SceneStatesInfo.MeshType.White);
                m_ScenesManager.SelectedScene.IsMarsAtlasEnabled = value;
                UpdateUI();
            });
            // Views
            m_AddView.onClick.AddListener(() =>
            {
                //m_ScenesManager.CamerasManager.AddLine(m_ScenesManager.SelectedScene.Type);
                UpdateUI();
            });
            m_RemoveView.onClick.AddListener(() =>
            {
                //m_ScenesManager.CamerasManager.RemoveLine(m_ScenesManager.SelectedScene.Type);
                UpdateUI();
            });
            // fMRI.
            m_AddfMRI.onClick.AddListener(() =>
            {
                if (ApplicationState.Module3D.ScenesManager.SelectedScene.AddFMRIColumn()) m_ScenesManager.SelectedScene.DisplayScreenMessage("fMRI successfully loaded. ", 2f, 150, 80);
                else m_ScenesManager.SelectedScene.DisplayScreenMessage("Error during fMRI loading. ", 2f, 170, 80);
                UpdateUI();
            });
            m_RemovefMRI.onClick.AddListener(() =>
            {
                ApplicationState.Module3D.ScenesManager.SelectedScene.RemoveLastFMRIColumn();
                UpdateUI();
            });

            //m_iEegButton.onClick.AddListener(() =>
            //{
            //    m_ScenesManager.SPScene.set_CCEP_display_mode(false);
            //    UpdateUI();
            //});
            //m_CcepButton.onClick.AddListener(() =>
            //{
            //    m_ScenesManager.SPScene.set_CCEP_display_mode(true);
            //    UpdateUI();
            //});



            //m_enableTriErasingButton.onClick.AddListener(() =>
            //{
            //    GetCurrentScene().set_tri_erasing(true);
            //    UpdateUI();
            //    m_ScenesManager.DisplayMessageInScene(m_IsSinglePatientScene, "Triangles erasing mode enabled. ", 2f, 200, 80);
            //});
            //m_disableTriErasingButton.onClick.AddListener(() =>
            //{
            //    GetCurrentScene().set_tri_erasing(false);
            //    UpdateUI();
            //    m_ScenesManager.DisplayMessageInScene(m_IsSinglePatientScene, "Triangles erasing mode disabled. ", 2f, 200, 80);
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
            //    m_ScenesManager.DisplayMessageInScene(m_IsSinglePatientScene, "Triangle pencil. ", 1f, 120, 50);
            //});
            //m_cylinderTriErasingButton.onClick.AddListener(() =>
            //{
            //    GetCurrentScene().set_tri_erasing_mode(TriEraser.Mode.Cylinder);
            //    UpdateUI();
            //    m_ScenesManager.DisplayMessageInScene(m_IsSinglePatientScene, "Cylinder pencil. ", 1f, 120, 50);
            //});
            //m_zoneTriErasingButton.onClick.AddListener(() =>
            //{
            //    GetCurrentScene().set_tri_erasing_mode(TriEraser.Mode.Zone);
            //    UpdateUI();
            //    m_ScenesManager.DisplayMessageInScene(m_IsSinglePatientScene, "Zone pencil. ", 1f, 120, 50);
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
            //    m_ScenesManager.SPScene.set_tri_erasing_zone_degrees(1f * degrees);
            //    m_ScenesManager.MPScene.set_tri_erasing_zone_degrees(1f * degrees);
            //});
        }
        void OnChangeScene()
        {
            m_ScenesManager.SelectedScene.ModesManager.OnChangeMode.AddListener((mode) => UpdateUI());
            m_SceneInformations.text = m_ScenesManager.SelectedScene.Name;
            UpdateUI();
        }
        /// <summary>
        /// Update the menu UI activity with the current parameters
        /// </summary>
        void UpdateUI()
        {
            /*
            HBP.Module3D.Cam.CamerasManager camerasManager = m_ScenesManager.CamerasManager;

            bool CanAddView = (camerasManager.MaximumNumberOfLines != camerasManager.GetNumberOfLines(m_ScenesManager.SelectedScene.Type));
            bool CanRemoveView = (camerasManager.GetNumberOfLines(m_ScenesManager.SelectedScene.Type) > 1);
            bool CanAddfMRI = (camerasManager.MaximumNumberOfColumns != camerasManager.GetNumberOfLines(m_ScenesManager.SelectedScene.Type));
            */
            bool CanRemovefMRI = (m_ScenesManager.SelectedScene.ColumnManager.ColumnsFMRI.Count > 0);
            bool CanUseMarsAtlas = m_ScenesManager.SelectedScene.SceneInformation.WhiteMeshesAvailables && m_ScenesManager.SelectedScene.SceneInformation.MarsAtlasParcelsLoaed;
            //bool CcepVisible = m_IsSinglePatientScene && !m_ScenesManager.SPScene.is_latency_mode_enabled();
            //bool iEegVisible = (!CcepVisible && m_IsSinglePatientScene);
            //bool isTriErasingEnabled = scene.is_tri_erasing_enabled();

            switch (m_ScenesManager.SelectedScene.ModesManager.CurrentMode.ID)
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
                    SetToggleState(m_MarsAtlas, true, true);
                    //SetButtonState(m_AddView, true, CanAddView);
                    //SetButtonState(m_RemoveView, true, CanRemoveView);               
                    //SetButtonState(m_AddfMRI, true, CanAddfMRI);
                    SetButtonState(m_RemovefMRI, true, CanRemovefMRI);
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
                    SetToggleState(m_MarsAtlas, true, CanUseMarsAtlas);
                    //SetButtonState(m_AddView, true, CanAddView);
                    //SetButtonState(m_RemoveView, true, CanRemoveView);
                    //SetButtonState(m_AddfMRI, true, CanAddfMRI);
                    SetButtonState(m_RemovefMRI, true, CanRemovefMRI);
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
                    //SetButtonState(m_AddView, true, CanAddView);
                    //SetButtonState(m_RemoveView, true, CanRemoveView);
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
                    SetToggleState(m_MarsAtlas, true, CanUseMarsAtlas);
                    //SetButtonState(m_AddView, true, CanAddView);
                    //SetButtonState(m_RemoveView, true, CanRemoveView);
                    //SetButtonState(m_AddfMRI, true, CanAddfMRI);
                    SetButtonState(m_RemovefMRI, true, CanRemovefMRI);
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
                    SetToggleState(m_MarsAtlas, true, CanUseMarsAtlas);
                    //SetButtonState(m_AddView, true, CanAddView);
                    //SetButtonState(m_RemoveView, true, CanRemoveView);
                    //SetButtonState(m_AddfMRI, true, CanAddfMRI);
                    SetButtonState(m_RemovefMRI, true, CanRemovefMRI);
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
                    Debug.LogError("Error : setUIActivity :" + m_ScenesManager.SelectedScene.ModesManager.CurrentMode.ID.ToString());
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