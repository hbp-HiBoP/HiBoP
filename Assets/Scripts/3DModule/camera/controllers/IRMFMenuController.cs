
/**
 * \file    IRMFMenuController.cs
 * \author  Lance Florian
 * \date    10/05/2016
 * \brief   Define IRMFMenuController, IRMFMenu class
 */


// system
using System.Collections.Generic;

// unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace HBP.VISU3D
{
    namespace Events
    {
        /// <summary>
        /// Sned a signal for closing the window
        /// </summary>
        public class CloseIRMFWindow : UnityEvent { }
    }

    /// <summary>
    /// An IRMF menu
    /// </summary>
    public class IRMFMenu : MonoBehaviour
    {
        #region members

        // scene
        private Base3DScene m_scene = null; /**< scene */
        
        // textures
        private Texture2D m_IRMFhistogram = null;

        // parameters
        private int m_columnId;
        private float m_calMin;
        private float m_calMax;

        // ui states
        private bool m_isAlphaMinimized = true;
        private bool m_isThresholdIRMFMinimized = true;

        // events
        public Events.CloseIRMFWindow CloseIRMFWindow = new Events.CloseIRMFWindow();

        #endregion members
        #region functions

        /// <summary>
        /// Init the menu
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="idColumn"></param>
        public void init(Base3DScene scene, int idColumn)
        {
            m_scene = scene;
            m_columnId = idColumn;
            m_IRMFhistogram = new Texture2D(1, 1);

            // define name
            string name = "IRMF left menu ";
            if (scene.singlePatient)
                name += "SP";
            else
                name += "MP";
            name += "" + idColumn;
            gameObject.name = name;

            // reset size and set position in the hierachy
            gameObject.transform.localScale = new Vector3(1, 1, 1);
            gameObject.transform.SetAsFirstSibling();

            // create listeners
            setListeners();
        }

        /// <summary>
        /// Update the IRMF data from the scene
        /// </summary>
        /// <param name="IRMFDataParams"></param>
        public void updateIRMFDataFromScene(IRMFDataParameters IRMFDataParams)
        {
            Transform contentPanelT = transform.Find("panel");

            // min amp            
            string minStr = "" + IRMFDataParams.calValues.min;
            contentPanelT.Find("Min parent").Find("Min value text").GetComponent<Text>().text = (minStr.Length > 5) ? minStr.Substring(0, 5) : minStr; // TODO : add unit

            // max amp
            string maxStr = "" + IRMFDataParams.calValues.max;
            contentPanelT.Find("Max parent").Find("Max value text").GetComponent<Text>().text = (maxStr.Length > 5) ? maxStr.Substring(0, 5) : maxStr;

            // alpha
            contentPanelT.Find("Value alpha parent").Find("Alpha slider").GetComponent<Slider>().value = IRMFDataParams.alpha;

            // calMin
            m_calMin = IRMFDataParams.calMin;
            contentPanelT.Find("Cal min parent").Find("Cal min slider").GetComponent<Slider>().value = m_calMin;

            // calMax
            m_calMax = IRMFDataParams.calMax;
            contentPanelT.Find("Cal max parent").Find("Cal max slider").GetComponent<Slider>().value = m_calMax;

            updateHistogram();
        }

        /// <summary>
        /// Define the listeners of the menu
        /// </summary>
        private void setListeners()
        {
            Button closeButton = transform.Find("title panel").Find("close button").GetComponent<Button>();
            closeButton.onClick.AddListener(() =>
            {
                CloseIRMFWindow.Invoke();
            });

            Transform contentPanelT = transform.Find("panel");
            
            // alpha
            Slider alphaSlider = contentPanelT.Find("Value alpha parent").Find("Alpha slider").GetComponent<Slider>();
            alphaSlider.onValueChanged.AddListener((value) =>
            {
                m_scene.updateIRMFAlpha(value, m_columnId);
            });

            // cal min
            Slider calMinSlider = contentPanelT.Find("Cal min parent").Find("Cal min slider").GetComponent<Slider>();
            calMinSlider.onValueChanged.AddListener((value) =>
            {
                m_calMin = value;
                m_scene.updateIRMFCalMin(m_calMin, m_columnId);
                updateHistogram();
            });

            // cal max
            Slider calMaxSlider = contentPanelT.Find("Cal max parent").Find("Cal max slider").GetComponent<Slider>();
            calMaxSlider.onValueChanged.AddListener((value) =>
            {
                m_calMax = value;
                m_scene.updateIRMFCalMax(m_calMax, m_columnId);
                updateHistogram();
            });


            // minimize / expand alpha
            Button alphaButton = contentPanelT.Find("Alpha parent").Find("Alpha button").GetComponent<Button>();
            alphaButton.onClick.AddListener(delegate
            {
                m_isAlphaMinimized = !m_isAlphaMinimized;
                contentPanelT.Find("Alpha parent").Find("expand image").gameObject.SetActive(m_isAlphaMinimized);
                contentPanelT.Find("Alpha parent").Find("minimize image").gameObject.SetActive(!m_isAlphaMinimized);
                contentPanelT.Find("Value alpha parent").gameObject.SetActive(!m_isAlphaMinimized);
            });

            // minimize/expand threshold IRMF
            Button thresholdIEEGButton = contentPanelT.Find("Threshold IRMF parent").Find("Threshold IRMF button").GetComponent<Button>();
            thresholdIEEGButton.onClick.AddListener(delegate
            {
                m_isThresholdIRMFMinimized = !m_isThresholdIRMFMinimized;
                contentPanelT.Find("Threshold IRMF parent").Find("expand image").gameObject.SetActive(m_isThresholdIRMFMinimized);
                contentPanelT.Find("Threshold IRMF parent").Find("minimize image").gameObject.SetActive(!m_isThresholdIRMFMinimized);

                contentPanelT.Find("Min parent").gameObject.SetActive(!m_isThresholdIRMFMinimized);
                contentPanelT.Find("Max parent").gameObject.SetActive(!m_isThresholdIRMFMinimized);
                contentPanelT.Find("Cal min parent").gameObject.SetActive(!m_isThresholdIRMFMinimized);
                contentPanelT.Find("Cal max parent").gameObject.SetActive(!m_isThresholdIRMFMinimized);
                contentPanelT.Find("Histogram parent").gameObject.SetActive(!m_isThresholdIRMFMinimized);                
            });
        }

        /// <summary>
        /// Update the IRMF histogram with a new image
        /// </summary>
        void updateHistogram()
        {
            Texture2D texture = DLL.Texture.generateDistributionHistogram(m_scene.CM.DLLVolumeIRMFList[m_columnId], 110, 110, m_calMin, m_calMax).getTexture2D();
            Transform contentPanelT = transform.Find("panel");
            Image image = contentPanelT.Find("Histogram parent").Find("Histogram panel").GetComponent<Image>();

            Destroy(m_IRMFhistogram);
            m_IRMFhistogram = texture;

            Destroy(image.sprite);
            image.sprite = Sprite.Create(m_IRMFhistogram,
                   new Rect(0, 0, m_IRMFhistogram.width, m_IRMFhistogram.height),
                   new Vector2(0.5f, 0.5f));
        }

        #endregion functions
    }


    /// <summary>
    /// A class for managing the IRMF menues for each scene and column
    /// </summary>
    public class IRMFMenuController : MonoBehaviour, UICameraOverlay
    {
        #region members

        // scenes
        private Base3DScene m_spScene = null; /**< SP scene */
        private Base3DScene m_mpScene = null; /**< MP scene */

        // menus
        List<GameObject> m_spIRMFMenuList = null; /**< SP iEEG menu list */
        List<GameObject> m_mpIRMFMenuList = null; /**< MP iEEG menu list */

        // canvas
        public Transform m_middlePanelT = null; /**< middle scene panel transform */

        // parameters
        private bool m_displayMenu = false; /**< menu displayed ? */
        private bool m_spSettings = true; /**< is menu displayed from the sp scene ? */
        private int m_spID = 0; /**< ID of the sp column menu to display */
        private int m_mpID = 0; /**< ID of the mp column menu to display */

        #endregion members

        #region functions

        /// <summary>
        /// Init the controller
        /// </summary>
        /// <param name="scenesManager"></param>
        public void init(ScenesManager scenesManager)
        {
            // retrieve scenes
            m_spScene = scenesManager.SPScene;
            m_mpScene = scenesManager.MPScene;

            // init lists of UI
            m_spIRMFMenuList = new List<GameObject>();
            m_mpIRMFMenuList = new List<GameObject>();

            // default initialization with one menu per scene
            m_spIRMFMenuList.Add(generateMenu(m_spScene, 0));
            m_mpIRMFMenuList.Add(generateMenu(m_mpScene, 0));

            // listeners
            m_spScene.SendIRMFParameters.AddListener((IRMFParams) =>
            {
                m_spIRMFMenuList[IRMFParams.columnId].GetComponent<IRMFMenu>().updateIRMFDataFromScene(IRMFParams);
            });

            m_mpScene.SendIRMFParameters.AddListener((IRMFParams) =>
            {
                m_mpIRMFMenuList[IRMFParams.columnId].GetComponent<IRMFMenu>().updateIRMFDataFromScene(IRMFParams);
            });
        }

        /// <summary>
        /// Generate a new iEEG menu
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="columndId"></param>
        /// <returns></returns>
        private GameObject generateMenu(Base3DScene scene, int columndId)
        {
            GameObject IRMFMenuGO = Instantiate(BaseGameObjects.IRMFLeftMenu);
            IRMFMenuGO.AddComponent<IRMFMenu>();
            IRMFMenuGO.transform.SetParent(m_middlePanelT.Find("IRMF left menu list"));

            IRMFMenu menu = IRMFMenuGO.GetComponent<IRMFMenu>();
            menu.init(scene, columndId);

            // set listeners 
            menu.CloseIRMFWindow.AddListener(() =>
            {
                switchUIVisibility();
            });

            return IRMFMenuGO;
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
        /// <param name="columnId"></param>
        public void defineCurrentMenu(bool spScene, int columnId = -1)
        {
            m_spSettings = spScene;

            if (columnId != -1)
            {
                int nbIEEGCols = spScene ? m_spScene.CM.nbIEEGCol() : m_mpScene.CM.nbIEEGCol();

                if (columnId >= nbIEEGCols)
                {
                    if (m_spSettings)
                    {
                        m_spID = columnId - nbIEEGCols;
                    }
                    else
                    {
                        m_mpID = columnId - nbIEEGCols;
                    }
                }
                else
                {
                    if (spScene)
                        m_spID = -1;
                    else
                        m_mpID = -1;
                }
            }

            updateUI();
        }

        /// <summary>
        /// Update the menu UI activity with the current parameters
        /// </summary>
        private void updateUI()
        {
            // hide all the menus
            for (int ii = 0; ii < m_spIRMFMenuList.Count; ++ii)
                m_spIRMFMenuList[ii].SetActive(false);

            for (int ii = 0; ii < m_mpIRMFMenuList.Count; ++ii)
                m_mpIRMFMenuList[ii].SetActive(false);

            // display the menu corresponding to the current scene and column
            if (m_displayMenu)
            {
                if (m_spSettings)
                {
                    if(m_spID != -1)
                        m_spIRMFMenuList[m_spID].SetActive(true);
                }
                else
                {
                    if (m_mpID != -1)
                        m_mpIRMFMenuList[m_mpID].SetActive(true);
                }
            }
        }

        /// <summary>
        /// Adapt the number of menues
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="IRMFColumnsNb"></param>
        public void defineColumnsNb(bool spScene, int IRMFColumnsNb)
        {
            List<GameObject> menuList = spScene ? m_spIRMFMenuList : m_mpIRMFMenuList;

            int diff = menuList.Count - IRMFColumnsNb;
            if (diff < 0) // add menus
            {
                for (int ii = 0; ii < -diff; ++ii)
                    addMenu(spScene);
            }
            else if (diff > 0) // remove menus
            {
                for (int ii = 0; ii < diff; ++ii)
                    removeLastMenu(spScene);
            }

            // define the current scene menu
            defineCurrentMenu(spScene, 0);
        }

        /// <summary>
        /// Add a new column menu
        /// </summary>
        public void addMenu(bool spScene)
        {
            List<GameObject> menuList = spScene ? m_spIRMFMenuList : m_mpIRMFMenuList;
            Base3DScene scene = spScene ? m_spScene : m_mpScene;
            menuList.Add(generateMenu(scene, menuList.Count));
        }

        /// <summary>
        /// Remove the last column menu
        /// </summary>
        /// <param name="spScene"></param>
        public void removeLastMenu(bool spScene)
        {
            List<GameObject> menuList = spScene ? m_spIRMFMenuList : m_mpIRMFMenuList;
            int id = menuList.Count - 1;
            Destroy(menuList[id]);
            menuList.RemoveAt(id);

            if(spScene)
            {
                if (m_spID > menuList.Count - 1)
                    m_spID = -1;
            }
            else
            {
                if (m_mpID > menuList.Count - 1)
                    m_mpID = -1;
            }
        }


        #endregion functions
    }
}