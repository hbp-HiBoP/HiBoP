/**
 * \file    PlanesController.cs
 * \author  Lance Florian - Adrien Gannerie
 * \date    2015 - 2017
 * \brief   Define PlanesController class
 */
using UnityEngine;
using UnityEngine.UI;
using HBP.Module3D.Cam;
using System.Globalization;
using System.Collections.Generic;

namespace HBP.Module3D
{
    /// <summary>
    /// Controller for the planes cut UI overlay
    /// </summary>
    public class PlanesController : IndividualSceneOverlayController
    {
        #region Properties
        [SerializeField, Candlelight.PropertyBackingField]
        private int m_MaxPlaneNumber = 5; /**< maximum number of plane cuts at the same time */
        public int MaxPlaneNumber
        {
            get { return m_MaxPlaneNumber; }
            set { m_MaxPlaneNumber = value; }
        }

        bool m_SetPlanePanelIsOpened = false; /**< is the set plane panel opened ? */
        int m_CurrentPlaneActived = 0; /**< current plane panel id active */
        int m_CurrentPlaneNumber = 0; /**< current number of plane cuts */
        
        Transform m_SetPlaneListCanvasOverlayTransform;
        Transform m_PlanesControlCanvasOverlayTransform;

        UIOverlay m_PlanesListSceneOverlay = new UIOverlay(); /**< planes lists scene overlay */
        UIOverlay m_PlanesControlSceneOverlay = new UIOverlay(); /**< planes control scene overlay */
        GameObject m_ChoosePlanePanel; /**< loaded prefab */
        List<GameObject> m_SetPlanePanelList = new List<GameObject>(); /**<  list of prefab instances */
        #endregion
            

        #region Public Methods
        /// <summary>
        /// Init the controller
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="camerasManager"></param>
        public new void Initialize(Base3DScene scene, CamerasManager camerasManager)
        {
            base.Initialize(scene, camerasManager);

            // associate transform canvas overlays
            GameObject canvaOverlay1 = new GameObject("set plane");
            canvaOverlay1.transform.SetParent(transform);

            GameObject canvaOverlay2 = new GameObject("choose plane");
            canvaOverlay2.transform.SetParent(transform);

            m_SetPlaneListCanvasOverlayTransform = canvaOverlay1.transform;
            m_PlanesControlCanvasOverlayTransform = canvaOverlay2.transform;

            // set scenes overlay transfoms
            m_PlanesListSceneOverlay.mainUITransform = m_SetPlaneListCanvasOverlayTransform;
            m_PlanesControlSceneOverlay.mainUITransform = m_PlanesControlCanvasOverlayTransform;

            // choose plane
            m_ChoosePlanePanel = Instantiate(GlobalGOPreloaded.ChoosePlanePanel);
            m_ChoosePlanePanel.name = "choosePlane panel overlay";
            m_ChoosePlanePanel.transform.SetParent(m_PlanesControlCanvasOverlayTransform, false);
            m_ChoosePlanePanel.SetActive(true);

            // choose plane 
            Button addPlaneButton = m_ChoosePlanePanel.transform.FindChild("choosePlane_addRemovePlane_layout").FindChild("choosePlane_addPlane_button").GetComponent<Button>();
            addPlaneButton.onClick.AddListener(delegate { if (m_CurrentPlaneNumber < m_MaxPlaneNumber) { add_and_init_plane(); } });

            Button removePlaneButton = m_ChoosePlanePanel.transform.FindChild("choosePlane_addRemovePlane_layout").FindChild("choosePlane_removePlane_button").GetComponent<Button>();
            removePlaneButton.onClick.AddListener(delegate { if (m_CurrentPlaneNumber > 0) { removePlane(); } });

            // updates planes after reloading volume 
            m_Scene.UpdatePlanes.AddListener(
                    delegate { updateAllSceneCutPlanes(); }
                );

            // add 3 planes
            //add_and_init_plane();
            //add_and_init_plane();
            //add_and_init_plane();
        }

        /// <summary>
        /// Update the UI with the current mode and activity
        /// </summary>
        public override void UpdateUI()
        {
            bool activity = m_CurrentActivity && m_IsVisibleFromScene && m_IsEnoughtRoom;

            if(m_CurrentMode != null)
                if (m_CurrentMode.IDMode == Mode.ModesId.NoPathDefined || m_CurrentMode.IDMode == Mode.ModesId.Error)
                {
                    activity = false;
                }

            // set activity
            m_PlanesListSceneOverlay.setActivity(activity);
            m_PlanesControlSceneOverlay.setActivity(activity);
        }

        /// <summary>
        /// Update the position of the UI in the scene
        /// </summary>
        public override void UpdatePosition()
        {
            RectTransform rectTransform;
            Rect rectCamera = m_CamerasManager.GetSceneRectTransform(m_Scene.Type).rect;

            // planes choose
            rectTransform = m_ChoosePlanePanel.GetComponent<RectTransform>();
            rectTransform.position = rectCamera.position + new Vector2(0, 36);

            for (int ii = 0; ii < m_CurrentPlaneNumber; ++ii)
            {
                Button choosePlaneButton = m_ChoosePlanePanel.transform.Find("choosePlane_selectPlane" + (ii) + "_button").GetComponent<Button>();
                ColorBlock cb = choosePlaneButton.colors;
                if (ii == m_CurrentPlaneActived && m_SetPlanePanelIsOpened)
                {
                    cb.normalColor = Color.green;
                    cb.highlightedColor = Color.green;
                    choosePlaneButton.transform.Find("Text").GetComponent<Text>().color = Color.black;
                }
                else
                {
                    Color unselectCol = new Color(56f / 255, 56f / 255, 56f / 255);
                    cb.normalColor = unselectCol;
                    cb.highlightedColor = unselectCol;
                    choosePlaneButton.transform.Find("Text").GetComponent<Text>().color = Color.white;
                }

                choosePlaneButton.colors = cb;
            }

            // planes set
            for (int ii = 0; ii < m_SetPlanePanelList.Count; ++ii)
            {
                if (ii == m_CurrentPlaneActived && m_SetPlanePanelIsOpened)
                {
                    m_SetPlanePanelList[ii].SetActive(true);
                    rectTransform = m_SetPlanePanelList[ii].GetComponent<RectTransform>();
                    rectTransform.position = rectCamera.position + new Vector2(47, 36);
                }
                else
                {
                    m_SetPlanePanelList[ii].SetActive(false);
                }
            }

            // check if enought room
            bool previous = m_IsEnoughtRoom;
            m_IsEnoughtRoom = (rectCamera.width > 330) && (rectCamera.height > 250);
            if (previous != m_IsEnoughtRoom)
            {
                UpdateUI();
            }
        }

        /// <summary>
        /// Return the RectT of the Planes choose panel
        /// </summary>
        /// <returns></returns>
        public RectTransform getPlanesChooseRectT()
        {
            return m_ChoosePlanePanel.GetComponent<RectTransform>();
        }

        /// <summary>
        /// Return the Rect of the Planes set panel
        /// </summary>
        /// <returns></returns>
        public RectTransform getSetPlanesRectT()
        {
            if (m_SetPlanePanelIsOpened && m_CurrentPlaneNumber > m_CurrentPlaneActived)
                return m_SetPlanePanelList[m_CurrentPlaneActived].GetComponent<RectTransform>();

            return null;
        }        

        /// <summary>
        /// Add a plane, and depending on the number of plances, init it with predefined parameters
        /// </summary>
        private void add_and_init_plane()
        {            
            add_plane();
            int currentPlanesNb = m_SetPlanePanelList.Count;

            if (currentPlanesNb == 1)
            {
                initPlane(0, 0.5f, 0, false, false, new Vector3(1, 0, 0));
            }
            else if(currentPlanesNb == 2)
            {
                initPlane(1, 0.5f, 1, false, false, new Vector3(1, 0, 0));
            }
            else if (currentPlanesNb == 3)
            {
                initPlane(2, 0.5f, 2, false, false, new Vector3(1, 0, 0));
            }
            else if (currentPlanesNb == 4)
            {
                initPlane(3, 0.5f, 3, false, false, new Vector3(1, 1, 0));
            }
            else
            {
                initPlane(4, 0.5f, 3, false, false, new Vector3(1, 5, 1));
            }
        }

        /// <summary>
        /// Initialize a plane with the input parameters
        /// </summary>
        /// <param name="idSetPlane"></param>
        /// <param name="position"></param>
        /// <param name="orientationId"></param>
        /// <param name="flip"></param>
        /// <param name="removeFrontPlane"></param>
        /// <param name="customPlane"></param>
        private void initPlane(int idSetPlane, float position, int orientationId, bool flip, bool removeFrontPlane, Vector3 customPlane)
        {
            m_SetPlanePanelList[idSetPlane].transform.Find("setplane bottom layout").Find("setplane_position_slider").GetComponent<Slider>().value = position;

            bool axial = false, sagital = false, coronal = false, custom = false;
            if (orientationId == 0)
                axial = true;
            else if (orientationId == 1)
                sagital = true;
            else if (orientationId == 2)
                coronal = true;
            else
                custom = true;

            m_SetPlanePanelList[idSetPlane].transform.Find("setplane_middle1_layout").Find("setplane_axial_radiobutton").GetComponent<UnityEngine.UI.Toggle>().isOn = axial;
            m_SetPlanePanelList[idSetPlane].transform.Find("setplane_middle1_layout").Find("setplane_sagital_radiobutton").GetComponent<UnityEngine.UI.Toggle>().isOn = sagital;
            m_SetPlanePanelList[idSetPlane].transform.Find("setplane_middle1_layout").Find("setplane_coronal_radiobutton").GetComponent<UnityEngine.UI.Toggle>().isOn = coronal;
            m_SetPlanePanelList[idSetPlane].transform.Find("setplane_middle1_layout").Find("setplane_flip_toggle").GetComponent<UnityEngine.UI.Toggle>().isOn = flip;
            m_SetPlanePanelList[idSetPlane].transform.Find("setplane_middle2_layout").Find("setplane_custom_radiobutton").GetComponent<UnityEngine.UI.Toggle>().isOn = custom;
            m_SetPlanePanelList[idSetPlane].transform.Find("setplane_middle2_layout").Find("setplane_x_inputfield").GetComponent<InputField>().text = customPlane.x.ToString();
            m_SetPlanePanelList[idSetPlane].transform.Find("setplane_middle2_layout").Find("setplane_y_inputfield").GetComponent<InputField>().text = customPlane.y.ToString();
            m_SetPlanePanelList[idSetPlane].transform.Find("setplane_middle2_layout").Find("setplane_z_inputfield").GetComponent<InputField>().text = customPlane.z.ToString();
            //setPlanePanelList[idSetPlane].transform.Find("setplane_bottom_layout").Find("setplane_removeFrontPlane_toggle").GetComponent<UnityEngine.UI.Toggle>().isOn = removeFrontPlane;
        }

        /// <summary>
        /// Open/Close the choose plane panel corresponding to the input id
        /// </summary>
        /// <param name="buttonId"></param>
        private void toggleChoosePlane(int buttonId)
        {
            if (buttonId == m_CurrentPlaneActived)
            {
                if (m_SetPlanePanelIsOpened)
                {
                    m_SetPlanePanelIsOpened = false;
                }
                else
                {
                    m_SetPlanePanelIsOpened = true;
                }
            }
            else
            {
                m_SetPlanePanelIsOpened = true;
                m_CurrentPlaneActived = buttonId;
            }
        }

        /// <summary>
        /// Update all the scene planes with the UI
        /// </summary>
        private void updateAllSceneCutPlanes()
        {
            for (int ii = 0; ii < m_SetPlanePanelList.Count; ++ii)
            {
                update_plane(ii);
            }
        }

        /// <summary>
        /// Update the scene planes corresponding to the input id with the UI
        /// </summary>
        /// <param name="idPlane"></param>
        private void update_plane(int idPlane)
        {
            float position = m_SetPlanePanelList[idPlane].transform.Find("setplane bottom layout").Find("setplane_position_slider").GetComponent<Slider>().value;
            bool sagital = m_SetPlanePanelList[idPlane].transform.Find("setplane_middle1_layout").Find("setplane_sagital_radiobutton").GetComponent<UnityEngine.UI.Toggle>().isOn;
            bool coronal = m_SetPlanePanelList[idPlane].transform.Find("setplane_middle1_layout").Find("setplane_coronal_radiobutton").GetComponent<UnityEngine.UI.Toggle>().isOn;
            bool flip = m_SetPlanePanelList[idPlane].transform.Find("setplane_middle1_layout").Find("setplane_flip_toggle").GetComponent<UnityEngine.UI.Toggle>().isOn;
            bool custom = m_SetPlanePanelList[idPlane].transform.Find("setplane_middle2_layout").Find("setplane_custom_radiobutton").GetComponent<UnityEngine.UI.Toggle>().isOn;
            string valueX = m_SetPlanePanelList[idPlane].transform.Find("setplane_middle2_layout").Find("setplane_x_inputfield").GetComponent<InputField>().text;
            string valueY = m_SetPlanePanelList[idPlane].transform.Find("setplane_middle2_layout").Find("setplane_y_inputfield").GetComponent<InputField>().text;
            string valueZ = m_SetPlanePanelList[idPlane].transform.Find("setplane_middle2_layout").Find("setplane_z_inputfield").GetComponent<InputField>().text;
            bool removeFrontPlane = false;// setPlanePanelList[idPlane].transform.Find("setplane_bottom_layout").Find("setplane_removeFrontPlane_toggle").GetComponent<UnityEngine.UI.Toggle>().isOn;

            int idOrientation = 0;
            if (sagital)
                idOrientation = 2;
            else if (coronal)
                idOrientation = 1;
            else if (custom)
                idOrientation = 3;

            float x = float.Parse(valueX, CultureInfo.InvariantCulture.NumberFormat);
            float y = float.Parse(valueY, CultureInfo.InvariantCulture.NumberFormat);
            float z = float.Parse(valueZ, CultureInfo.InvariantCulture.NumberFormat);

            m_Scene.UpdateCutPlane(idOrientation, flip, removeFrontPlane, new Vector3(x, y, z), idPlane, position);
        }

        /// <summary>
        /// Add a new plane in the UI
        /// </summary>
        private void add_plane()
        {
            // add plane button
            GameObject newPlaneButton = Instantiate(m_ChoosePlanePanel.transform.FindChild("choosePlane_selectPlaneBase_button").gameObject);
            newPlaneButton.SetActive(true);
            newPlaneButton.transform.SetParent(m_ChoosePlanePanel.transform, false);
            newPlaneButton.transform.FindChild("Text").gameObject.GetComponent<Text>().text = "Cut " + (m_CurrentPlaneNumber +1);
            newPlaneButton.name = "choosePlane_selectPlane" + m_CurrentPlaneNumber + "_button";

            // raise ui position
            m_ChoosePlanePanel.GetComponent<RectTransform>().sizeDelta =
                new Vector2(m_ChoosePlanePanel.GetComponent<RectTransform>().rect.width, m_ChoosePlanePanel.GetComponent<RectTransform>().rect.height + 25);

            // add toggle choose plane event when clicking on the new button
            int idButton = m_CurrentPlaneNumber;
            newPlaneButton.GetComponent<Button>().onClick.AddListener(delegate
            {
                toggleChoosePlane(idButton);
            });

            // add a new setPlane panel corresponding to the new plane
            m_SetPlanePanelList.Add(Instantiate(GlobalGOPreloaded.SetPlanePanel));
            int idSetPlane = m_SetPlanePanelList.Count - 1;
            m_SetPlanePanelList[idSetPlane].transform.SetParent(m_SetPlaneListCanvasOverlayTransform);
            
            m_SetPlanePanelList[idSetPlane].name = "Set plane panel overlay " + m_CurrentPlaneNumber;

            // add setPlanes listeners
            m_SetPlanePanelList[idSetPlane].transform.Find("setplane bottom layout").Find("setplane_position_slider").GetComponent<Slider>().onValueChanged.AddListener(
                delegate { update_plane(idSetPlane); }
            );
            m_SetPlanePanelList[idSetPlane].transform.Find("setplane_middle1_layout").Find("setplane_axial_radiobutton").GetComponent<UnityEngine.UI.Toggle>().onValueChanged.AddListener(
                delegate { update_plane(idSetPlane); }
            );
            m_SetPlanePanelList[idSetPlane].transform.Find("setplane_middle1_layout").Find("setplane_sagital_radiobutton").GetComponent<UnityEngine.UI.Toggle>().onValueChanged.AddListener(
                delegate { update_plane(idSetPlane); }
            );
            m_SetPlanePanelList[idSetPlane].transform.Find("setplane_middle1_layout").Find("setplane_coronal_radiobutton").GetComponent<UnityEngine.UI.Toggle>().onValueChanged.AddListener(
                delegate { update_plane(idSetPlane); }
            );
            m_SetPlanePanelList[idSetPlane].transform.Find("setplane_middle1_layout").Find("setplane_flip_toggle").GetComponent<UnityEngine.UI.Toggle>().onValueChanged.AddListener(
                delegate { update_plane(idSetPlane); }
            );
            m_SetPlanePanelList[idSetPlane].transform.Find("setplane_middle2_layout").Find("setplane_custom_radiobutton").GetComponent<UnityEngine.UI.Toggle>().onValueChanged.AddListener(
                delegate { update_plane(idSetPlane); }
            );
            m_SetPlanePanelList[idSetPlane].transform.Find("setplane_middle2_layout").Find("setplane_x_inputfield").GetComponent<InputField>().onEndEdit.AddListener(
                delegate { update_plane(idSetPlane); }
            );
            m_SetPlanePanelList[idSetPlane].transform.Find("setplane_middle2_layout").Find("setplane_y_inputfield").GetComponent<InputField>().onEndEdit.AddListener(
                delegate { update_plane(idSetPlane); }
            );
            m_SetPlanePanelList[idSetPlane].transform.Find("setplane_middle2_layout").Find("setplane_z_inputfield").GetComponent<InputField>().onEndEdit.AddListener(
                delegate { update_plane(idSetPlane); }
            );
            //setPlanePanelList[idSetPlane].transform.Find("setplane_bottom_layout").Find("setplane_removeFrontPlane_toggle").GetComponent<UnityEngine.UI.Toggle>().onValueChanged.AddListener(
            //    delegate { updatePlane(idSetPlane); }
            //);

            // add new plane and update the scene
            ++m_CurrentPlaneNumber;
            m_Scene.AddCutPlane();
            update_plane(idSetPlane);
        }

        /// <summary>
        /// Remove the last plane of the UI
        /// </summary>
        private void removePlane()
        {
            // destroy plane button
            Destroy(m_ChoosePlanePanel.transform.FindChild("choosePlane_selectPlane" + (m_CurrentPlaneNumber - 1) + "_button").gameObject);

            // down ui
            m_ChoosePlanePanel.GetComponent<RectTransform>().sizeDelta = new Vector2(m_ChoosePlanePanel.GetComponent<RectTransform>().rect.width, m_ChoosePlanePanel.GetComponent<RectTransform>().rect.height - 25);

            // destroy setPlane panel
            Destroy(m_SetPlanePanelList[m_SetPlanePanelList.Count - 1]);
            m_SetPlanePanelList.RemoveAt(m_SetPlanePanelList.Count - 1);

            // remove last plane and update the scene
            m_Scene.RemoveLastCutPlane();
            --m_CurrentPlaneNumber;            
        }


        #endregion
    }
}