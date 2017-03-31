
/**
 * \file    TopMenuController.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define TopMenuController class
 */

// system
using System.Collections.Generic;

// unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace HBP.VISU3D
{
    /// <summary>
    /// A class for managing the top menu in the UI
    /// </summary>
    public class TopMenuController : MonoBehaviour, UICameraOverlay
    {
        #region members

        string nameSPScene = "";
        
        private bool m_isCurrentSceneSp;
        private Mode m_currentMode = null;

        List<string> m_spColumnsNames = new List<string>();
        List<string> m_mpColumnsNames = new List<string>();

        private ScenesManager m_scenesM = null;        

        // UI
        public Transform m_topPanelMenu;  /**< top panel of the menu transform */

        Button m_left = null;
        Button m_right = null;
        Button m_both= null;

        Button m_hemi = null;
        Button m_white = null;
        Button m_inflated = null;

        Button m_mars = null;
        Button m_removeMars = null;

        Button m_addView = null;
        Button m_removeView = null;

        Button m_iEegButton = null;
        Button m_CcepButton = null;

        Button m_addFMriButton = null;
        Button m_removeLastFMriButton = null;

        Button m_enableTriErasingButton = null;
        Button m_disableTriErasingButton = null;
        Button m_resetTriErasingButton = null;
        Button m_cancelTriErasingButton = null;
        Button m_pointTriErasingButton = null;
        Button m_zoneTriErasingButton = null;
        Button m_invertTriErasingButton = null;
        Button m_expandTriErasingButton = null;
        Button m_cylinderTriErasingButton = null;

        InputField m_zoneDegreesInputField = null;
        
        #endregion members

        #region functions

        /// <summary>
        /// Init the controller
        /// </summary>
        /// <param name="scenesManager"></param>
        public void init(ScenesManager scenesManager)
        {
            // retrieve scenes
            m_scenesM = scenesManager;
 
            // retrieve buttons
            Transform baseTransfo = m_topPanelMenu.Find("left part").Find("buttons");
            m_left = baseTransfo.Find("brain left button").GetComponent<Button>();
            m_right = baseTransfo.Find("brain right button").GetComponent<Button>();
            m_both = baseTransfo.Find("brain both button").GetComponent<Button>();

            m_hemi = baseTransfo.Find("brain hemi button").GetComponent<Button>();
            m_white = baseTransfo.Find("brain white button").GetComponent<Button>();
            m_inflated = baseTransfo.Find("brain inflated button").GetComponent<Button>();

            m_mars = baseTransfo.Find("mars parcels button").GetComponent<Button>();
            m_removeMars = baseTransfo.Find("remove mars parcels button").GetComponent<Button>();

            m_removeView = baseTransfo.Find("remove view button").GetComponent<Button>();
            m_addView = baseTransfo.Find("add view button").GetComponent<Button>();

            m_iEegButton = baseTransfo.Find("iEEG button").GetComponent<Button>();
            m_CcepButton = baseTransfo.Find("CCEP button").GetComponent<Button>();

            m_addFMriButton = baseTransfo.Find("add fMRI button").GetComponent<Button>();
            m_removeLastFMriButton = baseTransfo.Find("remove fMRI button").GetComponent<Button>();

            m_enableTriErasingButton = baseTransfo.Find("tri erasing button").GetComponent<Button>();
            m_disableTriErasingButton = baseTransfo.Find("escape tri erasing button").GetComponent<Button>();
            m_resetTriErasingButton = baseTransfo.Find("reset tri erasing button").GetComponent<Button>();
            m_cancelTriErasingButton = baseTransfo.Find("cancel tri erasing button").GetComponent<Button>();
            m_pointTriErasingButton = baseTransfo.Find("onePoint tri erasing button").GetComponent<Button>();
            m_cylinderTriErasingButton = baseTransfo.Find("cylinder tri erasing button").GetComponent<Button>();
            m_zoneTriErasingButton = baseTransfo.Find("zone tri erasing button").GetComponent<Button>();
            m_invertTriErasingButton = baseTransfo.Find("invert tri erasing button").GetComponent<Button>();
            m_expandTriErasingButton = baseTransfo.Find("expand tri erasing button").GetComponent<Button>();
            m_zoneDegreesInputField = baseTransfo.Find("zone degree input field").GetComponent<InputField>();
            
            // set listeners
            m_addView.onClick.AddListener(() =>
            {
                m_scenesM.CamerasManager.add_view_line_cameras(m_isCurrentSceneSp);
                update_UI();
            });
            m_removeView.onClick.AddListener(() =>
            {
                m_scenesM.CamerasManager.remove_view_line_cameras(m_isCurrentSceneSp);
                update_UI();
            });
            
            m_hemi.onClick.AddListener(() =>
            {
                if (current_scene().data_.marsAtlasMode)
                    current_scene().switch_mars_atlas_color();

                current_scene().update_mesh_type_to_display(SceneStatesInfo.MeshType.Hemi);
            });
            m_white.onClick.AddListener(() =>
            {
                current_scene().update_mesh_type_to_display(SceneStatesInfo.MeshType.White);
            });
            m_inflated.onClick.AddListener(() =>
            {
                m_scenesM.MPScene.update_mesh_type_to_display(SceneStatesInfo.MeshType.Inflated);
            });

            m_left.onClick.AddListener(() =>
            {
                current_scene().update_mesh_part_to_display(SceneStatesInfo.MeshPart.Left);
            });
            m_right.onClick.AddListener(() =>
            {
                current_scene().update_mesh_part_to_display(SceneStatesInfo.MeshPart.Right);
            });
            m_both.onClick.AddListener(() =>
            {
                current_scene().update_mesh_part_to_display(SceneStatesInfo.MeshPart.Both);
            });

            m_mars.onClick.AddListener(() =>
            {
                current_scene().update_mesh_type_to_display(SceneStatesInfo.MeshType.White);
                current_scene().switch_mars_atlas_color();
                update_UI();
            });
            m_removeMars.onClick.AddListener(() =>
            {
                current_scene().switch_mars_atlas_color();
                update_UI();
            });

            m_iEegButton.onClick.AddListener(() =>
            {
                m_scenesM.SPScene.set_CCEP_display_mode(false);
                update_UI();
            });
            m_CcepButton.onClick.AddListener(() =>
            {
                m_scenesM.SPScene.set_CCEP_display_mode(true);
                update_UI();
            });

            m_addFMriButton.onClick.AddListener(() =>
            {
                if(StaticComponents.HBPMain.add_fMRI_column(m_isCurrentSceneSp))
                    m_scenesM.display_message_in_scene(m_isCurrentSceneSp, "fMRI successfully loaded. ", 2f, 150, 80);
                else
                    m_scenesM.display_message_in_scene(m_isCurrentSceneSp, "Error during fMRI loading. ", 2f, 170, 80);

                update_UI();
            });
            m_removeLastFMriButton.onClick.AddListener(() =>
            {
                StaticComponents.HBPMain.remove_last_fMRI_column(m_isCurrentSceneSp);
                update_UI();
            });

            m_enableTriErasingButton.onClick.AddListener(() =>
            {
                current_scene().set_tri_erasing(true);
                update_UI();
                m_scenesM.display_message_in_scene(m_isCurrentSceneSp, "Triangles erasing mode enabled. ", 2f, 200, 80);
            });
            m_disableTriErasingButton.onClick.AddListener(() =>
            {
                current_scene().set_tri_erasing(false);
                update_UI();
                m_scenesM.display_message_in_scene(m_isCurrentSceneSp, "Triangles erasing mode disabled. ", 2f, 200, 80);
            });
            m_resetTriErasingButton.onClick.AddListener(() =>
            {
                current_scene().reset_tri_erasing();
                update_UI();
            });
            m_cancelTriErasingButton.onClick.AddListener(() =>
            {
                current_scene().cancel_last_tri_erasing();
                update_UI();
            });
            m_pointTriErasingButton.onClick.AddListener(() =>
            {
                current_scene().set_tri_erasing_mode(TriEraser.Mode.OneTri);
                update_UI();
                m_scenesM.display_message_in_scene(m_isCurrentSceneSp, "Triangle pencil. ", 1f, 120, 50);
            });
            m_cylinderTriErasingButton.onClick.AddListener(() =>
            {
                current_scene().set_tri_erasing_mode(TriEraser.Mode.Cylinder);
                update_UI();
                m_scenesM.display_message_in_scene(m_isCurrentSceneSp, "Cylinder pencil. ", 1f, 120, 50);
            });
            m_zoneTriErasingButton.onClick.AddListener(() =>
            {
                current_scene().set_tri_erasing_mode(TriEraser.Mode.Zone);
                update_UI();
                m_scenesM.display_message_in_scene(m_isCurrentSceneSp, "Zone pencil. ", 1f, 120, 50);
            });
            m_invertTriErasingButton.onClick.AddListener(() =>
            {
                current_scene().set_tri_erasing_mode(TriEraser.Mode.Invert);
                update_UI();
            });
            m_expandTriErasingButton.onClick.AddListener(() =>
            {
                current_scene().set_tri_erasing_mode(TriEraser.Mode.Expand);                
                update_UI();
            });

            m_zoneDegreesInputField.onEndEdit.AddListener((value) =>
            {
                int degrees = 0;
                if (value.Length > 0)
                {
                    degrees = int.Parse(value);
                    if (degrees > 180)
                        degrees = 180;
                    if (degrees < 0)
                        degrees = 0;
                }

                m_zoneDegreesInputField.text = "" + degrees;
                m_scenesM.SPScene.set_tri_erasing_zone_degrees(1f * degrees);
                m_scenesM.MPScene.set_tri_erasing_zone_degrees(1f * degrees);
            });
        }

        /// <summary>
        /// Return the current active scene
        /// </summary>
        /// <returns></returns>
        public Base3DScene current_scene()
        {
            return (m_isCurrentSceneSp ? (Base3DScene)m_scenesM.SPScene : m_scenesM.MPScene);
        }

        /// <summary>
        /// Update top left information
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="idColumn"></param>
        public void update_current_scene_and_column(string nameScene, bool spScene, int idColumn)
        {
            m_isCurrentSceneSp = spScene;

            if (nameScene.Length > 0 && spScene)
                nameSPScene = nameScene;

            Transform sceneNameTxt = m_topPanelMenu.Find("left part").Find("scene name panel");
            
            sceneNameTxt.Find("Text").GetComponent<Text>().text = spScene ? nameSPScene : "MNI";
            int sizeText = sceneNameTxt.Find("Text").GetComponent<Text>().text.Length * 10 + 20;

            sceneNameTxt.gameObject.GetComponent<LayoutElement>().minWidth = sizeText;
            Rect rectText = sceneNameTxt.Find("Text").GetComponent<RectTransform>().rect;
            sceneNameTxt.Find("Text").GetComponent<RectTransform>().sizeDelta = new Vector2(sizeText, rectText.height);

            m_topPanelMenu.Find("left part").Find("column num panel").Find("Text").GetComponent<Text>().text = "Column " + (idColumn+1);
            rectText = m_topPanelMenu.Find("left part").Find("column num panel").Find("Text").GetComponent<RectTransform>().rect;

            string colTxt = "..."; 
            if (idColumn != -1)
                colTxt = spScene ? (idColumn < m_spColumnsNames.Count ? m_spColumnsNames[idColumn] : "...") : (idColumn < m_mpColumnsNames.Count ? m_mpColumnsNames[idColumn] : "...");
            sizeText = colTxt.Length * 10 + 20;

            m_topPanelMenu.Find("left part").Find("column name panel").gameObject.GetComponent<LayoutElement>().minWidth = sizeText;
            m_topPanelMenu.Find("left part").Find("column name panel").Find("Text").GetComponent<RectTransform>().sizeDelta = new Vector2(sizeText, rectText.height);
            m_topPanelMenu.Find("left part").Find("column name panel").Find("Text").GetComponent<Text>().text = colTxt;

            update_UI();
        }

        /// <summary>
        /// Update the UI components states with the current mode
        /// </summary>
        /// <param name="mode"></param>
        public void update_UI_with_mode(Mode mode)
        {
            m_currentMode = mode;
            update_UI();
        }

        /// <summary>
        /// Define the state and the interaction of a button
        /// </summary>
        /// <param name="button"></param>
        /// <param name="active"></param>
        /// <param name="interactable"></param>
        private void set_state_button(Button button, bool active, bool interactable)
        {
            button.interactable = interactable;
            button.gameObject.SetActive(active);
        }

        /// <summary>
        /// Update the menu UI activity with the current parameters
        /// </summary>
        void update_UI()
        {
            Base3DScene scene = current_scene();
            Cam.CamerasManager camerasM = m_scenesM.CamerasManager;

            bool addViewInter    = (camerasM.max_views_line() != camerasM.views_nb(m_isCurrentSceneSp));
            bool removeViewInter = (camerasM.views_nb(m_isCurrentSceneSp) > 1);

            bool addFMriInter    = (camerasM.max_conditions_col() != camerasM.columns_nb(m_isCurrentSceneSp));            
            bool removeFMriInter = (scene.FMRI_columns_nb() > 0);

            bool CcepVisible = m_isCurrentSceneSp && !m_scenesM.SPScene.is_latency_mode_enabled();
            bool iEegVisible = (!CcepVisible && m_isCurrentSceneSp);

            bool marsVisible = scene.data_.whiteMeshesAvailables && scene.data_.marsAtlasParcelsLoaed && !scene.data_.marsAtlasMode;
            bool removeMarsVisible = scene.data_.whiteMeshesAvailables && scene.data_.marsAtlasParcelsLoaed && scene.data_.marsAtlasMode;

            bool isTriErasingEnabled = scene.is_tri_erasing_enabled();
            
            switch (m_currentMode.m_idMode)
            {
                case Mode.ModesId.NoPathDefined:
                    set_state_button(m_addView, true, false);
                    set_state_button(m_removeView, true, false);

                    set_state_button(m_left, true, false);
                    set_state_button(m_right, true, false);
                    set_state_button(m_both, true, false);

                    set_state_button(m_hemi, true, false);
                    set_state_button(m_white, true, false);
                    set_state_button(m_inflated, !m_isCurrentSceneSp, false);

                    set_state_button(m_iEegButton, iEegVisible, false);
                    set_state_button(m_CcepButton, CcepVisible, false);

                    set_state_button(m_addFMriButton, true, false);
                    set_state_button(m_removeLastFMriButton, true, false);

                    set_state_button(m_mars, true, false);
                    set_state_button(m_removeMars, false, false);

                    set_state_button(m_enableTriErasingButton, true, false);
                    set_state_button(m_disableTriErasingButton, false, false);
                    set_state_button(m_resetTriErasingButton, false, false);
                    set_state_button(m_cancelTriErasingButton, false, false);
                    set_state_button(m_pointTriErasingButton, false, false);
                    set_state_button(m_cylinderTriErasingButton, false, false);
                    set_state_button(m_zoneTriErasingButton, false, false);
                    set_state_button(m_invertTriErasingButton, false, false);
                    set_state_button(m_expandTriErasingButton, false, false);
                    m_zoneDegreesInputField.gameObject.SetActive(false);
                    m_zoneDegreesInputField.interactable = false;

                    break;
                case Mode.ModesId.MinPathDefined:
                    set_state_button(m_addView, true, addViewInter);
                    set_state_button(m_removeView, true, removeViewInter);

                    set_state_button(m_left, true, true);
                    set_state_button(m_right, true, true);
                    set_state_button(m_both, true, true);

                    set_state_button(m_hemi, true, true);
                    set_state_button(m_white, true, true);
                    set_state_button(m_inflated, !m_isCurrentSceneSp, true);

                    set_state_button(m_iEegButton, iEegVisible, true);
                    set_state_button(m_CcepButton, CcepVisible, true);

                    set_state_button(m_addFMriButton, true, addFMriInter);
                    set_state_button(m_removeLastFMriButton, true, removeFMriInter);

                    set_state_button(m_mars, !scene.data_.marsAtlasMode, marsVisible);
                    set_state_button(m_removeMars, scene.data_.marsAtlasMode, removeMarsVisible);

                    set_state_button(m_enableTriErasingButton, !isTriErasingEnabled, true);
                    set_state_button(m_disableTriErasingButton, isTriErasingEnabled, true);
                    set_state_button(m_resetTriErasingButton, isTriErasingEnabled, true);
                    set_state_button(m_cancelTriErasingButton, isTriErasingEnabled, true);
                    set_state_button(m_pointTriErasingButton, isTriErasingEnabled, true);
                    set_state_button(m_cylinderTriErasingButton, isTriErasingEnabled, true);
                    set_state_button(m_zoneTriErasingButton, isTriErasingEnabled, true);
                    set_state_button(m_invertTriErasingButton, isTriErasingEnabled, true);
                    set_state_button(m_expandTriErasingButton, isTriErasingEnabled, true);
                    m_zoneDegreesInputField.gameObject.SetActive(isTriErasingEnabled);
                    m_zoneDegreesInputField.interactable = true;

                    break;
                case Mode.ModesId.AllPathDefined:
                    set_state_button(m_addView, true, addViewInter);
                    set_state_button(m_removeView, true, removeViewInter);

                    set_state_button(m_left, true, true);
                    set_state_button(m_right, true, true);
                    set_state_button(m_both, true, true);

                    set_state_button(m_hemi, true, true);
                    set_state_button(m_white, true, true);
                    set_state_button(m_inflated, !m_isCurrentSceneSp, true);

                    set_state_button(m_iEegButton, iEegVisible, true);
                    set_state_button(m_CcepButton, CcepVisible, true);

                    set_state_button(m_addFMriButton, true, addFMriInter);
                    set_state_button(m_removeLastFMriButton, true, removeFMriInter);

                    set_state_button(m_mars, !scene.data_.marsAtlasMode, marsVisible);
                    set_state_button(m_removeMars, scene.data_.marsAtlasMode, removeMarsVisible);

                    set_state_button(m_enableTriErasingButton, !isTriErasingEnabled, true);
                    set_state_button(m_disableTriErasingButton, isTriErasingEnabled, true);
                    set_state_button(m_resetTriErasingButton, isTriErasingEnabled, true);
                    set_state_button(m_cancelTriErasingButton, isTriErasingEnabled, true);
                    set_state_button(m_pointTriErasingButton, isTriErasingEnabled, true);
                    set_state_button(m_cylinderTriErasingButton, isTriErasingEnabled, true);
                    set_state_button(m_zoneTriErasingButton, isTriErasingEnabled, true);
                    set_state_button(m_invertTriErasingButton, isTriErasingEnabled, true);
                    set_state_button(m_expandTriErasingButton, isTriErasingEnabled, true);
                    m_zoneDegreesInputField.gameObject.SetActive(isTriErasingEnabled);
                    m_zoneDegreesInputField.interactable = true;

                    break;
                case Mode.ModesId.ComputingAmplitudes:
                    set_state_button(m_addView, true, addViewInter);
                    set_state_button(m_removeView, true, removeViewInter);

                    set_state_button(m_left, true, false);
                    set_state_button(m_right, true, false);
                    set_state_button(m_both, true, false);

                    set_state_button(m_hemi, true, false);
                    set_state_button(m_white, true, false);
                    set_state_button(m_inflated, !m_isCurrentSceneSp, false);

                    set_state_button(m_iEegButton, iEegVisible, false);
                    set_state_button(m_CcepButton, CcepVisible, false);

                    set_state_button(m_addFMriButton, true, false);
                    set_state_button(m_removeLastFMriButton, true, false);

                    set_state_button(m_mars, !scene.data_.marsAtlasMode, false);
                    set_state_button(m_removeMars, scene.data_.marsAtlasMode, false);

                    set_state_button(m_enableTriErasingButton, !isTriErasingEnabled, false);
                    set_state_button(m_disableTriErasingButton, isTriErasingEnabled, false);
                    set_state_button(m_resetTriErasingButton, isTriErasingEnabled, false);
                    set_state_button(m_cancelTriErasingButton, isTriErasingEnabled, false);
                    set_state_button(m_pointTriErasingButton, isTriErasingEnabled, false);
                    set_state_button(m_cylinderTriErasingButton, isTriErasingEnabled, false);
                    set_state_button(m_zoneTriErasingButton, isTriErasingEnabled, false);
                    set_state_button(m_invertTriErasingButton, isTriErasingEnabled, false);
                    set_state_button(m_expandTriErasingButton, isTriErasingEnabled, false);
                    m_zoneDegreesInputField.gameObject.SetActive(isTriErasingEnabled);
                    m_zoneDegreesInputField.interactable = false;

                    break;
                case Mode.ModesId.AmplitudesComputed:
                    set_state_button(m_addView, true, addViewInter);
                    set_state_button(m_removeView, true, removeViewInter);

                    set_state_button(m_left, true, true);
                    set_state_button(m_right, true, true);
                    set_state_button(m_both, true, true);

                    set_state_button(m_hemi, true, true);
                    set_state_button(m_white, true, true);
                    set_state_button(m_inflated, !m_isCurrentSceneSp, true);

                    set_state_button(m_iEegButton, iEegVisible, true);
                    set_state_button(m_CcepButton, CcepVisible, true);

                    set_state_button(m_addFMriButton, true, addFMriInter);
                    set_state_button(m_removeLastFMriButton, true, removeFMriInter);

                    set_state_button(m_mars, !scene.data_.marsAtlasMode, marsVisible);
                    set_state_button(m_removeMars, scene.data_.marsAtlasMode, removeMarsVisible);

                    set_state_button(m_enableTriErasingButton, !isTriErasingEnabled, true);
                    set_state_button(m_disableTriErasingButton, isTriErasingEnabled, true);
                    set_state_button(m_resetTriErasingButton, isTriErasingEnabled, true);
                    set_state_button(m_cancelTriErasingButton, isTriErasingEnabled, true);
                    set_state_button(m_pointTriErasingButton, isTriErasingEnabled, true);
                    set_state_button(m_cylinderTriErasingButton, isTriErasingEnabled, true);
                    set_state_button(m_zoneTriErasingButton, isTriErasingEnabled, true);
                    set_state_button(m_invertTriErasingButton, isTriErasingEnabled, true);
                    set_state_button(m_expandTriErasingButton, isTriErasingEnabled, true);

                    m_zoneDegreesInputField.gameObject.SetActive(isTriErasingEnabled);
                    m_zoneDegreesInputField.interactable = true;

                    break;
                case Mode.ModesId.AmpNeedUpdate:
                    set_state_button(m_addView, true, addViewInter);
                    set_state_button(m_removeView, true, removeViewInter);

                    set_state_button(m_left, true, true);
                    set_state_button(m_right, true, true);
                    set_state_button(m_both, true, true);

                    set_state_button(m_hemi, true, true);
                    set_state_button(m_white, true, true);
                    set_state_button(m_inflated, !m_isCurrentSceneSp, true);

                    set_state_button(m_iEegButton, iEegVisible, true);
                    set_state_button(m_CcepButton, CcepVisible, true);

                    set_state_button(m_addFMriButton, true, addFMriInter);
                    set_state_button(m_removeLastFMriButton, true, removeFMriInter);

                    set_state_button(m_mars, !scene.data_.marsAtlasMode, marsVisible);
                    set_state_button(m_removeMars, scene.data_.marsAtlasMode, removeMarsVisible);

                    set_state_button(m_enableTriErasingButton, !isTriErasingEnabled, true);
                    set_state_button(m_disableTriErasingButton, isTriErasingEnabled, true);
                    set_state_button(m_resetTriErasingButton, isTriErasingEnabled, true);
                    set_state_button(m_cancelTriErasingButton, isTriErasingEnabled, true);
                    set_state_button(m_pointTriErasingButton, isTriErasingEnabled, true);
                    set_state_button(m_cylinderTriErasingButton, isTriErasingEnabled, true);
                    set_state_button(m_zoneTriErasingButton, isTriErasingEnabled, true);
                    set_state_button(m_invertTriErasingButton, isTriErasingEnabled, true);
                    set_state_button(m_expandTriErasingButton, isTriErasingEnabled, true);

                    m_zoneDegreesInputField.gameObject.SetActive(isTriErasingEnabled);
                    m_zoneDegreesInputField.interactable = true;

                    break;
                case Mode.ModesId.Error:
                    set_state_button(m_addView, true, false);
                    set_state_button(m_removeView, true, false);

                    set_state_button(m_left, true, false);
                    set_state_button(m_right, true, false);
                    set_state_button(m_both, true, false);

                    set_state_button(m_hemi, true, false);
                    set_state_button(m_white, true, false);
                    set_state_button(m_inflated, !m_isCurrentSceneSp, false);

                    set_state_button(m_iEegButton, iEegVisible, false);
                    set_state_button(m_CcepButton, CcepVisible, false);

                    set_state_button(m_addFMriButton, true, false);
                    set_state_button(m_removeLastFMriButton, true, false);

                    set_state_button(m_mars, true, false);
                    set_state_button(m_removeMars, false, false);

                    set_state_button(m_enableTriErasingButton, !isTriErasingEnabled, false);
                    set_state_button(m_disableTriErasingButton, isTriErasingEnabled, false);
                    set_state_button(m_resetTriErasingButton, isTriErasingEnabled, false);
                    set_state_button(m_cancelTriErasingButton, isTriErasingEnabled, false);
                    set_state_button(m_pointTriErasingButton, isTriErasingEnabled, false);
                    set_state_button(m_cylinderTriErasingButton, isTriErasingEnabled, false);
                    set_state_button(m_zoneTriErasingButton, isTriErasingEnabled, false);
                    set_state_button(m_invertTriErasingButton, isTriErasingEnabled, false);
                    set_state_button(m_expandTriErasingButton, isTriErasingEnabled, false);

                    m_zoneDegreesInputField.gameObject.SetActive(isTriErasingEnabled);
                    m_zoneDegreesInputField.interactable = false;

                    break;                
                default:
                    Debug.LogError("Error : setUIActivity :" + m_currentMode.m_idMode.ToString());
                    break;
            }

            switch (scene.current_tri_erasing_mode())
            {
                case TriEraser.Mode.OneTri:
                    m_pointTriErasingButton.interactable = false;
                break;
                case TriEraser.Mode.Cylinder:
                    m_cylinderTriErasingButton.interactable = false;
                break;
                case TriEraser.Mode.Zone:
                    m_zoneTriErasingButton.interactable = false;
                break;
            }

            switch (scene.data_.meshTypeToDisplay)
            {
                case SceneStatesInfo.MeshType.Hemi:
                    m_hemi.interactable = false;
                break;
                case SceneStatesInfo.MeshType.Inflated:
                    m_inflated.interactable = false;
                break;
                case SceneStatesInfo.MeshType.White:
                    m_white.interactable = false;
                break;
            }

            switch(scene.data_.meshPartToDisplay)
            {
                case SceneStatesInfo.MeshPart.Both:
                    m_both.interactable = false;
                break;
                case SceneStatesInfo.MeshPart.Left:
                    m_left.interactable = false;
                break;
                case SceneStatesInfo.MeshPart.Right:
                    m_right.interactable = false;
                break;
            }

        }

        /// <summary>
        /// Define the columns name of a scene
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="columnsData"></param>
        public void define_columns_names(bool spScene, List<HBP.Data.Visualisation.ColumnData> columnsData)
        {
            List<string> names = spScene ? m_spColumnsNames : m_mpColumnsNames;
            names.Clear();
            for (int ii = 0; ii < columnsData.Count; ++ii)
                names.Add(columnsData[ii].Label);            
        }

        /// <summary>
        /// Add a new column name in a scene
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="colName"></param>
        public void add_column_name(bool spScene, string colName)
        {
            List<string> names = spScene ? m_spColumnsNames : m_mpColumnsNames;
            names.Add(colName);
        }

        /// <summary>
        /// Remove the last added column name of a scene
        /// </summary>
        /// <param name="spScene"></param>
        public void remove_last_column_name(bool spScene)
        {
            List<string> names = spScene ? m_spColumnsNames : m_mpColumnsNames;
            names.RemoveAt(names.Count - 1);
        }

        #endregion functions
    }
}