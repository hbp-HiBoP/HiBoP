
/**
 * \file    iEEGMenuController.cs
 * \author  Lance Florian
 * \date    4/04/2016
 * \brief   Define iEEGMenuController,iEEGMenu classes
 */


// system
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace HBP.VISU3D
{
    namespace Events
    {
        /// <summary>
        /// Send the alpha params (params : alphaParams)
        /// </summary>
        public class ApplyAlphaToTAllCols : UnityEvent<iEEGAlphaParameters> { }

        /// <summary>
        /// Send the sites params (params : sitesParams)
        /// </summary>
        public class ApplySitesToTAllCols : UnityEvent<iEEGSitesParameters> { }

        /// <summary>
        /// Send the threshold params (params : thresholdParams)
        /// </summary>
        public class ApplyThresholdToTAllCols : UnityEvent<iEEGThresholdParameters> { }

        /// <summary>
        /// Sned a signal for closing the window
        /// </summary>
        public class CloseIEEGWindow : UnityEvent { }
    }

    /// <summary>
    /// iEEG UI menu
    /// </summary>
    public class iEEGMenu : MonoBehaviour
    {
        #region members

        // parameters
        private bool m_isAlphaMinimized = true;
        private bool m_isSitesMinimized = true;
        private bool m_isThresholdIEEGMinimized = true;

        private iEEGDataParameters m_;

        public int m_columnId = -1;
        public int m_maxDistance = 50;

        Texture2D m_iEEGHistogram = null;
        private Base3DScene m_scene = null; /**< SP scene */

        // events
        public Events.ApplyAlphaToTAllCols ApplyAlphaToTAllCols = new Events.ApplyAlphaToTAllCols();
        public Events.ApplySitesToTAllCols ApplySitesToTAllCols = new Events.ApplySitesToTAllCols();
        public Events.ApplyThresholdToTAllCols ApplyThresholdToTAllCols = new Events.ApplyThresholdToTAllCols();
        public Events.CloseIEEGWindow CloseIEEGWindow = new Events.CloseIEEGWindow();

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
            m_iEEGHistogram = new Texture2D(1, 1);

            // define name
            string name = "iEEG left menu ";
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
        /// Update the UI alpha values
        /// </summary>
        /// <param name="iEEGAlphaParams"></param>
        public void updateAlphaValues(iEEGAlphaParameters iEEGAlphaParams)
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
        public void updateSitesValues(iEEGSitesParameters iEEGSitesParams)
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
        public void updateThresholdValues(iEEGThresholdParameters iEEGThresholdParams)
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

            updateHistogram();
        }


        /// <summary>
        /// Return the truncated thresholds parameters for this menu, increment the warnings 
        /// </summary>
        /// <param name="iEEGThresholdParams"></param>
        /// <param name="warningsTruncation"></param>
        /// <returns></returns>
        public iEEGThresholdParameters checkThresholdValues(iEEGThresholdParameters iEEGThresholdParams, List<bool> warningsTruncation)
        {
            iEEGThresholdParameters thresholdParams = iEEGThresholdParams;

            bool warning = false;
            Transform contentPanelT = transform.Find("panel");

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
        public void updateIEEGDataFromScene(iEEGDataParameters iEEGDataParams)
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

            iEEGAlphaParameters alphaParams;
            alphaParams.alphaMin = iEEGDataParams.alphaMin;
            alphaParams.alphaMax = iEEGDataParams.alphaMax;
            alphaParams.columnId = iEEGDataParams.columnId;
            updateAlphaValues(alphaParams);

            iEEGSitesParameters sitesParams;
            sitesParams.gain = iEEGDataParams.gain;
            sitesParams.maxDistance = iEEGDataParams.maxDistance;
            sitesParams.columnId = iEEGDataParams.columnId;
            updateSitesValues(sitesParams);

            iEEGThresholdParameters thresholdsParams;
            thresholdsParams.minSpan = iEEGDataParams.spanMin;
            thresholdsParams.middle = iEEGDataParams.middle;
            thresholdsParams.maxSpan = iEEGDataParams.spanMax;
            thresholdsParams.columnId = iEEGDataParams.columnId;
            updateThresholdValues(thresholdsParams);
        }

        /// <summary>
        /// Define the listeneras associated to the menu
        /// </summary>
        private void setListeners()
        {
            Button closeButton = transform.Find("title panel").Find("close button").GetComponent<Button>();
            closeButton.onClick.AddListener(() =>
            {
                CloseIEEGWindow.Invoke();
            });

            Transform contentPanelT = transform.Find("panel");

            Text minText = contentPanelT.Find("Min parent").Find("Min value text").GetComponent<Text>();
            Text maxText = contentPanelT.Find("Max parent").Find("Max value text").GetComponent<Text>();

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
            minAlphaSlider.onValueChanged.AddListener((value) =>{ m_.alphaMin = value; m_scene.updateMinAlpha(value, m_columnId); });            
            maxAlphaSlider.onValueChanged.AddListener((value) =>{ m_.alphaMax = value; m_scene.updateMaxAlpha(value, m_columnId); });

            // gain bubble            
            gainSlider.onValueChanged.AddListener((value) => { m_.gain = value; m_scene.updateGainBubbles(value, m_columnId); });            

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
                m_scene.updateSpanMin(m_.spanMin, m_columnId);

                updateHistogram();
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
                m_scene.updateSpanMin(m_.spanMin, m_columnId);

                updateHistogram();
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
                m_scene.updateMiddle(m_.middle, m_columnId);

                updateHistogram();
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
                m_scene.updateMiddle(m_.middle, m_columnId);

                updateHistogram();
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
                m_scene.updateSpanMax(m_.spanMax, m_columnId);

                updateHistogram();
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
                m_scene.updateSpanMax(m_.spanMax, m_columnId);

                updateHistogram();
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

                m_scene.updateMaxDistance(m_.maxDistance, m_columnId);
            });

            // set alpha to all 
            Button setAlphaForAllColsButton = contentPanelT.Find("set alpha for all columns parent").Find("set alpha for all columns button").GetComponent<Button>();
            setAlphaForAllColsButton.onClick.AddListener(delegate
            {
                iEEGAlphaParameters iEEGAlphaParams;
                iEEGAlphaParams.alphaMin = minAlphaSlider.value;
                iEEGAlphaParams.alphaMax = maxAlphaSlider.value;
                iEEGAlphaParams.columnId = m_columnId;
                ApplyAlphaToTAllCols.Invoke(iEEGAlphaParams);
            });

            // set sites params to all 
            Button setSiteForAllColsButton = contentPanelT.Find("set sites params for all columns parent").Find("set sites params for all columns button").GetComponent<Button>();
            setSiteForAllColsButton.onClick.AddListener(delegate
            {
                iEEGSitesParameters iEEGSitesParams;
                iEEGSitesParams.gain = gainSlider.value;
                iEEGSitesParams.maxDistance = float.Parse(maxDistInputF.text);
                iEEGSitesParams.columnId = m_columnId;
                ApplySitesToTAllCols.Invoke(iEEGSitesParams);
            });

            // set threshold to all 
            Button setThresholdForAllColsButton = contentPanelT.Find("set threshold for all columns parent").Find("set threshold for all columns button").GetComponent<Button>();
            setThresholdForAllColsButton.onClick.AddListener(delegate
            {
                iEEGThresholdParameters iEEGThresholdParams;
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
        }

        /// <summary>
        /// Update the iEEG histogram with a new image
        /// </summary>
        private void updateHistogram()
        {
            Transform contentPanelT = transform.Find("panel");
            float spanMinFactor = contentPanelT.Find("Cal min iEEG slider").GetComponent<Slider>().value;
            float middleFactor = contentPanelT.Find("Cal middle iEEG slider").GetComponent<Slider>().value;
            float spanMaxFactor = contentPanelT.Find("Cal max iEEG slider").GetComponent<Slider>().value;

            Destroy(m_iEEGHistogram);
            m_iEEGHistogram = DLL.Texture.generateDistributionHistogram(m_scene.CM.IEEGCol(m_columnId).amplitudes, 110, 110, spanMinFactor, spanMaxFactor, middleFactor).getTexture2D();
            //m_iEEGHistogram = DLL.Texture.generateDistributionHistogram(m_scene.CM.DLLVolume, 110, 110, spanMinFactor, spanMaxFactor, middleFactor).getTexture2D();

            Image image = contentPanelT.Find("Histogram iEEG parent").Find("Histogram panel").GetComponent<Image>();

            Destroy(image.sprite);
            image.sprite = Sprite.Create(m_iEEGHistogram,
                   new Rect(0, 0, m_iEEGHistogram.width, m_iEEGHistogram.height),
                   new Vector2(0.5f, 0.5f));
        }            

        #endregion functions
    }


    public class iEEGMenuController : MonoBehaviour, UICameraOverlay
    {
        #region members

        // scenes
        private Base3DScene m_spScene = null; /**< SP scene */
        private Base3DScene m_mpScene = null; /**< MP scene */

        // canvas
        public Transform m_middlePanelT = null; /**< middle scene panel transform */
        List<GameObject> m_spIEEGMenuList = null; /**< SP iEEG menu list */
        List<GameObject> m_mpIEEGMenuList = null; /**< MP iEEG menu list */

        private bool m_spSettings = true; /**< is current menu from the sp scene ? */
        private bool m_displayMenu = false; /**< is a menu displayed ? */

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
            m_spScene = scenesManager.SPScene;
            m_mpScene = scenesManager.MPScene;

            // init lists of UI
            m_spIEEGMenuList = new List<GameObject>();
            m_mpIEEGMenuList = new List<GameObject>();

            // default initialization with one menu per scene
            m_spIEEGMenuList.Add(generateMenu(m_spScene, 0));
            m_mpIEEGMenuList.Add(generateMenu(m_mpScene, 0));

            // listeners
            m_spScene.SendIEEGParameters.AddListener((iEEGDataParams) =>
            {
                m_spIEEGMenuList[iEEGDataParams.columnId].GetComponent<iEEGMenu>().updateIEEGDataFromScene(iEEGDataParams);
            });

            m_mpScene.SendIEEGParameters.AddListener((iEEGDataParams) =>
            {
                m_mpIEEGMenuList[iEEGDataParams.columnId].GetComponent<iEEGMenu>().updateIEEGDataFromScene(iEEGDataParams);
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
            GameObject iEEGMenuGO = Instantiate(BaseGameObjects.IEEGLeftMenu);
            iEEGMenuGO.AddComponent<iEEGMenu>();
            iEEGMenuGO.transform.SetParent(m_middlePanelT.Find("iEEG left menu list"));

            iEEGMenu menu = iEEGMenuGO.GetComponent<iEEGMenu>();
            menu.init(scene, columndId);

            // set listeners 
            menu.CloseIEEGWindow.AddListener(() =>
            {
                switchUIVisibility();
            });

            menu.ApplyAlphaToTAllCols.AddListener((alphaParams) =>
            {
                // update UI/scenes values for all others columns 
                int senderId = alphaParams.columnId;
                List<GameObject> menuList = scene.singlePatient ? m_spIEEGMenuList : m_mpIEEGMenuList;
                for (int ii = 0; ii < menuList.Count; ++ii)
                {
                    if (ii == senderId)
                        continue;

                    alphaParams.columnId = ii;

                    // update UI values
                    menuList[alphaParams.columnId].GetComponent<iEEGMenu>().updateAlphaValues(alphaParams);

                    // update scene values
                    scene.updateMinAlpha(alphaParams.alphaMin, alphaParams.columnId);
                    scene.updateMaxAlpha(alphaParams.alphaMax, alphaParams.columnId);
                }
            });

            menu.ApplySitesToTAllCols.AddListener((sitesParams) =>
            {
                // update UI/scenes values for all others columns 
                int senderId = sitesParams.columnId;
                List<GameObject> menuList = scene.singlePatient ? m_spIEEGMenuList : m_mpIEEGMenuList;
                for (int ii = 0; ii < menuList.Count; ++ii)
                {
                    if (ii == senderId)
                        continue;

                    sitesParams.columnId = ii;

                    // update UI values
                    menuList[sitesParams.columnId].GetComponent<iEEGMenu>().updateSitesValues(sitesParams);

                    // update scene values
                    scene.updateGainBubbles(sitesParams.gain, sitesParams.columnId);
                    scene.updateMaxDistance(sitesParams.maxDistance, sitesParams.columnId);
                }

            });

            menu.ApplyThresholdToTAllCols.AddListener((thresholdParams) =>
            {
                // update UI/scenes values for all others columns 
                List<bool> truncateWarnings = new List<bool>();
                int senderId = thresholdParams.columnId;
                List<GameObject> menuList = scene.singlePatient ? m_spIEEGMenuList : m_mpIEEGMenuList;
                for (int ii = 0; ii < menuList.Count; ++ii)
                {
                    if (ii == senderId)
                        continue;

                    thresholdParams.columnId = ii;

                    // retrieve menu
                    iEEGMenu menuToUpdate = menuList[thresholdParams.columnId].GetComponent<iEEGMenu>();

                    // check if values must be truncated
                    iEEGThresholdParameters truncatedValues = menuToUpdate.checkThresholdValues(thresholdParams, truncateWarnings);

                    // update UI values
                    menuToUpdate.updateThresholdValues(truncatedValues);

                    // update scene values
                    scene.updateMiddle(truncatedValues.middle, thresholdParams.columnId);
                    scene.updateSpanMin(truncatedValues.minSpan, thresholdParams.columnId);                    
                    scene.updateSpanMax(truncatedValues.maxSpan, thresholdParams.columnId);
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
                    scene.displayScreenMessage("Parameters truncated by min/max values for columns : " + warnigsCols, 2f, 300, 100);
                }

            });


            return iEEGMenuGO;
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
                    menuDisplayed = false;
                    break;
                case Mode.ModesId.TriErasing:
                    menuDisplayed = false;
                    break;
                case Mode.ModesId.Error:
                    menuDisplayed = false;
                    break;
            }

            if (mode.m_sceneSp)
            {
                if (m_spScene.data_.displayLatenciesMode)
                    menuDisplayed = false;
            }


            // set the state of the menu
            setUIVisibility(menuDisplayed);
        }

        /// <summary>
        /// Update the menu UI activity with the current parameters
        /// </summary>
        private void updateUI()
        {
            // hide all the menus
            for (int ii = 0; ii < m_spIEEGMenuList.Count; ++ii)
                m_spIEEGMenuList[ii].SetActive(false);

            for (int ii = 0; ii < m_mpIEEGMenuList.Count; ++ii)
                m_mpIEEGMenuList[ii].SetActive(false);

            // display the menu corresponding to the current scene and column
            if (m_displayMenu)
            {
                if (m_spSettings)
                {
                    if (m_spID < m_spIEEGMenuList.Count) // if not IRMF column
                        m_spIEEGMenuList[m_spID].SetActive(true);
                }
                else
                {
                    if (m_mpID < m_mpIEEGMenuList.Count) // if not IRMF column
                        m_mpIEEGMenuList[m_mpID].SetActive(true);
                }
            }
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
                if (m_spSettings)
                {
                    m_spID = columnId;
                }
                else
                {
                    m_mpID = columnId;
                }
            }

            updateUI();
        }

        /// <summary>
        /// Adapt the number of menues
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="iEEGColumnsNb"></param>
        public void defineColumnsNb(bool spScene, int iEEGColumnsNb)
        {
            List<GameObject> menuList = spScene ? m_spIEEGMenuList : m_mpIEEGMenuList;
            Base3DScene scene = spScene ? m_spScene : m_mpScene;

            int diff = menuList.Count - iEEGColumnsNb;
            if (diff < 0) // add menus
                for (int ii = 0; ii < -diff; ++ii)
                    menuList.Add(generateMenu(scene, menuList.Count));
            else if (diff > 0) // remove menus
            {
                for (int ii = 0; ii < diff; ++ii)
                {
                    int id = menuList.Count - 1;
                    Destroy(menuList[id]);
                    menuList.RemoveAt(id);
                }
            }

            // define the current scene menu
            defineCurrentMenu(spScene, 0);
        }

        #endregion functions

    }
}