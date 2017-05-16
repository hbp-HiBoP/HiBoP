
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
using HBP.Module3D;

namespace HBP.UI.Module3D
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
    public class FMRIMenu : MonoBehaviour
    {
        #region Properties

        public bool m_isDisplayed = false;
        public bool m_isROIMenuMinimized = true;

        // scene
        private Base3DScene m_scene = null; /**< scene */
        
        // textures
        private Texture2D m_IRMFhistogram = null;

        // parameters
        public int m_columnId;
        private float m_calMin;
        private float m_calMax;

        // ui states
        private bool m_isAlphaMinimized = true;
        private bool m_isThresholdIRMFMinimized = true;

        // events
        public Events.CloseIRMFWindow CloseIRMFWindow = new Events.CloseIRMFWindow();
        public Events.UpdateROIMenuDisplay UpdateROIMenuDisplay = new Events.UpdateROIMenuDisplay();

        #endregion
        #region Public Methods

        /// <summary>
        /// Init the menu
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="idColumn"></param>
        public void init(Base3DScene scene, int idColumn)
        {
            m_scene = scene;
            m_columnId = idColumn;
            m_IRMFhistogram = Texture2Dutility.GenerateHistogram();

            // define name
            string nameMenu = "fMRI left menu ";
            switch(scene.Type)
            {
                case SceneType.SinglePatient:
                    nameMenu += "SP";
                    break;
                case SceneType.MultiPatients:
                    nameMenu += "MP";
                    transform.Find("panel").Find("ROI parent").gameObject.SetActive(true);
                    transform.Find("panel").Find("separation panel 0").gameObject.SetActive(true);
                    break;
            }

            nameMenu += "" + idColumn;
            name = nameMenu;

            // reset size and set position in the hierachy
            transform.localScale = new Vector3(1, 1, 1);
            transform.SetAsFirstSibling();

            // create listeners
            set_listeners();
        }

        /// <summary>
        /// Update the IRMF data from the scene
        /// </summary>
        /// <param name="IRMFDataParams"></param>
        public void update_FMRI_params_from_scene(FMRIDataParameters IRMFDataParams)
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

            update_histogram();
        }

        /// <summary>
        /// Define the listeners of the menu
        /// </summary>
        private void set_listeners()
        {
            Button closeButton = transform.Find("title panel").Find("close button").GetComponent<Button>();
            closeButton.onClick.AddListener(() =>
            {
                if (!m_isROIMenuMinimized)
                    switch_ROI_menu();

                CloseIRMFWindow.Invoke();
            });

            Transform contentPanelT = transform.Find("panel");
            
            // alpha
            Slider alphaSlider = contentPanelT.Find("Value alpha parent").Find("Alpha slider").GetComponent<Slider>();
            alphaSlider.onValueChanged.AddListener((value) =>
            {
                m_scene.UpdateFMRIAlpha(value, m_columnId);
            });

            // cal min
            Slider calMinSlider = contentPanelT.Find("Cal min parent").Find("Cal min slider").GetComponent<Slider>();
            calMinSlider.onValueChanged.AddListener((value) =>
            {
                m_calMin = value;
                m_scene.UpdateFMRICalMin(m_calMin, m_columnId);
                update_histogram();
            });

            // cal max
            Slider calMaxSlider = contentPanelT.Find("Cal max parent").Find("Cal max slider").GetComponent<Slider>();
            calMaxSlider.onValueChanged.AddListener((value) =>
            {
                m_calMax = value;
                m_scene.UpdateFMRICalMax(m_calMax, m_columnId);
                update_histogram();
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

            // minimize/expand ROI menu
            if(m_scene.Type == SceneType.MultiPatients)
            {
                Button ROIButton = contentPanelT.Find("ROI parent").Find("ROI button").GetComponent<Button>();
                ROIButton.onClick.AddListener(delegate
                {
                    switch_ROI_menu();
                });
            }
        }

        private void switch_ROI_menu()
        {
            Transform contentPanelT = transform.Find("panel");
            m_isROIMenuMinimized = !m_isROIMenuMinimized;
            contentPanelT.Find("ROI parent").Find("expand image").gameObject.SetActive(m_isROIMenuMinimized);
            contentPanelT.Find("ROI parent").Find("minimize image").gameObject.SetActive(!m_isROIMenuMinimized);

            UpdateROIMenuDisplay.Invoke(!m_isROIMenuMinimized);
        }


        /// <summary>
        /// Update the IRMF histogram with a new image
        /// </summary>
        void update_histogram()
        {
            HBP.Module3D.DLL.Texture.GenerateDistributionHistogram(m_scene.Column3DViewManager.DLLVolumeFMriList[m_columnId], 4 * 110, 4 * 110, m_calMin, m_calMax).UpdateTexture2D(m_IRMFhistogram);

            Transform contentPanelT = transform.Find("panel");
            Image image = contentPanelT.Find("Histogram parent").Find("Histogram panel").GetComponent<Image>();
            Destroy(image.sprite);
            image.sprite = Sprite.Create(m_IRMFhistogram,
                   new Rect(0, 0, m_IRMFhistogram.width, m_IRMFhistogram.height),
                   new Vector2(0.5f, 0.5f), 400f);
        }

        #endregion
    }


    /// <summary>
    /// A class for managing the FMRI menues for each scene and column
    /// </summary>
    public class FMRIMenuController : MonoBehaviour, UICameraOverlay
    {
        #region Properties

        // scenes
        private Base3DScene m_scene = null; /**< scene */

        // menus
        List<GameObject> m_FMRIMenuList = new List<GameObject>(); /**< iEEG menu list */
        FMRIMenu m_currentMenu = null;

        // canvas
        public Transform m_middlePanelT = null; /**< middle scene panel transform */

        // events
        public Events.UpdateColROIMenuDisplay UpdateColROIMenuDisplay = new Events.UpdateColROIMenuDisplay();

        #endregion

        #region Public Methods

        /// <summary>
        /// Init the controller
        /// </summary>
        /// <param name="scenesManager"></param>
        public void init(Base3DScene scene)
        {
            // retrieve scenes
            m_scene = scene;

            // listeners
            m_scene.OnSendFMRIParameters.AddListener((FMRIParams) =>
            {
                m_FMRIMenuList[FMRIParams.columnId].GetComponent<FMRIMenu>().update_FMRI_params_from_scene(FMRIParams);
            });
        }

        /// <summary>
        /// Generate a new iEEG menu
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="columndId"></param>
        /// <returns></returns>
        private GameObject generate_menu(int columndId)
        {
            GameObject IRMFMenuGO = Instantiate(GlobalGOPreloaded.fMRILeftMenu);
            IRMFMenuGO.AddComponent<FMRIMenu>();
            IRMFMenuGO.transform.SetParent(m_middlePanelT);

            FMRIMenu menu = IRMFMenuGO.GetComponent<FMRIMenu>();
            menu.init(m_scene, columndId);

            // set listeners 
            menu.CloseIRMFWindow.AddListener(() =>
            {
                switch_UI_visibility();
            });

            menu.UpdateROIMenuDisplay.AddListener((enabled) =>
            {
                UpdateColROIMenuDisplay.Invoke(enabled, columndId);
            });

            return IRMFMenuGO;
        }

        /// <summary>
        /// Update the UI visibility with the current mode
        /// </summary>
        /// <param name="mode"></param>
        public void UpdateByMode(Mode mode)
        {
            if (m_currentMenu == null)
                return;

            bool menuDisplayed = m_currentMenu.m_isDisplayed;

            // define mode ui specifities
            switch (mode.ID)
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
            set_UI_visibility(menuDisplayed);
        }

        /// <summary>
        /// Swith the UI visibility of the UI
        /// </summary>
        public void switch_UI_visibility()
        {
            m_currentMenu.m_isDisplayed = !m_currentMenu.m_isDisplayed;
            update_UI();
        }

        /// <summary>
        /// Set the visibility of the UI
        /// </summary>
        /// <param name="visible"></param>
        private void set_UI_visibility(bool visible)
        {
            m_currentMenu.m_isDisplayed = visible;
            update_UI();
        }

        /// <summary>
        /// Define the current column selected
        /// </summary>
        /// <param name="columnId"> id of the column (counting iEEG and fMRI)</param>
        public void define_current_column(int columnId = -1)
        {
            int nbIEEGCols = m_scene.Column3DViewManager.ColumnsIEEG.Count;
            if (columnId >= nbIEEGCols)
                m_currentMenu = m_FMRIMenuList[columnId - nbIEEGCols].GetComponent<FMRIMenu>();
            else
                m_currentMenu = null;

            update_UI();
        }

        /// <summary>
        /// Update the menu UI activity with the current parameters
        /// </summary>
        private void update_UI()
        {
            // hide all the menus
            for (int ii = 0; ii < m_FMRIMenuList.Count; ++ii)
                m_FMRIMenuList[ii].SetActive(false);

            if(m_currentMenu != null)
                m_currentMenu.gameObject.SetActive(m_currentMenu.m_isDisplayed);
        }

        /// <summary>
        /// Add a new column menu
        /// </summary>
        public void add_menu()
        {
            m_FMRIMenuList.Add(generate_menu(m_FMRIMenuList.Count));
            update_UI();
        }

        /// <summary>
        /// Remove the last column menu
        /// </summary>
        public void remove_last_menu()
        {
            int id = m_FMRIMenuList.Count - 1;
            Destroy(m_FMRIMenuList[id]);
            m_FMRIMenuList.RemoveAt(id);
            
            if(m_currentMenu != null)
                if(id == m_currentMenu.m_columnId) // current menu is deleted
                    m_currentMenu = null;
            
            update_UI();
        }


        #endregion
    }
}