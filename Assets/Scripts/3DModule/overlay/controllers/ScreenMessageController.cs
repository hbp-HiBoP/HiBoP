

/**
 * \file    ScreenMessageController.cs
 * \author  Lance Florian
 * \date    03/05/2016
 * \brief   Define ScreenMessageController class
 */

// system
using System.Collections;
using System.Collections.Generic;

// unity
using UnityEngine;
using UnityEngine.UI;

// hbp
using HBP.VISU3D.Cam;
using System;

namespace HBP.VISU3D
{
    /// <summary>
    /// Controller for the screen messages display
    /// </summary>
    public class ScreenMessageController : BothScenesOverlayController
    {
        #region members

        private GameObject m_messageScreenDisplay = null;
        private GameObject m_spScreenMessage = null;
        private GameObject m_mpScreenMessage = null;

        private GameObject m_spScreenProgressBar = null;
        private GameObject m_mpScreenProgressBar = null;

        private Text m_spScreenText = null;
        private Text m_mpScreenText = null;

        private ProgressBar m_spProgressBar = null;
        private ProgressBar m_mpProgressBar = null;

        private bool m_displaySpMessage = false;
        private bool m_displayMpMessage = false;

        private bool m_displaySpProgessBar = false;
        private bool m_displayMpProgessBar = false;

        private float m_spMDuration;
        private float m_mpMDuration;
        private Vector2 m_spMessageSize;
        private Vector2 m_mpMessageSize;

        private float m_spPDuration;
        private float m_mpPDuration;
        private Vector2 m_spProgressBarSize;
        private Vector2 m_mpProgressBarSize;

        private double spMessageT;
        private double mpMessageT;

        private double spProgressBarT;
        private double mpProgressBarT;

        #endregion members

        #region mono_behaviour

        // ...

        #endregion mono_behaviour

        #region others

        /// <summary>
        /// Init the controller
        /// </summary>
        /// <param name="camerasManager"></param>
        public new void init(ScenesManager scenesManager)
        {
            base.init(scenesManager);

            m_messageScreenDisplay = new GameObject("screen message display");
            m_messageScreenDisplay.SetActive(true);
            m_messageScreenDisplay.transform.SetParent(m_canvasOverlayParent);

            m_spScreenMessage = Instantiate(BaseGameObjects.ScreenMessage);
            m_spScreenMessage.name = "sp screen message panel";
            m_spScreenMessage.transform.SetParent(m_messageScreenDisplay.transform);
            m_spScreenMessage.SetActive(false);
            m_spScreenText = m_spScreenMessage.transform.Find("Text").GetComponent<Text>();

            m_mpScreenMessage = Instantiate(BaseGameObjects.ScreenMessage);
            m_mpScreenMessage.name = "mp screen message panel";
            m_mpScreenMessage.transform.SetParent(m_messageScreenDisplay.transform);
            m_mpScreenMessage.SetActive(false);
            m_mpScreenText = m_mpScreenMessage.transform.Find("Text").GetComponent<Text>();

            m_spScreenProgressBar = Instantiate(BaseGameObjects.ScreenProgressBar);
            m_spScreenProgressBar.name = "sp screen loading panel";
            m_spScreenProgressBar.transform.SetParent(m_messageScreenDisplay.transform);
            m_spScreenProgressBar.SetActive(false);
            m_spProgressBar = m_spScreenProgressBar.GetComponent<ProgressBar>();
            m_spProgressBar.init();

            m_mpScreenProgressBar = Instantiate(BaseGameObjects.ScreenProgressBar);
            m_mpScreenProgressBar.name = "mp screen loading panel";
            m_mpScreenProgressBar.transform.SetParent(m_messageScreenDisplay.transform);
            m_mpScreenProgressBar.SetActive(false);
            m_mpProgressBar = m_mpScreenProgressBar.GetComponent<ProgressBar>();
            m_mpProgressBar.init();

            m_spScene.DisplayScreenMessage.AddListener((message, duration, width, height) =>
            {
                stop(true);
                m_spScreenText.text = message;
                m_spMDuration = duration;
                m_spMessageSize = new Vector2(width, height);

                spMessageT = TimeExecution.getWorldTime();
                StartCoroutine(displayMessage(true));
            });

            m_spScene.DisplaySceneProgressBar.AddListener((duration, width, height, value) =>
            {
                stop(true, true);
                m_spPDuration = duration;
                m_spProgressBarSize = new Vector2(width, height);
                m_spProgressBar.setProgessBarState(value);

                spProgressBarT = TimeExecution.getWorldTime();
                StartCoroutine(displayProgress(true));
            });

            m_mpScene.DisplayScreenMessage.AddListener((message, duration, width, height) =>
            {
                stop(false);
                m_mpScreenText.text = message;
                m_mpMDuration = duration;
                m_mpMessageSize = new Vector2(width, height);

                mpMessageT = TimeExecution.getWorldTime();
                StartCoroutine(displayMessage(false));
            });

            m_mpScene.DisplaySceneProgressBar.AddListener((duration, width, height, value) =>
            {
                stop(false, true);
                m_mpPDuration = duration;
                m_mpProgressBarSize = new Vector2(width, height);
                m_mpProgressBar.setProgessBarState(value);

                mpProgressBarT = TimeExecution.getWorldTime();
                StartCoroutine(displayProgress(false));
            });
        }

        /// <summary>
        /// Update the UI with the current mode and activity
        /// </summary>
        public override void updateUI()
        {
            m_messageScreenDisplay.SetActive(isVisibleFromSPScene || isVisibleFromMPScene);
        }
   
        /// <summary>
        /// Update the position of the UI in the scene
        /// </summary>
        public override void updateUIPosition()
        {
            if (m_displaySpMessage)
            {
                Rect rectCamera = CamerasManager.GetScreenRect(m_camerasManager.getSceneRectT(true), m_backGroundCamera);
                RectTransform rectT = m_spScreenMessage.GetComponent<RectTransform>();
                rectT.position = rectCamera.position + new Vector2(rectCamera.width/2,rectCamera.height/2);
                rectT.sizeDelta = m_spMessageSize;
            }

            if (m_displaySpProgessBar)
            {
                Rect rectCamera = CamerasManager.GetScreenRect(m_camerasManager.getSceneRectT(true), m_backGroundCamera);
                RectTransform rectT = m_spScreenProgressBar.GetComponent<RectTransform>();
                rectT.position = rectCamera.position + new Vector2(rectCamera.width / 2, rectCamera.height / 2 - m_spMessageSize.y);
                rectT.sizeDelta = m_spProgressBarSize;
            }

            if (m_displayMpMessage)
            {
                Rect rectCamera = CamerasManager.GetScreenRect(m_camerasManager.getSceneRectT(false), m_backGroundCamera);
                RectTransform rectT = m_mpScreenMessage.GetComponent<RectTransform>();
                rectT.position = rectCamera.position + new Vector2(rectCamera.width / 2, rectCamera.height / 2);
                rectT.sizeDelta = m_mpMessageSize;
            }
            if (m_displayMpProgessBar)
            {
                Rect rectCamera = CamerasManager.GetScreenRect(m_camerasManager.getSceneRectT(false), m_backGroundCamera);
                RectTransform rectT = m_mpScreenProgressBar.GetComponent<RectTransform>();
                rectT.position = rectCamera.position + new Vector2(rectCamera.width / 2, rectCamera.height / 2 - m_mpMessageSize.y);
                rectT.sizeDelta = m_mpProgressBarSize;
            }


        }

        /// <summary>
        /// Hide the message
        /// </summary>
        /// <param name="spScene"></param>
        public void stop(bool spScene, bool progressBar = false)
        {
            if (spScene)
            {
                if (!progressBar)
                    m_spMDuration = 0f;
                else
                    m_spPDuration = 0f;
            }
            else
            {
                if (!progressBar)
                    m_mpMDuration = 0f;
                else
                    m_mpPDuration = 0f;
            }
        }

        /// <summary>
        /// Display a new message
        /// </summary>
        /// <param name="spScene"></param>
        /// <returns></returns>
        IEnumerator displayMessage(bool spScene)
        {
            if (spScene)
            {
                m_displaySpMessage = true;
                m_spScreenMessage.SetActive(isVisibleFromSPScene);

                bool notEnd = true;
                while (notEnd)
                {
                    if (TimeExecution.getWorldTime() - spMessageT > m_spMDuration)
                        break;

                    yield return new WaitForSeconds(0.05f);
                }

                m_displaySpMessage = false;
                m_spScreenMessage.SetActive(false);
            }
            else
            {
                m_displayMpMessage = true;
                m_mpScreenMessage.SetActive(isVisibleFromMPScene);

                bool notEnd = true;
                while (notEnd)
                {
                    if (TimeExecution.getWorldTime() - mpMessageT > m_mpMDuration)
                        break;

                    yield return new WaitForSeconds(0.05f);
                }


                m_displayMpMessage = false;
                m_mpScreenMessage.SetActive(false);
            }
        }

        IEnumerator displayProgress(bool spScene)
        {
            if (spScene)
            {
                m_displaySpProgessBar = true;
                m_spScreenProgressBar.SetActive(isVisibleFromSPScene);

                bool notEnd = true;
                while (notEnd)
                {
                    if (TimeExecution.getWorldTime() - spProgressBarT > m_spPDuration)
                        break;

                    yield return new WaitForSeconds(0.05f);
                }

                m_displaySpProgessBar= false;
                m_spScreenProgressBar.SetActive(false);
            }
            else
            {
                m_displayMpProgessBar = true;
                m_mpScreenProgressBar.SetActive(isVisibleFromMPScene);

                bool notEnd = true;
                while (notEnd)
                {
                    if (TimeExecution.getWorldTime() - mpProgressBarT > m_mpPDuration)
                        break;

                    yield return new WaitForSeconds(0.05f);
                }

                m_displayMpProgessBar = false;
                m_mpScreenProgressBar.SetActive(false);
            }
        }

        #endregion others
    }
}