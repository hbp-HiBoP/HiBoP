
/**
 * \file    PlanesController.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define PlanesController class
 */

// system
using System.Globalization;
using System.Collections.Generic;

// unity
using UnityEngine;
using UnityEngine.UI;

// hbp
using HBP.VISU3D.Cam;

namespace HBP.VISU3D
{
    /// <summary>
    /// Controller for the planes cut UI overlay
    /// </summary>
    public class PlanesController : IndividualSceneOverlayController
    {
        #region members

        public int maxPlaneNumber = 5; /**< maximum number of plane cuts at the same time */

        private bool setPlanePanelOpened = false; /**< is the set plane panel opened ? */
        private int currentActivePlaneId = 0; /**< current plane panel id active */
        private int currentPlaneNumber = 0; /**< current number of plane cuts */
        
        private Transform m_setPlaneListCanvasOverlayTransform;
        private Transform m_planesControlCanvasOverlayTransform;

        private UIOverlay planesListSceneOverlay = new UIOverlay(); /**< planes lists scene overlay */
        private UIOverlay planesControlSceneOverlay = new UIOverlay(); /**< planes control scene overlay */
        private GameObject m_ChoosePlanePanel; /**< loaded prefab */
        private List<GameObject> setPlanePanelList = new List<GameObject>(); /**<  list of prefab instances */

        #endregion members
            

        #region others

        /// <summary>
        /// Init the controller
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="camerasManager"></param>
        public new void init(Base3DScene scene, CamerasManager camerasManager)
        {
            base.init(scene, camerasManager);

            // associate transform canvas overlays
            GameObject canvaOverlay1 = new GameObject("set plane");
            canvaOverlay1.transform.SetParent(m_canvasOverlayParent);

            GameObject canvaOverlay2 = new GameObject("choose plane");
            canvaOverlay2.transform.SetParent(m_canvasOverlayParent);

            m_setPlaneListCanvasOverlayTransform = canvaOverlay1.transform;
            m_planesControlCanvasOverlayTransform = canvaOverlay2.transform;

            // set scenes overlay transfoms
            planesListSceneOverlay.mainUITransform = m_setPlaneListCanvasOverlayTransform;
            planesControlSceneOverlay.mainUITransform = m_planesControlCanvasOverlayTransform;

            // choose plane
            m_ChoosePlanePanel = Instantiate(GlobalGOPreloaded.ChoosePlanePanel);
            m_ChoosePlanePanel.name = "choosePlane panel overlay";
            m_ChoosePlanePanel.transform.SetParent(m_planesControlCanvasOverlayTransform, false);
            m_ChoosePlanePanel.SetActive(true);

            // choose plane 
            Button addPlaneButton = m_ChoosePlanePanel.transform.FindChild("choosePlane_addRemovePlane_layout").FindChild("choosePlane_addPlane_button").GetComponent<Button>();
            addPlaneButton.onClick.AddListener(delegate { if (currentPlaneNumber < maxPlaneNumber) { add_and_init_plane(); } });

            Button removePlaneButton = m_ChoosePlanePanel.transform.FindChild("choosePlane_addRemovePlane_layout").FindChild("choosePlane_removePlane_button").GetComponent<Button>();
            removePlaneButton.onClick.AddListener(delegate { if (currentPlaneNumber > 0) { removePlane(); } });

            // updates planes after reloading volume 
            m_scene.UpdatePlanes.AddListener(
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
        public override void update_UI()
        {
            bool activity = currentActivity && isVisibleFromScene && isEnoughtRoom;

            if(currentMode != null)
                if (currentMode.m_idMode == Mode.ModesId.NoPathDefined || currentMode.m_idMode == Mode.ModesId.Error)
                {
                    activity = false;
                }

            // set activity
            planesListSceneOverlay.setActivity(activity);
            planesControlSceneOverlay.setActivity(activity);
        }

        /// <summary>
        /// Update the position of the UI in the scene
        /// </summary>
        public override void update_UI_position()
        {
            RectTransform rectTransform;
            Rect rectCamera = CamerasManager.screen_rectangle(m_camerasManager.screen_rectangleT(m_scene.singlePatient), m_backGroundCamera);

            // planes choose
            rectTransform = m_ChoosePlanePanel.GetComponent<RectTransform>();
            rectTransform.position = rectCamera.position + new Vector2(0, 36);

            for (int ii = 0; ii < currentPlaneNumber; ++ii)
            {
                Button choosePlaneButton = m_ChoosePlanePanel.transform.Find("choosePlane_selectPlane" + (ii) + "_button").GetComponent<Button>();
                ColorBlock cb = choosePlaneButton.colors;
                if (ii == currentActivePlaneId && setPlanePanelOpened)
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
            for (int ii = 0; ii < setPlanePanelList.Count; ++ii)
            {
                if (ii == currentActivePlaneId && setPlanePanelOpened)
                {
                    setPlanePanelList[ii].SetActive(true);
                    rectTransform = setPlanePanelList[ii].GetComponent<RectTransform>();
                    rectTransform.position = rectCamera.position + new Vector2(47, 36);
                }
                else
                {
                    setPlanePanelList[ii].SetActive(false);
                }
            }

            // check if enought room
            bool previous = isEnoughtRoom;
            isEnoughtRoom = (rectCamera.width > 330) && (rectCamera.height > 250);
            if (previous != isEnoughtRoom)
            {
                update_UI();
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
            if (setPlanePanelOpened && currentPlaneNumber > currentActivePlaneId)
                return setPlanePanelList[currentActivePlaneId].GetComponent<RectTransform>();

            return null;
        }        

        /// <summary>
        /// Add a plane, and depending on the number of plances, init it with predefined parameters
        /// </summary>
        private void add_and_init_plane()
        {            
            add_plane();
            int currentPlanesNb = setPlanePanelList.Count;

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
            setPlanePanelList[idSetPlane].transform.Find("setplane bottom layout").Find("setplane_position_slider").GetComponent<Slider>().value = position;

            bool axial = false, sagital = false, coronal = false, custom = false;
            if (orientationId == 0)
                axial = true;
            else if (orientationId == 1)
                sagital = true;
            else if (orientationId == 2)
                coronal = true;
            else
                custom = true;

            setPlanePanelList[idSetPlane].transform.Find("setplane_middle1_layout").Find("setplane_axial_radiobutton").GetComponent<UnityEngine.UI.Toggle>().isOn = axial;
            setPlanePanelList[idSetPlane].transform.Find("setplane_middle1_layout").Find("setplane_sagital_radiobutton").GetComponent<UnityEngine.UI.Toggle>().isOn = sagital;
            setPlanePanelList[idSetPlane].transform.Find("setplane_middle1_layout").Find("setplane_coronal_radiobutton").GetComponent<UnityEngine.UI.Toggle>().isOn = coronal;
            setPlanePanelList[idSetPlane].transform.Find("setplane_middle1_layout").Find("setplane_flip_toggle").GetComponent<UnityEngine.UI.Toggle>().isOn = flip;
            setPlanePanelList[idSetPlane].transform.Find("setplane_middle2_layout").Find("setplane_custom_radiobutton").GetComponent<UnityEngine.UI.Toggle>().isOn = custom;
            setPlanePanelList[idSetPlane].transform.Find("setplane_middle2_layout").Find("setplane_x_inputfield").GetComponent<InputField>().text = customPlane.x.ToString();
            setPlanePanelList[idSetPlane].transform.Find("setplane_middle2_layout").Find("setplane_y_inputfield").GetComponent<InputField>().text = customPlane.y.ToString();
            setPlanePanelList[idSetPlane].transform.Find("setplane_middle2_layout").Find("setplane_z_inputfield").GetComponent<InputField>().text = customPlane.z.ToString();
            //setPlanePanelList[idSetPlane].transform.Find("setplane_bottom_layout").Find("setplane_removeFrontPlane_toggle").GetComponent<UnityEngine.UI.Toggle>().isOn = removeFrontPlane;
        }

        /// <summary>
        /// Open/Close the choose plane panel corresponding to the input id
        /// </summary>
        /// <param name="buttonId"></param>
        private void toggleChoosePlane(int buttonId)
        {
            if (buttonId == currentActivePlaneId)
            {
                if (setPlanePanelOpened)
                {
                    setPlanePanelOpened = false;
                }
                else
                {
                    setPlanePanelOpened = true;
                }
            }
            else
            {
                setPlanePanelOpened = true;
                currentActivePlaneId = buttonId;
            }
        }

        /// <summary>
        /// Update all the scene planes with the UI
        /// </summary>
        private void updateAllSceneCutPlanes()
        {
            for (int ii = 0; ii < setPlanePanelList.Count; ++ii)
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
            float position = setPlanePanelList[idPlane].transform.Find("setplane bottom layout").Find("setplane_position_slider").GetComponent<Slider>().value;
            bool sagital = setPlanePanelList[idPlane].transform.Find("setplane_middle1_layout").Find("setplane_sagital_radiobutton").GetComponent<UnityEngine.UI.Toggle>().isOn;
            bool coronal = setPlanePanelList[idPlane].transform.Find("setplane_middle1_layout").Find("setplane_coronal_radiobutton").GetComponent<UnityEngine.UI.Toggle>().isOn;
            bool flip = setPlanePanelList[idPlane].transform.Find("setplane_middle1_layout").Find("setplane_flip_toggle").GetComponent<UnityEngine.UI.Toggle>().isOn;
            bool custom = setPlanePanelList[idPlane].transform.Find("setplane_middle2_layout").Find("setplane_custom_radiobutton").GetComponent<UnityEngine.UI.Toggle>().isOn;
            string valueX = setPlanePanelList[idPlane].transform.Find("setplane_middle2_layout").Find("setplane_x_inputfield").GetComponent<InputField>().text;
            string valueY = setPlanePanelList[idPlane].transform.Find("setplane_middle2_layout").Find("setplane_y_inputfield").GetComponent<InputField>().text;
            string valueZ = setPlanePanelList[idPlane].transform.Find("setplane_middle2_layout").Find("setplane_z_inputfield").GetComponent<InputField>().text;
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

            m_scene.update_cut_plane(idOrientation, flip, removeFrontPlane, new Vector3(x, y, z), idPlane, position);
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
            newPlaneButton.transform.FindChild("Text").gameObject.GetComponent<Text>().text = "Cut " + (currentPlaneNumber +1);
            newPlaneButton.name = "choosePlane_selectPlane" + currentPlaneNumber + "_button";

            // raise ui position
            m_ChoosePlanePanel.GetComponent<RectTransform>().sizeDelta =
                new Vector2(m_ChoosePlanePanel.GetComponent<RectTransform>().rect.width, m_ChoosePlanePanel.GetComponent<RectTransform>().rect.height + 25);

            // add toggle choose plane event when clicking on the new button
            int idButton = currentPlaneNumber;
            newPlaneButton.GetComponent<Button>().onClick.AddListener(delegate
            {
                toggleChoosePlane(idButton);
            });

            // add a new setPlane panel corresponding to the new plane
            setPlanePanelList.Add(Instantiate(GlobalGOPreloaded.SetPlanePanel));
            int idSetPlane = setPlanePanelList.Count - 1;
            setPlanePanelList[idSetPlane].transform.SetParent(m_setPlaneListCanvasOverlayTransform);
            
            setPlanePanelList[idSetPlane].name = "Set plane panel overlay " + currentPlaneNumber;

            // add setPlanes listeners
            setPlanePanelList[idSetPlane].transform.Find("setplane bottom layout").Find("setplane_position_slider").GetComponent<Slider>().onValueChanged.AddListener(
                delegate { update_plane(idSetPlane); }
            );
            setPlanePanelList[idSetPlane].transform.Find("setplane_middle1_layout").Find("setplane_axial_radiobutton").GetComponent<UnityEngine.UI.Toggle>().onValueChanged.AddListener(
                delegate { update_plane(idSetPlane); }
            );
            setPlanePanelList[idSetPlane].transform.Find("setplane_middle1_layout").Find("setplane_sagital_radiobutton").GetComponent<UnityEngine.UI.Toggle>().onValueChanged.AddListener(
                delegate { update_plane(idSetPlane); }
            );
            setPlanePanelList[idSetPlane].transform.Find("setplane_middle1_layout").Find("setplane_coronal_radiobutton").GetComponent<UnityEngine.UI.Toggle>().onValueChanged.AddListener(
                delegate { update_plane(idSetPlane); }
            );
            setPlanePanelList[idSetPlane].transform.Find("setplane_middle1_layout").Find("setplane_flip_toggle").GetComponent<UnityEngine.UI.Toggle>().onValueChanged.AddListener(
                delegate { update_plane(idSetPlane); }
            );
            setPlanePanelList[idSetPlane].transform.Find("setplane_middle2_layout").Find("setplane_custom_radiobutton").GetComponent<UnityEngine.UI.Toggle>().onValueChanged.AddListener(
                delegate { update_plane(idSetPlane); }
            );
            setPlanePanelList[idSetPlane].transform.Find("setplane_middle2_layout").Find("setplane_x_inputfield").GetComponent<InputField>().onEndEdit.AddListener(
                delegate { update_plane(idSetPlane); }
            );
            setPlanePanelList[idSetPlane].transform.Find("setplane_middle2_layout").Find("setplane_y_inputfield").GetComponent<InputField>().onEndEdit.AddListener(
                delegate { update_plane(idSetPlane); }
            );
            setPlanePanelList[idSetPlane].transform.Find("setplane_middle2_layout").Find("setplane_z_inputfield").GetComponent<InputField>().onEndEdit.AddListener(
                delegate { update_plane(idSetPlane); }
            );
            //setPlanePanelList[idSetPlane].transform.Find("setplane_bottom_layout").Find("setplane_removeFrontPlane_toggle").GetComponent<UnityEngine.UI.Toggle>().onValueChanged.AddListener(
            //    delegate { updatePlane(idSetPlane); }
            //);

            // add new plane and update the scene
            ++currentPlaneNumber;
            m_scene.add_new_cut_plane();
            update_plane(idSetPlane);
        }

        /// <summary>
        /// Remove the last plane of the UI
        /// </summary>
        private void removePlane()
        {
            // destroy plane button
            Destroy(m_ChoosePlanePanel.transform.FindChild("choosePlane_selectPlane" + (currentPlaneNumber - 1) + "_button").gameObject);

            // down ui
            m_ChoosePlanePanel.GetComponent<RectTransform>().sizeDelta = new Vector2(m_ChoosePlanePanel.GetComponent<RectTransform>().rect.width, m_ChoosePlanePanel.GetComponent<RectTransform>().rect.height - 25);

            // destroy setPlane panel
            Destroy(setPlanePanelList[setPlanePanelList.Count - 1]);
            setPlanePanelList.RemoveAt(setPlanePanelList.Count - 1);

            // remove last plane and update the scene
            m_scene.remove_last_cut_plane();
            --currentPlaneNumber;            
        }


        #endregion others
    }
}