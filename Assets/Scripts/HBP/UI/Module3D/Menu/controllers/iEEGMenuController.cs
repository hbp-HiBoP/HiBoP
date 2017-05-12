
/**
 * \file    iEEGMenuController.cs
 * \author  Lance Florian
 * \date    4/04/2016
 * \brief   Define iEEGMenuController,iEEGMenu classes
 */

// system
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
        /// Send the alpha params (params : alphaParams)
        /// </summary>
        public class ApplyAlphaToTAllCols : UnityEvent<IEEGAlphaParameters> { }

        /// <summary>
        /// Send the sites params (params : sitesParams)
        /// </summary>
        public class ApplySitesToTAllCols : UnityEvent<IEEGSitesParameters> { }

        /// <summary>
        /// Send the threshold params (params : thresholdParams)
        /// </summary>
        public class ApplyThresholdToTAllCols : UnityEvent<IEEGThresholdParameters> { }

        /// <summary>
        /// Send a signal for closing the window
        /// </summary>
        public class CloseIEEGWindow : UnityEvent { }

        /// <summary>
        /// Send a signal for opening/closing the ROI menu
        /// </summary>
        public class UpdateROIMenuDisplay : UnityEvent<bool> { }

        /// <summary>
        /// Send a signal for opening/closing the ROI menu for a column
        /// </summary>
        public class UpdateColROIMenuDisplay : UnityEvent<bool,int> { }
    }

    /// <summary>
    /// iEEG UI menu
    /// </summary>
    public class iEEGMenu : MonoBehaviour
    {
        #region Properties

        public bool m_isDisplayed = false;

        // parameters
        private bool m_isAlphaMinimized = true;
        private bool m_isSitesMinimized = true;
        private bool m_isThresholdIEEGMinimized = true;
        private bool m_isROIMenuMinimized = true;
        
        private IEEGDataParameters m_;

        public int m_columnId = -1;
        public int m_maxDistance = 50;

        Texture2D m_iEEGHistogram = null;
        private Base3DScene m_scene = null; /**< SP scene */

        // events
        public Events.ApplyAlphaToTAllCols ApplyAlphaToTAllCols = new Events.ApplyAlphaToTAllCols();
        public Events.ApplySitesToTAllCols ApplySitesToTAllCols = new Events.ApplySitesToTAllCols();
        public Events.ApplyThresholdToTAllCols ApplyThresholdToTAllCols = new Events.ApplyThresholdToTAllCols();
        public Events.CloseIEEGWindow CloseIEEGWindow = new Events.CloseIEEGWindow();
        public Events.UpdateROIMenuDisplay UpdateROIMenuDisplay  = new Events.UpdateROIMenuDisplay();        

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
            m_iEEGHistogram = Texture2Dutility.GenerateHistogram();

            // define name
            string name = "iEEG left menu ";
            switch (scene.Type)
            {
                case SceneType.SinglePatient:
                    name += "SP";
                    break;
                case SceneType.MultiPatients:
                    name += "MP";
                    break;
                default:
                    break;
            }
            name += "" + idColumn;
            gameObject.name = name;

            // reset size and set position in the hierachy
            gameObject.transform.localScale = new Vector3(1, 1, 1);
            gameObject.transform.SetAsFirstSibling();

            // create listeners
            set_listeners();            
        }

        /// <summary>
        /// Update the UI alpha values
        /// </summary>
        /// <param name="iEEGAlphaParams"></param>
        public void update_UI_alpha_values(IEEGAlphaParameters iEEGAlphaParams)
        {
            m_.alphaMin = iEEGAlphaParams.alphaMin;
            m_.alphaMax = iEEGAlphaParams.alphaMax;

            Transform contentPanelT = transform.Find("panel");
            contentPanelT.Find("Min alpha parent").Find("Min alpha slider").GetComponent<Slider>().value = m_.alphaMin;
            contentPanelT.Find("Max alpha parent").Find("Max alpha slider").GetComponent<Slider>().value = m_.alphaMax;
        }

        /// <summary>
        /// Update the UI sites  values
        /// </summary>
        /// <param name="menus"></param>
        /// <param name="iEEGSitesParams"></param>
        public void update_UI_gain_sites_values(IEEGSitesParameters iEEGSitesParams)
        {
            m_.maxDistance = iEEGSitesParams.maxDistance;
            m_.gain = iEEGSitesParams.gain;

            Transform contentPanelT = transform.Find("panel");
            contentPanelT.Find("Inf dist parent").Find("Inf dist inputfield").GetComponent<InputField>().text = "" + m_.maxDistance;
            contentPanelT.Find("Gain plots parent").Find("Gain plot slider").GetComponent<Slider>().value = m_.gain;
        }

        /// <summary>
        /// Update the UI threshold values
        /// </summary>
        /// <param name="iEEGThresholdParams"></param>
        public void update_UI_threshold_values(IEEGThresholdParameters iEEGThresholdParams)
        {
            m_.spanMin = iEEGThresholdParams.minSpan;
            m_.middle = iEEGThresholdParams.middle;
            m_.spanMax = iEEGThresholdParams.maxSpan;

            Transform contentPanelT = transform.Find("panel");
            contentPanelT.Find("Min span parent").Find("Min span inputfield").GetComponent<InputField>().text = "" + m_.spanMin;
            contentPanelT.Find("Middle parent").Find("Middle inputfield").GetComponent<InputField>().text = "" + m_.middle;
            contentPanelT.Find("Max span parent").Find("Max span inputfield").GetComponent<InputField>().text = "" + m_.spanMax;

            // cal sliders
            float diff = m_.maxAmp - m_.minAmp;
            contentPanelT.Find("Cal min iEEG slider").GetComponent<Slider>().value = (iEEGThresholdParams.minSpan - m_.minAmp) / diff;
            contentPanelT.Find("Cal middle iEEG slider").GetComponent<Slider>().value = (iEEGThresholdParams.middle - m_.minAmp) / diff;
            contentPanelT.Find("Cal max iEEG slider").GetComponent<Slider>().value = (iEEGThresholdParams.maxSpan - m_.minAmp) / diff;

            update_histogram();
        }


        /// <summary>
        /// Return the truncated thresholds parameters for this menu, increment the warnings 
        /// </summary>
        /// <param name="iEEGThresholdParams"></param>
        /// <param name="warningsTruncation"></param>
        /// <returns></returns>
        public IEEGThresholdParameters check_threshold_values(IEEGThresholdParameters iEEGThresholdParams, List<bool> warningsTruncation)
        {
            IEEGThresholdParameters thresholdParams = iEEGThresholdParams;

            bool warning = false;
            //Transform contentPanelT = transform.Find("panel");

            // middle
            if (thresholdParams.middle < m_.minAmp || thresholdParams.middle > m_.maxAmp)
            {
                thresholdParams.middle = m_.minAmp;
                warning = true;
            }

            // span min
            if (thresholdParams.minSpan < m_.minAmp || thresholdParams.minSpan > m_.maxAmp)
            {
                thresholdParams.minSpan = m_.minAmp;
                warning = true;
            }

            // span max
            if (thresholdParams.maxSpan > m_.maxAmp || thresholdParams.maxSpan < m_.minAmp)
            {
                thresholdParams.maxSpan = m_.maxAmp;
                warning = true;
            }

            warningsTruncation.Add(warning);

            return thresholdParams;
        }

        /// <summary>
        /// Update the IEEG UI values
        /// </summary>
        /// <param name="menus"></param>
        /// <param name="iEEGDataParams"></param>
        public void update_UI_iEEG_data_from_scene(IEEGDataParameters iEEGDataParams)
        {
            Transform contentPanelT = transform.Find("panel");

            // min amp            
            m_.minAmp = iEEGDataParams.minAmp;
            string minAmpStr = "" + m_.minAmp;
            contentPanelT.Find("Min parent").Find("Min value text").GetComponent<Text>().text = (minAmpStr.Length > 5) ? minAmpStr.Substring(0, 5) : minAmpStr; // TODO : add unit

            // max amp
            m_.maxAmp = iEEGDataParams.maxAmp;
            string maxAmpStr = "" + m_.maxAmp;
            contentPanelT.Find("Max parent").Find("Max value text").GetComponent<Text>().text = (maxAmpStr.Length > 5) ? maxAmpStr.Substring(0, 5) : maxAmpStr;

            IEEGAlphaParameters alphaParams;
            alphaParams.alphaMin = iEEGDataParams.alphaMin;
            alphaParams.alphaMax = iEEGDataParams.alphaMax;
            alphaParams.columnId = iEEGDataParams.columnId;
            update_UI_alpha_values(alphaParams);

            IEEGSitesParameters sitesParams;
            sitesParams.gain = iEEGDataParams.gain;
            sitesParams.maxDistance = iEEGDataParams.maxDistance;
            sitesParams.columnId = iEEGDataParams.columnId;
            update_UI_gain_sites_values(sitesParams);

            IEEGThresholdParameters thresholdsParams;
            thresholdsParams.minSpan = iEEGDataParams.spanMin;
            thresholdsParams.middle = iEEGDataParams.middle;
            thresholdsParams.maxSpan = iEEGDataParams.spanMax;
            thresholdsParams.columnId = iEEGDataParams.columnId;
            update_UI_threshold_values(thresholdsParams);
        }

        /// <summary>
        /// Define the listeneras associated to the menu
        /// </summary>
        private void set_listeners()
        {
            Button closeButton = transform.Find("title panel").Find("close button").GetComponent<Button>();
            closeButton.onClick.AddListener(() =>
            {
                if (!m_isROIMenuMinimized)
                    switch_ROI_menu();

                CloseIEEGWindow.Invoke();
            });

            Transform contentPanelT = transform.Find("panel");

            //Text minText = contentPanelT.Find("Min parent").Find("Min value text").GetComponent<Text>();
            //Text maxText = contentPanelT.Find("Max parent").Find("Max value text").GetComponent<Text>();

            InputField minSpanInputF = contentPanelT.Find("Min span parent").Find("Min span inputfield").GetComponent<InputField>();
            InputField middleInputF = contentPanelT.Find("Middle parent").Find("Middle inputfield").GetComponent<InputField>();
            InputField maxSpanInputF = contentPanelT.Find("Max span parent").Find("Max span inputfield").GetComponent<InputField>();

            Slider minAlphaSlider = contentPanelT.Find("Min alpha parent").Find("Min alpha slider").GetComponent<Slider>();
            Slider maxAlphaSlider = contentPanelT.Find("Max alpha parent").Find("Max alpha slider").GetComponent<Slider>();
            Slider gainSlider = contentPanelT.Find("Gain plots parent").Find("Gain plot slider").GetComponent<Slider>();
            Slider minSpanSlider = contentPanelT.Find("Cal min iEEG slider").GetComponent<Slider>();
            Slider middleSpanSlider = contentPanelT.Find("Cal middle iEEG slider").GetComponent<Slider>();
            Slider maxSpanSlider = contentPanelT.Find("Cal max iEEG slider").GetComponent<Slider>();

            // alpha
            minAlphaSlider.onValueChanged.AddListener((value) =>{ m_.alphaMin = value; m_scene.UpdateIEEGMinAlpha(value, m_columnId); });            
            maxAlphaSlider.onValueChanged.AddListener((value) =>{ m_.alphaMax = value; m_scene.UpdateIEEGMaxAlpha(value, m_columnId); });

            // gain bubble            
            gainSlider.onValueChanged.AddListener((value) => { m_.gain = value; m_scene.UpdateBubblesGain(value, m_columnId); });            

            // min span            
            minSpanInputF.onEndEdit.AddListener(delegate
            {
                if (minSpanInputF.text.Length == 0)
                    minSpanInputF.text = "0";
                else if (minSpanInputF.text.Length == 1)
                {
                    if (minSpanInputF.text[0] == '.')
                        minSpanInputF.text = "0" + minSpanInputF.text;

                    if (minSpanInputF.text[0] == '-')
                        minSpanInputF.text = "0";
                }

                m_.spanMin = float.Parse(minSpanInputF.text);
                float diff = m_.maxAmp - m_.minAmp;

                // truncate min value
                if (m_.spanMin > m_.middle)
                {
                    m_.spanMin = m_.middle;
                    minSpanInputF.text = "" + m_.spanMin;
                }
                if (m_.spanMin < m_.minAmp)
                {
                    m_.spanMin = m_.minAmp;
                    minSpanInputF.text = "" + m_.spanMin;
                }

                // update span min
                minSpanSlider.value = (m_.spanMin - m_.minAmp) / diff;
                m_scene.UpdateIEEGSpanMin(m_.spanMin, m_columnId);

                update_histogram();
            });


            minSpanSlider.onValueChanged.AddListener((value) =>
            {
                float diff = m_.maxAmp - m_.minAmp;
                m_.spanMin = m_.minAmp + value * diff;

                if (m_.spanMin > m_.middle)
                {
                    m_.spanMin = m_.middle;
                    minSpanSlider.value = (m_.spanMin - m_.minAmp) / diff;
                }
                
                minSpanInputF.text = "" + m_.spanMin;
                m_scene.UpdateIEEGSpanMin(m_.spanMin, m_columnId);

                update_histogram();
            });


            // middle            
            middleInputF.onEndEdit.AddListener(delegate
            {
                if (middleInputF.text.Length == 0)
                    middleInputF.text = "0";
                else if (middleInputF.text.Length == 1)
                {
                    if (middleInputF.text[0] == '.')
                        middleInputF.text = "0" + middleInputF.text;

                    if (middleInputF.text[0] == '-')
                        middleInputF.text = "0";
                }

                m_.middle = float.Parse(middleInputF.text);
                float diff = m_.maxAmp - m_.minAmp;

                // truncate middle values
                if (m_.middle > m_.maxAmp)
                {
                    m_.middle = m_.maxAmp;
                    middleInputF.text = "" + m_.middle;
                }
                if (m_.middle < m_.minAmp)
                {
                    m_.middle = m_.minAmp;
                    middleInputF.text = "" + m_.middle;
                }

                if (m_.spanMin > m_.middle)
                {
                    m_.spanMin = m_.middle;
                    minSpanInputF.text = "" + m_.spanMin;

                    // update span min
                    minSpanSlider.value = (m_.spanMin - m_.minAmp) / diff;
                }

                if (m_.spanMax < m_.middle)
                {
                    m_.spanMax = m_.middle;
                    maxSpanInputF.text = "" + m_.spanMax;

                    // update span max
                    maxSpanSlider.value = (m_.spanMax - m_.minAmp) / diff;
                }

                // update middle
                middleSpanSlider.value = (m_.middle - m_.minAmp) / diff;
                m_scene.UpdateIEEGMiddle(m_.middle, m_columnId);

                update_histogram();
            });

            middleSpanSlider.onValueChanged.AddListener((value) =>
            {
                float diff = m_.maxAmp - m_.minAmp;
                m_.middle = m_.minAmp + value * diff;

                if (m_.middle < m_.spanMin)
                {
                    m_.middle = m_.spanMin;
                    middleSpanSlider.value = (m_.middle - m_.minAmp) / diff;
                }

                if (m_.middle > m_.spanMax)
                {
                    m_.middle = m_.spanMax;
                    middleSpanSlider.value = (m_.middle - m_.minAmp) / diff;
                }

                middleInputF.text = "" + m_.middle;
                m_scene.UpdateIEEGMiddle(m_.middle, m_columnId);

                update_histogram();
            });

            // max span            
            maxSpanInputF.onEndEdit.AddListener(delegate
            {
                if (maxSpanInputF.text.Length == 0)
                    maxSpanInputF.text = "0";
                else if (maxSpanInputF.text.Length == 1)
                {
                    if (maxSpanInputF.text[0] == '.')
                        maxSpanInputF.text = "0" + maxSpanInputF.text;

                    if (maxSpanInputF.text[0] == '-')
                        maxSpanInputF.text = "0";
                }

                m_.spanMax = float.Parse(maxSpanInputF.text);

                float diff = m_.maxAmp - m_.minAmp;
                if (m_.spanMax > m_.maxAmp)
                {
                    m_.spanMax = m_.maxAmp;
                    maxSpanInputF.text = "" + m_.spanMax;
                }
                if (m_.spanMax < m_.middle)
                {
                    m_.spanMax = m_.middle;
                    maxSpanInputF.text = "" + m_.spanMax;
                }

                maxSpanSlider.value = (m_.spanMax - m_.minAmp) / diff;
                m_scene.UpdateIEEGSpanMax(m_.spanMax, m_columnId);

                update_histogram();
            });

            maxSpanSlider.onValueChanged.AddListener((value) =>
            {
                float diff = m_.maxAmp - m_.minAmp;
                m_.spanMax = m_.minAmp + value * diff;

                if (m_.spanMax < m_.middle)
                {
                    m_.spanMax = m_.middle;
                    maxSpanSlider.value = (m_.spanMax - m_.minAmp) / diff;
                }

                maxSpanInputF.text = "" + m_.spanMax;
                m_scene.UpdateIEEGSpanMax(m_.spanMax, m_columnId);

                update_histogram();
            });


            // max distance
            InputField maxDistInputF = contentPanelT.Find("Inf dist parent").Find("Inf dist inputfield").GetComponent<InputField>();
            maxDistInputF.onEndEdit.AddListener(delegate
            {
                Regex regex = new Regex("-");
                maxDistInputF.text = regex.Replace(maxDistInputF.text, "");

                if (maxDistInputF.text.Length == 0)
                    maxDistInputF.text = "0";
                else if (maxDistInputF.text.Length >= 1)
                {
                    if (maxDistInputF.text[0] == '.')
                        maxDistInputF.text = "0" + maxDistInputF.text;
                }

                m_.maxDistance = float.Parse(maxDistInputF.text);
                if (m_.maxDistance > m_maxDistance)
                {
                    m_.maxDistance = m_maxDistance;
                    maxDistInputF.text = "" + m_maxDistance;
                }

                m_scene.UpdateSiteMaximumInfluence(m_.maxDistance, m_columnId);
            });

            // set alpha to all 
            Button setAlphaForAllColsButton = contentPanelT.Find("set alpha for all columns parent").Find("set alpha for all columns button").GetComponent<Button>();
            setAlphaForAllColsButton.onClick.AddListener(delegate
            {
                IEEGAlphaParameters iEEGAlphaParams;
                iEEGAlphaParams.alphaMin = minAlphaSlider.value;
                iEEGAlphaParams.alphaMax = maxAlphaSlider.value;
                iEEGAlphaParams.columnId = m_columnId;
                ApplyAlphaToTAllCols.Invoke(iEEGAlphaParams);
            });

            // set sites params to all 
            Button setSiteForAllColsButton = contentPanelT.Find("set sites params for all columns parent").Find("set sites params for all columns button").GetComponent<Button>();
            setSiteForAllColsButton.onClick.AddListener(delegate
            {
                IEEGSitesParameters iEEGSitesParams;
                iEEGSitesParams.gain = gainSlider.value;
                iEEGSitesParams.maxDistance = float.Parse(maxDistInputF.text);
                iEEGSitesParams.columnId = m_columnId;
                ApplySitesToTAllCols.Invoke(iEEGSitesParams);
            });

            // set threshold to all 
            Button setThresholdForAllColsButton = contentPanelT.Find("set threshold for all columns parent").Find("set threshold for all columns button").GetComponent<Button>();
            setThresholdForAllColsButton.onClick.AddListener(delegate
            {
                IEEGThresholdParameters iEEGThresholdParams;
                iEEGThresholdParams.minSpan = float.Parse(minSpanInputF.text);
                iEEGThresholdParams.middle = float.Parse(middleInputF.text);
                iEEGThresholdParams.maxSpan = float.Parse(maxSpanInputF.text);
                iEEGThresholdParams.columnId = m_columnId;
                ApplyThresholdToTAllCols.Invoke(iEEGThresholdParams);
            });

            // minimize/expand alpha
            Button alphaButton = contentPanelT.Find("Alpha parent").Find("Alpha button").GetComponent<Button>();
            alphaButton.onClick.AddListener(delegate
            {
                m_isAlphaMinimized = !m_isAlphaMinimized;
                contentPanelT.Find("Alpha parent").Find("expand image").gameObject.SetActive(m_isAlphaMinimized);
                contentPanelT.Find("Alpha parent").Find("minimize image").gameObject.SetActive(!m_isAlphaMinimized);
                contentPanelT.Find("Max alpha parent").gameObject.SetActive(!m_isAlphaMinimized);
                contentPanelT.Find("Min alpha parent").gameObject.SetActive(!m_isAlphaMinimized);
                contentPanelT.Find("set alpha for all columns parent").gameObject.SetActive(!m_isAlphaMinimized);
            });

            Button sitesParamsButton = contentPanelT.Find("Sites parent").Find("Sites button").GetComponent<Button>();
            sitesParamsButton.onClick.AddListener(delegate
            {
                m_isSitesMinimized = !m_isSitesMinimized;
                contentPanelT.Find("Sites parent").Find("expand image").gameObject.SetActive(m_isSitesMinimized);
                contentPanelT.Find("Sites parent").Find("minimize image").gameObject.SetActive(!m_isSitesMinimized);
                contentPanelT.Find("Gain plots parent").gameObject.SetActive(!m_isSitesMinimized);
                contentPanelT.Find("Inf dist parent").gameObject.SetActive(!m_isSitesMinimized);
                contentPanelT.Find("set sites params for all columns parent").gameObject.SetActive(!m_isSitesMinimized);
            });

            // minimize/expand threshold iEEG
            Button thresholdIEEGButton = contentPanelT.Find("Threshold iEEG parent").Find("Threshold iEEG button").GetComponent<Button>();
            thresholdIEEGButton.onClick.AddListener(delegate
            {
                m_isThresholdIEEGMinimized = !m_isThresholdIEEGMinimized;
                contentPanelT.Find("Threshold iEEG parent").Find("expand image").gameObject.SetActive(m_isThresholdIEEGMinimized);
                contentPanelT.Find("Threshold iEEG parent").Find("minimize image").gameObject.SetActive(!m_isThresholdIEEGMinimized);

                contentPanelT.Find("Min parent").gameObject.SetActive(!m_isThresholdIEEGMinimized);
                contentPanelT.Find("Max parent").gameObject.SetActive(!m_isThresholdIEEGMinimized);
                contentPanelT.Find("Min span parent").gameObject.SetActive(!m_isThresholdIEEGMinimized);
                contentPanelT.Find("Middle parent").gameObject.SetActive(!m_isThresholdIEEGMinimized);
                contentPanelT.Find("Max span parent").gameObject.SetActive(!m_isThresholdIEEGMinimized);

                contentPanelT.Find("Cal min iEEG slider").gameObject.SetActive(!m_isThresholdIEEGMinimized);
                contentPanelT.Find("Cal middle iEEG slider").gameObject.SetActive(!m_isThresholdIEEGMinimized);
                contentPanelT.Find("Cal max iEEG slider").gameObject.SetActive(!m_isThresholdIEEGMinimized);
                contentPanelT.Find("Histogram iEEG parent").gameObject.SetActive(!m_isThresholdIEEGMinimized);

                contentPanelT.Find("set threshold for all columns parent").gameObject.SetActive(!m_isThresholdIEEGMinimized);
            });

            switch (m_scene.Type)
            {
                case SceneType.SinglePatient:
                    contentPanelT.Find("ROI parent").gameObject.SetActive(false);
                    contentPanelT.Find("separation panel 0").gameObject.SetActive(false);
                    break;
                case SceneType.MultiPatients:
                    Button ROIButton = contentPanelT.Find("ROI parent").Find("ROI button").GetComponent<Button>();
                    ROIButton.onClick.AddListener(delegate
                    {
                        switch_ROI_menu();
                    });
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Update the iEEG histogram with a new image
        /// </summary>
        private void update_histogram()
        {
            Transform contentPanelT = transform.Find("panel");
            float spanMinFactor = contentPanelT.Find("Cal min iEEG slider").GetComponent<Slider>().value;
            float middleFactor = contentPanelT.Find("Cal middle iEEG slider").GetComponent<Slider>().value;
            float spanMaxFactor = contentPanelT.Find("Cal max iEEG slider").GetComponent<Slider>().value;

            HBP.Module3D.DLL.Texture.GenerateDistributionHistogram(m_scene.Column3DViewManager.ColumnsIEEG[m_columnId].IEEGValues, 4 * 110, 4 * 110, spanMinFactor, spanMaxFactor, middleFactor).UpdateTexture2D(m_iEEGHistogram);

            Image image = contentPanelT.Find("Histogram iEEG parent").Find("Histogram panel").GetComponent<Image>();
            Destroy(image.sprite);
            image.sprite = Sprite.Create(m_iEEGHistogram,
                   new Rect(0, 0, m_iEEGHistogram.width, m_iEEGHistogram.height),
                   new Vector2(0.5f, 0.5f), 400f);
        }            

        private void switch_ROI_menu()
        {
            Transform contentPanelT = transform.Find("panel");
            m_isROIMenuMinimized = !m_isROIMenuMinimized;
            contentPanelT.Find("ROI parent").Find("expand image").gameObject.SetActive(m_isROIMenuMinimized);
            contentPanelT.Find("ROI parent").Find("minimize image").gameObject.SetActive(!m_isROIMenuMinimized);
            UpdateROIMenuDisplay.Invoke(!m_isROIMenuMinimized);
        }


        #endregion
    }


    public class iEEGMenuController : MonoBehaviour, UICameraOverlay
    {
        #region Properties

        // scenes
        private Base3DScene m_scene = null; /**< associated scene */

        // canvas
        public Transform m_middlePanelT = null; /**< middle scene panel transform */
        List<GameObject> m_iEEGMenuList = new List<GameObject>(); /**< iEEG menu list */            

        private iEEGMenu m_currentMenu = null;

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
            m_scene = scene;

            // default initialization with one menu per scene
            m_iEEGMenuList.Add(generate_menu(0));

            // listeners
            m_scene.SendIEEGParameters.AddListener((iEEGDataParams) =>
            {
                m_iEEGMenuList[iEEGDataParams.columnId].GetComponent<iEEGMenu>().update_UI_iEEG_data_from_scene(iEEGDataParams);
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
            GameObject iEEGMenuGO = Instantiate(GlobalGOPreloaded.IEEGLeftMenu);
            iEEGMenuGO.AddComponent<iEEGMenu>();
            iEEGMenuGO.transform.SetParent(m_middlePanelT);

            iEEGMenu menu = iEEGMenuGO.GetComponent<iEEGMenu>();
            menu.init(m_scene, columndId);

            // set listeners 
            menu.CloseIEEGWindow.AddListener(() =>
            {
                switch_UI_visibility();
            });

            menu.ApplyAlphaToTAllCols.AddListener((alphaParams) =>
            {
                // update UI/scenes values for all others columns 
                int senderId = alphaParams.columnId;
                for (int ii = 0; ii < m_iEEGMenuList.Count; ++ii)
                {
                    if (ii == senderId)
                        continue;

                    alphaParams.columnId = ii;

                    // update UI values
                    m_iEEGMenuList[alphaParams.columnId].GetComponent<iEEGMenu>().update_UI_alpha_values(alphaParams);

                    // update scene values
                    m_scene.UpdateIEEGMinAlpha(alphaParams.alphaMin, alphaParams.columnId);
                    m_scene.UpdateIEEGMaxAlpha(alphaParams.alphaMax, alphaParams.columnId);
                }
            });

            menu.ApplySitesToTAllCols.AddListener((sitesParams) =>
            {
                // update UI/scenes values for all others columns 
                int senderId = sitesParams.columnId;
                for (int ii = 0; ii < m_iEEGMenuList.Count; ++ii)
                {
                    if (ii == senderId)
                        continue;

                    sitesParams.columnId = ii;

                    // update UI values
                    m_iEEGMenuList[sitesParams.columnId].GetComponent<iEEGMenu>().update_UI_gain_sites_values(sitesParams);

                    // update scene values
                    m_scene.UpdateBubblesGain(sitesParams.gain, sitesParams.columnId);
                    m_scene.UpdateSiteMaximumInfluence(sitesParams.maxDistance, sitesParams.columnId);
                }

            });

            menu.ApplyThresholdToTAllCols.AddListener((thresholdParams) =>
            {
                // update UI/scenes values for all others columns 
                List<bool> truncateWarnings = new List<bool>();
                int senderId = thresholdParams.columnId;
                for (int ii = 0; ii < m_iEEGMenuList.Count; ++ii)
                {
                    if (ii == senderId)
                        continue;

                    thresholdParams.columnId = ii;

                    // retrieve menu
                    iEEGMenu menuToUpdate = m_iEEGMenuList[thresholdParams.columnId].GetComponent<iEEGMenu>();

                    // check if values must be truncated
                    IEEGThresholdParameters truncatedValues = menuToUpdate.check_threshold_values(thresholdParams, truncateWarnings);

                    // update UI values
                    menuToUpdate.update_UI_threshold_values(truncatedValues);

                    // update scene values
                    m_scene.UpdateIEEGMiddle(truncatedValues.middle, thresholdParams.columnId);
                    m_scene.UpdateIEEGSpanMin(truncatedValues.minSpan, thresholdParams.columnId);
                    m_scene.UpdateIEEGSpanMax(truncatedValues.maxSpan, thresholdParams.columnId);
                }

                // check if a warning message must be displayed
                bool displayWarning = false;
                string warnigsCols = "";
                for (int ii = 0; ii < truncateWarnings.Count; ++ii)
                {
                    if (truncateWarnings[ii])
                    {
                        displayWarning = true;
                        warnigsCols += "" + ii + " ";
                    }
                }
                if (displayWarning)
                {
                    m_scene.DisplayScreenMessage("Parameters truncated by min/max values for columns : " + warnigsCols, 2f, 300, 100);
                }

            });

            menu.UpdateROIMenuDisplay.AddListener((enabled) =>
            {
                UpdateColROIMenuDisplay.Invoke(enabled, columndId);                
            });

            return iEEGMenuGO;
        }

        /// <summary>
        /// Update the UI visibility with the current mode
        /// </summary>
        /// <param name="mode"></param>
        public void UpdateByMode(Mode mode)
        {
            if (m_currentMenu == null)
                return;

            //bool menuDisplayed = m_displayMenu;
            bool menuDisplayed = m_currentMenu.m_isDisplayed;

            // define mode ui specifities
            switch (mode.IDMode)
            {
                case Mode.ModesId.NoPathDefined:
                    menuDisplayed = false;
                    break;
                case Mode.ModesId.MinPathDefined:
                    menuDisplayed = false;
                    break;
                case Mode.ModesId.TriErasing:
                    menuDisplayed = false;
                    break;
                case Mode.ModesId.Error:
                    menuDisplayed = false;
                    break;
            }

            if (m_scene.Type == SceneType.SinglePatient)
            {
                if (m_scene.SceneInformation.DisplayCCEPMode)
                    menuDisplayed = false;
            }


            // set the state of the menu
            set_UI_visibility(menuDisplayed);
        }

        /// <summary>
        /// Update the menu UI activity with the current parameters
        /// </summary>
        private void updateUI()
        {
            // hide all the menus
            for (int ii = 0; ii < m_iEEGMenuList.Count; ++ii)
                m_iEEGMenuList[ii].SetActive(false);

            m_currentMenu.gameObject.SetActive(m_currentMenu.m_isDisplayed);
        }


        /// <summary>
        /// Swith the UI visibility of the UI
        /// </summary>
        public void switch_UI_visibility()
        {
            m_currentMenu.m_isDisplayed = !m_currentMenu.m_isDisplayed;
            updateUI();
        }

        /// <summary>
        /// Set the visibility of the UI
        /// </summary>
        /// <param name="visible"></param>
        private void set_UI_visibility(bool visible)
        {
            m_currentMenu.m_isDisplayed = visible;
            updateUI();
        }

        /// <summary>
        /// Define the current column selected
        /// </summary>
        /// <param name="columnId"></param>
        public void define_current_column(int columnId = -1)
        {
            if (columnId >= m_iEEGMenuList.Count)
                return;

            m_currentMenu = m_iEEGMenuList[columnId].GetComponent<iEEGMenu>();
            updateUI();
        }

        /// <summary>
        /// Adapt the number of menues
        /// </summary>
        /// <param name="iEEGColumnsNb"></param>
        public void define_columns_nb(int iEEGColumnsNb)
        {
            int diff = m_iEEGMenuList.Count - iEEGColumnsNb;
            if (diff < 0) // add menus
                for (int ii = 0; ii < -diff; ++ii)
                    m_iEEGMenuList.Add(generate_menu(m_iEEGMenuList.Count));
            else if (diff > 0) // remove menus
            {
                for (int ii = 0; ii < diff; ++ii)
                {
                    int id = m_iEEGMenuList.Count - 1;
                    Destroy(m_iEEGMenuList[id]);
                    m_iEEGMenuList.RemoveAt(id);
                }
            }

            // define the current scene menu
            define_current_column(0);
        }

        #endregion

    }
}