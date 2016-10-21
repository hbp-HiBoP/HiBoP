

/**
 * \file    MinimizeController.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define MinimizeController class
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
    /// Controller for the minimize buttons in the columns
    /// </summary>
    public class MinimizeController : BothScenesOverlayController
    {
        #region members

        

        private Transform minimizeControllerOverlay = null;

        private List<GameObject> m_spMinimizeOverlayList = new List<GameObject>();
        private List<GameObject> m_mpMinimizeOverlayList = new List<GameObject>();

        private List<bool> m_spMinimizeStateList = new List<bool>();
        public List<bool> SPMinimizeStateList
        {
            get { return m_spMinimizeStateList; }
        }

        private List<bool> m_mpMinimizeStateList = new List<bool>();
        public List<bool> MPMinimizeStateList
        {
            get { return m_mpMinimizeStateList; }
        }

        private bool m_spIsActive = true;
        private bool m_mpIsActive = true;

        private int m_currentSpColumnsNb;
        private int m_currentMpColumnsNb;

        // events
        public SendBoolValueEvent m_minimizeStateSwitchEvent = new SendBoolValueEvent(); /**< event indicating thaht a column minimized state has changed (bool : spScene) */


        #endregion members

        #region others

        /// <summary>
        /// Init the controller
        /// </summary>
        /// <param name="scenesManager"></param>
        public new void init(ScenesManager scenesManager)
        {
            base.init(scenesManager);

            // associate transform canvas overlays
            GameObject overlayCanvas = new GameObject("minimize cameras");
            overlayCanvas.transform.SetParent(m_canvasOverlayParent);
            minimizeControllerOverlay = overlayCanvas.transform;

            updateSPMinimizeButtonNb(1);
            updateMPMinimizeButtonNb(1);
        }

        /// <summary>
        /// Update the UI with the current mode and activity
        /// </summary>
        public override void updateUI()
        {
            // sp
            if (currentSPMode != null)
                m_spIsActive = isVisibleFromSPScene && currentSPActivity;
  

            for (int ii = 0; ii < m_spMinimizeOverlayList.Count; ++ii)
                m_spMinimizeOverlayList[ii].SetActive(m_spIsActive);

            // mp
            if (currentMPMode != null)
                m_mpIsActive = isVisibleFromMPScene && currentMPActivity;

            for (int ii = 0; ii < m_mpMinimizeOverlayList.Count; ++ii)
                m_mpMinimizeOverlayList[ii].SetActive(m_mpIsActive);
        }


        /// <summary>
        /// Update the position of the UI in the scene
        /// </summary>
        public override void updateUIPosition()
        {
            int spMinimizedColumnsNb = 0;
            for (int ii = 0; ii < m_spMinimizeOverlayList.Count; ++ii)
            {
                if (m_camerasManager.getColumnsNumber(true) < ii)
                    break;

                RectTransform rectTransfoCamera = m_camerasManager.getCameraRect(true, ii, 0);
                Rect rectCamera = CamerasManager.GetScreenRect(rectTransfoCamera, m_backGroundCamera);

                RectTransform rectTransformMinimizeButton = m_spMinimizeOverlayList[ii].GetComponent<RectTransform>();
                rectTransformMinimizeButton.position = rectCamera.position + new Vector2(rectCamera.width, rectCamera.height);

                Vector2 size = m_spMinimizeOverlayList[ii].transform.Find("horizontal panel").GetComponent<RectTransform>().sizeDelta;
                size.x = rectCamera.width - 45;
                m_spMinimizeOverlayList[ii].transform.Find("horizontal panel").GetComponent<RectTransform>().sizeDelta = size;

                size = m_spMinimizeOverlayList[ii].transform.Find("vertical panel").GetComponent<RectTransform>().sizeDelta;

                int nbLines = m_camerasManager.getNumberOfViews(true);
                size.x = nbLines * rectCamera.height + (nbLines-1)*2 - 60;
                size.y = 20;
                m_spMinimizeOverlayList[ii].transform.Find("vertical panel").GetComponent<RectTransform>().sizeDelta = size;

                // count minimized columns
                if (m_spMinimizeStateList[ii])
                {
                    spMinimizedColumnsNb++;
                }

                m_spMinimizeOverlayList[ii].transform.Find("focus button").gameObject.SetActive(!m_spMinimizeStateList[ii]);
            }

            // disable minimization if only one column remains
            bool disactiveMinimization = false;
            if (m_spMinimizeStateList.Count - spMinimizedColumnsNb == 1)
            {
                disactiveMinimization = true;
            }
            for (int jj = 0; jj < m_spMinimizeOverlayList.Count; ++jj)
            {
                if (m_spMinimizeStateList[jj])
                {
                    m_spMinimizeOverlayList[jj].transform.Find("minimize_button").GetComponent<Button>().interactable = true;
                    m_spMinimizeOverlayList[jj].transform.Find("focus button").GetComponent<Button>().interactable = true;
                }
                else
                {
                    m_spMinimizeOverlayList[jj].transform.Find("minimize_button").GetComponent<Button>().interactable = !disactiveMinimization;
                    m_spMinimizeOverlayList[jj].transform.Find("focus button").GetComponent<Button>().interactable = !disactiveMinimization;
                }
            }

            int mpMinimizedColumnsNb = 0;
            for (int ii = 0; ii < m_mpMinimizeOverlayList.Count; ++ii)
            {
                if (m_camerasManager.getColumnsNumber(false) < ii)
                    break;

                RectTransform rectTransfoCamera = m_camerasManager.getCameraRect(false, ii, 0);
                Rect rectCamera = CamerasManager.GetScreenRect(rectTransfoCamera, m_backGroundCamera);

                RectTransform rectTransformMinimizeButton = m_mpMinimizeOverlayList[ii].GetComponent<RectTransform>();
                rectTransformMinimizeButton.position = rectCamera.position + new Vector2(rectCamera.width, rectCamera.height);

                Vector2 size = m_mpMinimizeOverlayList[ii].transform.Find("horizontal panel").GetComponent<RectTransform>().sizeDelta;
                size.x = rectCamera.width - 45;
                m_mpMinimizeOverlayList[ii].transform.Find("horizontal panel").GetComponent<RectTransform>().sizeDelta = size;

                size = m_mpMinimizeOverlayList[ii].transform.Find("vertical panel").GetComponent<RectTransform>().sizeDelta;

                int nbLines = m_camerasManager.getNumberOfViews(false);
                size.x = nbLines * rectCamera.height + (nbLines - 1) * 2 - 60;
                size.y = 20;
                m_mpMinimizeOverlayList[ii].transform.Find("vertical panel").GetComponent<RectTransform>().sizeDelta = size;

                // count minimized columns
                if (m_mpMinimizeStateList[ii])
                    mpMinimizedColumnsNb++;

                m_mpMinimizeOverlayList[ii].transform.Find("focus button").gameObject.SetActive(!m_mpMinimizeStateList[ii]);
            }

            // disable minimization if only one column remains
            disactiveMinimization = false;
            if (m_mpMinimizeStateList.Count - mpMinimizedColumnsNb == 1)
            {
                disactiveMinimization = true;
            }
            for (int jj = 0; jj < m_mpMinimizeOverlayList.Count; ++jj)
            {
                if (m_mpMinimizeStateList[jj])
                {
                    m_mpMinimizeOverlayList[jj].transform.Find("minimize_button").GetComponent<Button>().interactable = true;
                    m_mpMinimizeOverlayList[jj].transform.Find("focus button").GetComponent<Button>().interactable = true;
                }
                else
                {
                    m_mpMinimizeOverlayList[jj].transform.Find("minimize_button").GetComponent<Button>().interactable = !disactiveMinimization;
                    m_mpMinimizeOverlayList[jj].transform.Find("focus button").GetComponent<Button>().interactable = !disactiveMinimization;
                }
            }

        }


        /// <summary>
        /// Set the name of a column
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="idColumn"></param>
        /// <param name="name"></param>
        public void setColumnName(bool spScene, int idColumn, string name)
        {
            if (spScene)
            {
                if (idColumn >= m_spMinimizeOverlayList.Count)
                    return;

                m_spMinimizeOverlayList[idColumn].transform.Find("horizontal panel").Find("Text").gameObject.GetComponent<Text>().text = name;
                m_spMinimizeOverlayList[idColumn].transform.Find("vertical panel").Find("Text").gameObject.GetComponent<Text>().text = name;
            }
            else
            {
                if (idColumn >= m_mpMinimizeOverlayList.Count)
                    return;

                m_mpMinimizeOverlayList[idColumn].transform.Find("horizontal panel").Find("Text").gameObject.GetComponent<Text>().text = name;
                m_mpMinimizeOverlayList[idColumn].transform.Find("vertical panel").Find("Text").gameObject.GetComponent<Text>().text = name;
            }
        }


        /// <summary>
        /// Return the number of columns of the scene
        /// </summary>
        /// <param name="spScene"></param>
        /// <returns></returns>
        public int getColumnsNb(bool spScene)
        {
            if (spScene)
                return m_currentSpColumnsNb;

            return m_currentMpColumnsNb;
        }

        /// <summary>
        /// Return the rect transform of the ui for the input scene and column 
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="idButton"></param>
        /// <returns></returns>
        public RectTransform getMinimizeButtonRectT(bool spScene, int idButton)
        {
            if (spScene)
            {
                return m_spMinimizeOverlayList[idButton].transform.Find("minimize_button").GetComponent<RectTransform>();
            }

            return m_mpMinimizeOverlayList[idButton].transform.Find("minimize_button").GetComponent<RectTransform>();
        }

        /// <summary>
        /// Add a new SP button
        /// </summary>
        public void addSPMinimizeButton()
        {
            updateSPMinimizeButtonNb(m_currentSpColumnsNb + 1);
        }

        /// <summary>
        /// Add a new MP button
        /// </summary>
        public void addMPMinimizeButton()
        {
            updateMPMinimizeButtonNb(m_currentMpColumnsNb + 1);
        }

        /// <summary>
        /// Remove the last SP button
        /// </summary>
        public void removeSPMinimizeButton()
        {
            updateSPMinimizeButtonNb(m_currentSpColumnsNb - 1);
        }

        /// <summary>
        /// Remove the last MP button
        /// </summary>
        public void removeMPMinimizeButton()
        {
            updateMPMinimizeButtonNb(m_currentMpColumnsNb - 1);
        }


        private void setOverlayState(GameObject minimizeOverlay, bool state)
        {
            minimizeOverlay.transform.Find("minimize_button").Find("Text").GetComponent<Text>().text = state ? "+" : "-";
            minimizeOverlay.transform.Find("horizontal panel").gameObject.SetActive(!state);
            minimizeOverlay.transform.Find("vertical panel").gameObject.SetActive(state);
        }

        /// <summary>
        /// Update the number of buttons for the SP scene
        /// </summary>
        /// <param name="columnsNb"></param>
        public void updateSPMinimizeButtonNb(int columnsNb)
        {
            int diff = m_currentSpColumnsNb - columnsNb;

            if (diff < 0)
            {
                for (int ii = 0; ii < -diff; ++ii)
                {
                    m_spMinimizeStateList.Add(false);
                    m_spMinimizeOverlayList.Add(Instantiate(BaseGameObjects.MinimizeDisplay));
                    int id = m_spMinimizeOverlayList.Count - 1;
                    m_spMinimizeOverlayList[id].name = "sp_minimize_control_overlay_" + id;
                    m_spMinimizeOverlayList[id].transform.SetParent(minimizeControllerOverlay, false);
                    m_spMinimizeOverlayList[id].SetActive(m_spIsActive);

                    // add button listener                
                    Button minimizeButton = m_spMinimizeOverlayList[id].transform.Find("minimize_button").GetComponent<Button>();
                    minimizeButton.onClick.AddListener(delegate
                    {
                        m_spMinimizeStateList[id] = !m_spMinimizeStateList[id];

                        for (int jj = 0; jj < m_camerasManager.GetComponent<CamerasManager>().getNumberOfViews(true); ++jj)
                            m_camerasManager.GetComponent<CamerasManager>().getCamera(true, id, jj).setMinimizeState(m_spMinimizeStateList[id]);

                        setOverlayState(m_spMinimizeOverlayList[id], m_spMinimizeStateList[id]);
                        m_minimizeStateSwitchEvent.Invoke(true);
                    });

                    Button focusButton = m_spMinimizeOverlayList[id].transform.Find("focus button").GetComponent<Button>();
                    focusButton.onClick.AddListener(delegate
                    {
                        for (int jj = 0; jj < m_spMinimizeStateList.Count; ++jj)
                        {
                            m_spMinimizeStateList[jj] = (jj != id);
                           
                            for (int kk = 0; kk < m_camerasManager.GetComponent<CamerasManager>().getNumberOfViews(true); ++kk)
                                m_camerasManager.GetComponent<CamerasManager>().getCamera(true, jj, kk).setMinimizeState(m_spMinimizeStateList[jj]);

                            setOverlayState(m_spMinimizeOverlayList[jj], m_spMinimizeStateList[jj]);
                        }

                        m_minimizeStateSwitchEvent.Invoke(true);
                    });
                }
            }
            else
            {
                for (int ii = 0; ii < diff; ++ii)
                {
                    int id = m_spMinimizeStateList.Count - 1;
                    m_spMinimizeStateList.RemoveAt(id);
                    Destroy(m_spMinimizeOverlayList[id]);
                    m_spMinimizeOverlayList.RemoveAt(id);
                }
            }


            for (int ii = 0; ii < m_spMinimizeStateList.Count; ++ii)
            {
                for (int jj = 0; jj < m_camerasManager.GetComponent<CamerasManager>().getNumberOfViews(true); ++jj)
                {
                    m_camerasManager.GetComponent<CamerasManager>().getCamera(true, ii, jj).setMinimizeState(m_spMinimizeStateList[ii]);
                }
            }

            m_currentSpColumnsNb = columnsNb;
        }

        /// <summary>
        /// Update the number of buttons for the MP scene
        /// </summary>
        /// <param name="columnsNb"></param>
        public void updateMPMinimizeButtonNb(int columnsNb)
        {
            int diff = m_currentMpColumnsNb - columnsNb;

            if (diff < 0)
            {
                for (int ii = 0; ii < -diff; ++ii)
                {
                    m_mpMinimizeStateList.Add(false);
                    m_mpMinimizeOverlayList.Add(Instantiate(BaseGameObjects.MinimizeDisplay));
                    int id = m_mpMinimizeOverlayList.Count - 1;
                    m_mpMinimizeOverlayList[id].name = "mp_minimize_control_overlay_" + id;
                    m_mpMinimizeOverlayList[id].transform.SetParent(minimizeControllerOverlay, false);
                    m_mpMinimizeOverlayList[id].SetActive(m_mpIsActive);

                    // add button listener                
                    Button minimizeButton = m_mpMinimizeOverlayList[id].transform.Find("minimize_button").GetComponent<Button>();
                    minimizeButton.onClick.AddListener(delegate
                    {
                        m_mpMinimizeStateList[id] = !m_mpMinimizeStateList[id];

                        for (int jj = 0; jj < m_camerasManager.GetComponent<CamerasManager>().getNumberOfViews(false); ++jj)
                            m_camerasManager.GetComponent<CamerasManager>().getCamera(false, id, jj).setMinimizeState(m_mpMinimizeStateList[id]);

                        setOverlayState(m_mpMinimizeOverlayList[id], m_mpMinimizeStateList[id]);
                        m_minimizeStateSwitchEvent.Invoke(false);
                    });


                    Button focusButton = m_mpMinimizeOverlayList[id].transform.Find("focus button").GetComponent<Button>();
                    focusButton.onClick.AddListener(delegate
                    {
                        for (int jj = 0; jj < m_mpMinimizeStateList.Count; ++jj)
                        {
                            m_mpMinimizeStateList[jj] = (jj != id);
                            
                            for (int kk = 0; kk < m_camerasManager.GetComponent<CamerasManager>().getNumberOfViews(false); ++kk)
                                m_camerasManager.GetComponent<CamerasManager>().getCamera(false, jj, kk).setMinimizeState(m_mpMinimizeStateList[jj]);

                            setOverlayState(m_mpMinimizeOverlayList[jj], m_mpMinimizeStateList[jj]); 
                        }

                        m_minimizeStateSwitchEvent.Invoke(false);
                    });
                }
            }
            else
            {
                for (int ii = 0; ii < diff; ++ii)
                {
                    int id = m_mpMinimizeStateList.Count - 1;
                    m_mpMinimizeStateList.RemoveAt(id);
                    Destroy(m_mpMinimizeOverlayList[id]);
                    m_mpMinimizeOverlayList.RemoveAt(id);
                }
            }


            for (int ii = 0; ii < m_mpMinimizeStateList.Count; ++ii)
            {
                for (int jj = 0; jj < m_camerasManager.GetComponent<CamerasManager>().getNumberOfViews(false); ++jj)
                {
                    m_camerasManager.GetComponent<CamerasManager>().getCamera(false, ii, jj).setMinimizeState(m_mpMinimizeStateList[ii]);
                }
            }

            m_currentMpColumnsNb = columnsNb;
        }



        

        #endregion others
    }
}