
/**
 * \file    CommonMenuController.cs
 * \author  Lance Florian
 * \date    24/08/2016
 * \brief   Define SceneMenuController,SceneMenu classes
 */

// system
using System;

// unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using HBP.UI.Module3D;

namespace HBP.Module3D
{
    namespace Events
    {
        /// Send a signal for closing the window
        /// </summary>
        public class CloseSceneWindow : UnityEvent { }
    }

    /// <summary>
    /// Scene menu UI
    /// </summary>
    public class SceneMenu : MonoBehaviour
    {
        #region Properties

        // scene
        private Base3DScene m_scene = null; /**< scene */
        private Cam.CamerasManager m_camerasManager = null;

        // texture
        private Texture2D m_mriHistogram = null;

        // parameters
        private float m_IRMCalMin = 0f;
        private float m_IRMCalMax = 1f;
        private float m_computedCalMinValue;
        private float m_computedCalMaxValue;

        // ui states
        private bool m_isThresholdIRMMinimized = true;
        private bool m_isDisplayMinimized = true;
        private bool m_isActionsMinimized = true;
        private bool m_cameraIsRotating = false;

        // events
        public Events.CloseSceneWindow CloseSceneWindow = new Events.CloseSceneWindow();

        #endregion

        #region Public Methods

        /// <summary>
        /// Init the menu
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="idColumn"></param>
        public void init(Base3DScene scene, Cam.CamerasManager camerasManager)
        {
            m_scene = scene;
            m_camerasManager = camerasManager;
            m_mriHistogram = new Texture2D(1, 1);

            // define name
            string name = "Scene menu ";
            switch(scene.Type)
            {
                case SceneType.SinglePatient:
                    name += "SP";
                    break;
                case SceneType.MultiPatients:
                    name += "MP";
                    break;
            }
            gameObject.name = name;

            // reset size and set position in the hierachy
            gameObject.transform.localScale = new Vector3(1, 1, 1);
            gameObject.transform.SetAsFirstSibling();

            // create listeners
            setListeners();
        }

        /// <summary>
        /// Define the listeners of the menu
        /// </summary>
        private void setListeners()
        {
            Button closeButton = transform.Find("title panel").Find("close button").GetComponent<Button>();
            closeButton.onClick.AddListener(() =>
            {
                CloseSceneWindow.Invoke();
            });

            Transform contentPanelT = transform.Find("panel");

            // edges switch
            Button edgeButton = contentPanelT.Find("Edges button").GetComponent<Button>();
            edgeButton.onClick.AddListener(delegate
            {
                bool showEdges;
                if ("Show edges" == edgeButton.transform.Find("Text").GetComponent<Text>().text)
                {
                    edgeButton.transform.Find("Text").GetComponent<Text>().text = "Hide edges";
                    showEdges = true;
                }
                else
                {
                    edgeButton.transform.Find("Text").GetComponent<Text>().text = "Show edges";
                    showEdges = false;
                }

                m_camerasManager.SetEdgesMode(m_scene.Type, showEdges);
            });

            // auto rotate
            Button rotateButton = contentPanelT.Find("Auto rotate button").GetComponent<Button>();
            rotateButton.onClick.AddListener(delegate
            {
                if (m_cameraIsRotating)
                {
                    rotateButton.transform.Find("Text").GetComponent<Text>().text = "Start rotating";
                    m_camerasManager.StopRotationOfAllCameras(m_scene.Type);
                }
                else
                {
                    rotateButton.transform.Find("Text").GetComponent<Text>().text = "Stop rotating";
                    m_camerasManager.RotateAllCameras(m_scene.Type);
                }

                m_cameraIsRotating = !m_cameraIsRotating;
            });


            //Image histogramImage = contentPanelT.Find("Histogram IRM parent").Find("Histogram panel").GetComponent<Image>();
            // IRM cal min
            Slider calMinSlider = contentPanelT.Find("Cal min IRM parent").Find("Cal min slider").GetComponent<Slider>();
            calMinSlider.onValueChanged.AddListener((value) =>
            {
                if (value < m_IRMCalMax)
                {
                    m_IRMCalMin = value;
                    m_scene.UpdateMRICalMin(m_IRMCalMin);
                    updateUIValues();
                }
                else
                    calMinSlider.value = m_IRMCalMin;
            });
            // IRM cal max
            Slider calMaxSlider = contentPanelT.Find("Cal max IRM parent").Find("Cal max slider").GetComponent<Slider>();
            calMaxSlider.onValueChanged.AddListener((value) =>
            {
                if (value > m_IRMCalMin)
                {
                    m_IRMCalMax = value;
                    m_scene.UpdateMRICalMax(m_IRMCalMax);
                    updateUIValues();
                }
                else
                    calMaxSlider.value = m_IRMCalMax;
            });

            // minimize/expand alpha
            Button thresholdIRMButton = contentPanelT.Find("Threshold IRM parent").Find("Threshold IRM button").GetComponent<Button>();
            thresholdIRMButton.onClick.AddListener(delegate
            {
                m_isThresholdIRMMinimized = !m_isThresholdIRMMinimized;
                contentPanelT.Find("Threshold IRM parent").Find("expand image").gameObject.SetActive(m_isThresholdIRMMinimized);
                contentPanelT.Find("Threshold IRM parent").Find("minimize image").gameObject.SetActive(!m_isThresholdIRMMinimized);
                contentPanelT.Find("Min IRM parent").gameObject.SetActive(!m_isThresholdIRMMinimized);
                contentPanelT.Find("Max IRM parent").gameObject.SetActive(!m_isThresholdIRMMinimized);
                contentPanelT.Find("Cal min IRM parent").gameObject.SetActive(!m_isThresholdIRMMinimized);
                contentPanelT.Find("Cal max IRM parent").gameObject.SetActive(!m_isThresholdIRMMinimized);
                contentPanelT.Find("Histogram IRM parent").gameObject.SetActive(!m_isThresholdIRMMinimized);
            });

            // minimize/expand display
            Button displayButton = contentPanelT.Find("Display parent").Find("Display button").GetComponent<Button>();
            displayButton.onClick.AddListener(delegate
            {
                m_isDisplayMinimized = !m_isDisplayMinimized;
                contentPanelT.Find("Display parent").Find("expand image").gameObject.SetActive(m_isDisplayMinimized);
                contentPanelT.Find("Display parent").Find("minimize image").gameObject.SetActive(!m_isDisplayMinimized);
                contentPanelT.Find("Edges button").gameObject.SetActive(!m_isDisplayMinimized);
            });

            // minimize/expand actions
            Button actionsButton = contentPanelT.Find("Actions parent").Find("Actions button").GetComponent<Button>();
            actionsButton.onClick.AddListener(delegate
            {
                m_isActionsMinimized = !m_isActionsMinimized;
                contentPanelT.Find("Actions parent").Find("expand image").gameObject.SetActive(m_isActionsMinimized);
                contentPanelT.Find("Actions parent").Find("minimize image").gameObject.SetActive(!m_isActionsMinimized);
                contentPanelT.Find("Auto rotate button").gameObject.SetActive(!m_isActionsMinimized);
            });
        }

        /// <summary>
        /// Retrieve cal values from scene and update ui values
        /// </summary>
        /// <param name="IRMCalValues"></param>
        public void updateUIValuesFromScene(MRICalValues IRMCalValues)
        {
            m_computedCalMinValue = IRMCalValues.computedCalMin;
            m_computedCalMaxValue = IRMCalValues.computedCalMax;
            updateUIValues();
        }

        /// <summary>
        /// Update the UI values 
        /// </summary>
        private void updateUIValues()
        {
            Transform contentPanelT = transform.Find("panel");
            contentPanelT.Find("Min IRM parent").Find("Min value text").GetComponent<Text>().text = "" + Math.Round((decimal)m_computedCalMinValue, 2);
            contentPanelT.Find("Max IRM parent").Find("Max value text").GetComponent<Text>().text = "" + Math.Round((decimal)m_computedCalMaxValue, 2);

            float diff = m_computedCalMaxValue - m_computedCalMinValue;
            contentPanelT.Find("Cal min IRM parent").Find("Cal min text").GetComponent<Text>().text = "Cal min : " + Math.Round((decimal)(m_computedCalMinValue + diff * m_IRMCalMin), 2);
            contentPanelT.Find("Cal max IRM parent").Find("Cal max text").GetComponent<Text>().text = "Cal max : " + Math.Round((decimal)(m_computedCalMinValue + diff * m_IRMCalMax), 2);

            update_MRI_histogram();
        }

        /// <summary>
        /// Update the MRI histogram with a new image
        /// </summary>
        void update_MRI_histogram()
        {
            DLL.Texture.GenerateDistributionHistogram(m_scene.ColumnManager.DLLVolume, 4 * 110, 4 * 110, m_IRMCalMin, m_IRMCalMax).UpdateTexture2D(m_mriHistogram);

            Transform contentPanelT = transform.Find("panel");
            Image histogramImage = contentPanelT.Find("Histogram IRM parent").Find("Histogram panel").GetComponent<Image>();
            Destroy(histogramImage.sprite);
            histogramImage.sprite = Sprite.Create(m_mriHistogram,
                   new Rect(0, 0, m_mriHistogram.width, m_mriHistogram.height),
                   new Vector2(0.5f, 0.5f), 400f);
        }

        #endregion
    }

    /// <summary>
    /// A class for managing the Scene menu for each scene
    /// </summary>
    public class SceneMenuController : MonoBehaviour, UICameraOverlay
    {
        #region Properties

        private Cam.CamerasManager m_camerasManager = null;

        // canvas
        public Transform m_middlePanelT = null; /**< middle scene panel transform */

        // menus
        GameObject m_sceneMenu = null;
        Base3DScene m_scene = null;

        // parameters
        private bool m_displayMenu = false; /**< menu displayed ? */

        #endregion

        #region Public Methods

        /// <summary>
        /// Init the controller
        /// </summary>
        /// <param name="scenesManager"></param>
        public void init(Base3DScene scene, Cam.CamerasManager camerasManager)
        {
            m_scene = scene;
            m_camerasManager = camerasManager;

            // generate sp menu
            m_sceneMenu = generate_menu();

            // listeners
            m_scene.Events.OnMRICalValuesUpdate.AddListener((IRMCalValues) =>
            {
                m_sceneMenu.GetComponent<SceneMenu>().updateUIValuesFromScene(IRMCalValues);
            });
        }

        private GameObject generate_menu()
        {
            GameObject sceneMenuGO = Instantiate(GlobalGOPreloaded.SceneLeftMenu);
            sceneMenuGO.AddComponent<SceneMenu>();
            sceneMenuGO.transform.SetParent(m_middlePanelT);

            SceneMenu menu = sceneMenuGO.GetComponent<SceneMenu>();
            menu.init(m_scene, m_camerasManager);

            // set listeners 
            menu.CloseSceneWindow.AddListener(() =>
            {
                switch_UI_visibility();
            });

            return sceneMenuGO;
        }

        /// <summary>
        /// Update the UI visibility with the current mode
        /// </summary>
        /// <param name="mode"></param>
        public void UpdateByMode(Mode mode)
        {
            bool menuDisplayed = m_displayMenu;

            // define mode ui specifities
            switch (mode.ID)
            {
                case Mode.ModesId.NoPathDefined:
                    menuDisplayed = false;
                    break;
                case Mode.ModesId.MinPathDefined:
                    menuDisplayed = true;
                    break;
                case Mode.ModesId.TriErasing:
                    menuDisplayed = false;
                    break;
                case Mode.ModesId.Error:
                    menuDisplayed = false;
                    break;
            }

            // set the state of the menu
            set_UI_visibility(menuDisplayed);
        }

        /// <summary>
        /// Swith the UI visibility of the UI
        /// </summary>
        public void switch_UI_visibility()
        {
            m_displayMenu = !m_displayMenu;
            update_UI();
        }

        /// <summary>
        /// Set the visibility of the UI
        /// </summary>
        /// <param name="visible"></param>
        private void set_UI_visibility(bool visible)
        {
            m_displayMenu = visible;
            update_UI();
        }

        /// <summary>
        /// Update the menu UI activity with the current parameters
        /// </summary>
        private void update_UI()
        {
            // hide all the menus
            m_sceneMenu.SetActive(false);

            // display the menu corresponding to the current column
            m_sceneMenu.SetActive(m_displayMenu);
        }

        #endregion
    }
}