
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

namespace HBP.VISU3D
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
        #region members

        // scene
        private Base3DScene m_scene = null; /**< scene */
        private ScenesManager m_sceneManager = null;

        // texture
        private Texture2D m_histogram = null;

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

        #endregion members

        #region functions

        /// <summary>
        /// Init the menu
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="idColumn"></param>
        public void init(Base3DScene scene, ScenesManager sceneManager)
        {
            m_scene = scene;
            m_sceneManager = sceneManager;
            m_histogram = new Texture2D(1, 1);

            // define name
            string name = "Scene menu ";
            if (scene.singlePatient)
                name += "SP";
            else
                name += "MP";
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

                m_sceneManager.CamerasManager.setEdgesMode(m_scene.singlePatient, showEdges);
            });

            // auto rotate
            Button rotateButton = contentPanelT.Find("Auto rotate button").GetComponent<Button>();
            rotateButton.onClick.AddListener(delegate
            {
                if (m_cameraIsRotating)
                {
                    rotateButton.transform.Find("Text").GetComponent<Text>().text = "Start rotating";
                    m_sceneManager.CamerasManager.stopRotationOfAllCameras(m_scene.singlePatient);
                }
                else
                {
                    rotateButton.transform.Find("Text").GetComponent<Text>().text = "Stop rotating";
                    m_sceneManager.CamerasManager.rotateAllCameras(m_scene.singlePatient);
                }

                m_cameraIsRotating = !m_cameraIsRotating;
            });


            Image histogramImage = contentPanelT.Find("Histogram IRM parent").Find("Histogram panel").GetComponent<Image>();
            // IRM cal min
            Slider calMinSlider = contentPanelT.Find("Cal min IRM parent").Find("Cal min slider").GetComponent<Slider>();
            calMinSlider.onValueChanged.AddListener((value) =>
            {
                if (value < m_IRMCalMax)
                {
                    m_IRMCalMin = value;
                    m_scene.updateIRMCalMin(m_IRMCalMin);
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
                    m_scene.updateIRMCalMax(m_IRMCalMax);
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
        public void updateUIValuesFromScene(IRMCalValues IRMCalValues)
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
            
            updateHistogram();
        }


        /// <summary>
        /// Update the IRM histogram with a new image
        /// </summary>
        void updateHistogram()
        {
            Texture2D texture = DLL.Texture.generateDistributionHistogram(m_scene.CM.DLLVolume, 110, 110, m_IRMCalMin, m_IRMCalMax).getTexture2D();
            Transform contentPanelT = transform.Find("panel");
            Image histogramImage = contentPanelT.Find("Histogram IRM parent").Find("Histogram panel").GetComponent<Image>();

            Destroy(m_histogram);
            m_histogram = texture;

            Destroy(histogramImage.sprite);
            histogramImage.sprite = Sprite.Create(m_histogram,
                   new Rect(0, 0, m_histogram.width, m_histogram.height),
                   new Vector2(0.5f, 0.5f));
        }

        #endregion functions
    }

    /// <summary>
    /// A class for managing the Scene menu for each scene
    /// </summary>
    public class SceneMenuController : MonoBehaviour, UICameraOverlay
    {
        #region members

        // scenes
        private ScenesManager m_sceneManager = null; 

        // canvas
        public Transform m_middlePanelT = null; /**< middle scene panel transform */

        // menus
        GameObject m_spSceneMenu = null;
        GameObject m_mpSceneMenu = null;

        // parameters
        private bool m_displayMenu = false; /**< menu displayed ? */
        private bool m_spSettings = true;   /**< is menu displayed from the sp scene ? */

        #endregion members

        #region functions


        /// <summary>
        /// Init the controller
        /// </summary>
        /// <param name="scenesManager"></param>
        public void init(ScenesManager scenesManager)
        {
            m_sceneManager = scenesManager;

            // generate sp menu
            m_spSceneMenu = generateMenu(scenesManager.SPScene);

            // generate mp menu
            m_mpSceneMenu = generateMenu(scenesManager.MPScene);

            // listeners
            scenesManager.SPScene.IRMCalValuesUpdate.AddListener((IRMCalValues) =>
            {
                m_spSceneMenu.GetComponent<SceneMenu>().updateUIValuesFromScene(IRMCalValues);
            });
            scenesManager.MPScene.IRMCalValuesUpdate.AddListener((IRMCalValues) =>
            {
                m_mpSceneMenu.GetComponent<SceneMenu>().updateUIValuesFromScene(IRMCalValues);
            });
        }

        /// <summary>
        /// Generate a new Scene menu
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        private GameObject generateMenu(Base3DScene scene)
        {
            GameObject sceneMenuGO = Instantiate(BaseGameObjects.SceneLeftMenu);
            sceneMenuGO.AddComponent<SceneMenu>();
            sceneMenuGO.transform.SetParent(m_middlePanelT.Find("Scene left menu list"));

            SceneMenu menu = sceneMenuGO.GetComponent<SceneMenu>();
            menu.init(scene, m_sceneManager);

            // set listeners 
            menu.CloseSceneWindow.AddListener(() =>
            {
                switchUIVisibility();
            });

            return sceneMenuGO;
        }

        /// <summary>
        /// Update the UI visibility with the current mode
        /// </summary>
        /// <param name="mode"></param>
        public void setUIActivity(Mode mode)
        {
            bool menuDisplayed = m_displayMenu;

            // define mode ui specifities
            switch (mode.m_idMode)
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
            setUIVisibility(menuDisplayed);
        }

        /// <summary>
        /// Swith the UI visibility of the UI
        /// </summary>
        public void switchUIVisibility()
        {
            m_displayMenu = !m_displayMenu;
            updateUI();
        }

        /// <summary>
        /// Set the visibility of the UI
        /// </summary>
        /// <param name="visible"></param>
        private void setUIVisibility(bool visible)
        {
            m_displayMenu = visible;
            updateUI();
        }

        /// <summary>
        /// Define the current scene and column selected
        /// </summary>
        /// <param name="spScene"></param>
        public void defineCurrentMenu(bool spScene)
        {
            m_spSettings = spScene;
            updateUI();
        }

        /// <summary>
        /// Update the menu UI activity with the current parameters
        /// </summary>
        private void updateUI()
        {
            // hide all the menus
            m_spSceneMenu.SetActive(false);
            m_mpSceneMenu.SetActive(false);   

            // display the menu corresponding to the current scene and column
            if (m_displayMenu)
            {
                m_spSceneMenu.SetActive(m_spSettings);
                m_mpSceneMenu.SetActive(!m_spSettings);
            }
        }

        #endregion functions
    }
}