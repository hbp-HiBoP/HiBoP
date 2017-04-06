

/**
 * \file    MinimizeController.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define MinimizeController class
 */

// system
using System.Collections.Generic;

// unity
using UnityEngine;
using UnityEngine.UI;

// hbp
using HBP.VISU3D.Cam;

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

            update_SP_minimize_button_nb(1);
            update_MP_minimize_button_nb(1);
        }

        /// <summary>
        /// Update the UI with the current mode and activity
        /// </summary>
        public override void update_UI()
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
        public override void update_UI_position()
        {
            int spMinimizedColumnsNb = 0;
            for (int ii = 0; ii < m_spMinimizeOverlayList.Count; ++ii)
            {
                if (m_camerasManager.GetNumberOfColumns(true) < ii)
                    break;

                RectTransform rectTransfoCamera = m_camerasManager.camera_rectangle(true, ii, 0);
                Rect rectCamera = CamerasManager.screen_rectangle(rectTransfoCamera, m_backGroundCamera);

                RectTransform rectTransformMinimizeButton = m_spMinimizeOverlayList[ii].GetComponent<RectTransform>();
                rectTransformMinimizeButton.position = rectCamera.position + new Vector2(rectCamera.width, rectCamera.height);

                Vector2 size = m_spMinimizeOverlayList[ii].transform.Find("horizontal panel").GetComponent<RectTransform>().sizeDelta;
                size.x = rectCamera.width - 45;
                m_spMinimizeOverlayList[ii].transform.Find("horizontal panel").GetComponent<RectTransform>().sizeDelta = size;

                size = m_spMinimizeOverlayList[ii].transform.Find("vertical panel").GetComponent<RectTransform>().sizeDelta;

                int nbLines = m_camerasManager.GetNumberOfViews(true);
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
                if (m_camerasManager.GetNumberOfColumns(false) < ii)
                    break;

                RectTransform rectTransfoCamera = m_camerasManager.camera_rectangle(false, ii, 0);
                Rect rectCamera = CamerasManager.screen_rectangle(rectTransfoCamera, m_backGroundCamera);

                RectTransform rectTransformMinimizeButton = m_mpMinimizeOverlayList[ii].GetComponent<RectTransform>();
                rectTransformMinimizeButton.position = rectCamera.position + new Vector2(rectCamera.width, rectCamera.height);

                Vector2 size = m_mpMinimizeOverlayList[ii].transform.Find("horizontal panel").GetComponent<RectTransform>().sizeDelta;
                size.x = rectCamera.width - 45;
                m_mpMinimizeOverlayList[ii].transform.Find("horizontal panel").GetComponent<RectTransform>().sizeDelta = size;

                size = m_mpMinimizeOverlayList[ii].transform.Find("vertical panel").GetComponent<RectTransform>().sizeDelta;

                int nbLines = m_camerasManager.GetNumberOfViews(false);
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
        public void set_column_name(bool spScene, int idColumn, string name)
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

        public int columns_nb(bool spScene)
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
        public RectTransform minimized_button_rectT(bool spScene, int idButton)
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
        public void add_SP_minimize_button()
        {
            update_SP_minimize_button_nb(m_currentSpColumnsNb + 1);
        }

        /// <summary>
        /// Add a new MP button
        /// </summary>
        public void add_MP_minimize_button()
        {
            update_MP_minimize_button_nb(m_currentMpColumnsNb + 1);
        }

        /// <summary>
        /// Remove the last SP button
        /// </summary>
        public void remove_SP_minimize_button()
        {
            update_SP_minimize_button_nb(m_currentSpColumnsNb - 1);
        }

        /// <summary>
        /// Remove the last MP button
        /// </summary>
        public void remove_MP_minimize_button()
        {
            update_MP_minimize_button_nb(m_currentMpColumnsNb - 1);
        }
        
        private void setçoverlay_state(GameObject minimizeOverlay, bool state)
        {
            minimizeOverlay.transform.Find("minimize_button").Find("Text").GetComponent<Text>().text = state ? "+" : "-";
            minimizeOverlay.transform.Find("horizontal panel").gameObject.SetActive(!state);
            minimizeOverlay.transform.Find("vertical panel").gameObject.SetActive(state);
        }

        /// <summary>
        /// Update the number of buttons for the SP scene
        /// </summary>
        /// <param name="columnsNb"></param>
        public void update_SP_minimize_button_nb(int columnsNb)
        {
            int diff = m_currentSpColumnsNb - columnsNb;

            if (diff < 0)
            {
                for (int ii = 0; ii < -diff; ++ii)
                {
                    m_spMinimizeStateList.Add(false);
                    m_spMinimizeOverlayList.Add(Instantiate(GlobalGOPreloaded.MinimizeDisplay));
                    int id = m_spMinimizeOverlayList.Count - 1;
                    m_spMinimizeOverlayList[id].name = "sp_minimize_control_overlay_" + id;
                    m_spMinimizeOverlayList[id].transform.SetParent(minimizeControllerOverlay, false);
                    m_spMinimizeOverlayList[id].SetActive(m_spIsActive);

                    // add button listener                
                    Button minimizeButton = m_spMinimizeOverlayList[id].transform.Find("minimize_button").GetComponent<Button>();
                    minimizeButton.onClick.AddListener(delegate
                    {
                        m_spMinimizeStateList[id] = !m_spMinimizeStateList[id];

                        for (int jj = 0; jj < m_camerasManager.GetComponent<CamerasManager>().GetNumberOfViews(true); ++jj)
                            m_camerasManager.GetComponent<CamerasManager>().get_camera(true, id, jj).set_minimized_state(m_spMinimizeStateList[id]);

                        setçoverlay_state(m_spMinimizeOverlayList[id], m_spMinimizeStateList[id]);
                        m_minimizeStateSwitchEvent.Invoke(true);
                    });

                    Button focusButton = m_spMinimizeOverlayList[id].transform.Find("focus button").GetComponent<Button>();
                    focusButton.onClick.AddListener(delegate
                    {
                        for (int jj = 0; jj < m_spMinimizeStateList.Count; ++jj)
                        {
                            m_spMinimizeStateList[jj] = (jj != id);
                           
                            for (int kk = 0; kk < m_camerasManager.GetComponent<CamerasManager>().GetNumberOfViews(true); ++kk)
                                m_camerasManager.GetComponent<CamerasManager>().get_camera(true, jj, kk).set_minimized_state(m_spMinimizeStateList[jj]);

                            setçoverlay_state(m_spMinimizeOverlayList[jj], m_spMinimizeStateList[jj]);
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
                for (int jj = 0; jj < m_camerasManager.GetComponent<CamerasManager>().GetNumberOfViews(true); ++jj)
                {
                    m_camerasManager.GetComponent<CamerasManager>().get_camera(true, ii, jj).set_minimized_state(m_spMinimizeStateList[ii]);
                }
            }

            m_currentSpColumnsNb = columnsNb;
        }

        /// <summary>
        /// Update the number of buttons for the MP scene
        /// </summary>
        /// <param name="columnsNb"></param>
        public void update_MP_minimize_button_nb(int columnsNb)
        {
            int diff = m_currentMpColumnsNb - columnsNb;

            if (diff < 0)
            {
                for (int ii = 0; ii < -diff; ++ii)
                {
                    m_mpMinimizeStateList.Add(false);
                    m_mpMinimizeOverlayList.Add(Instantiate(GlobalGOPreloaded.MinimizeDisplay));
                    int id = m_mpMinimizeOverlayList.Count - 1;
                    m_mpMinimizeOverlayList[id].name = "mp_minimize_control_overlay_" + id;
                    m_mpMinimizeOverlayList[id].transform.SetParent(minimizeControllerOverlay, false);
                    m_mpMinimizeOverlayList[id].SetActive(m_mpIsActive);

                    // add button listener                
                    Button minimizeButton = m_mpMinimizeOverlayList[id].transform.Find("minimize_button").GetComponent<Button>();
                    minimizeButton.onClick.AddListener(delegate
                    {
                        m_mpMinimizeStateList[id] = !m_mpMinimizeStateList[id];

                        for (int jj = 0; jj < m_camerasManager.GetComponent<CamerasManager>().GetNumberOfViews(false); ++jj)
                            m_camerasManager.GetComponent<CamerasManager>().get_camera(false, id, jj).set_minimized_state(m_mpMinimizeStateList[id]);

                        setçoverlay_state(m_mpMinimizeOverlayList[id], m_mpMinimizeStateList[id]);
                        m_minimizeStateSwitchEvent.Invoke(false);
                    });


                    Button focusButton = m_mpMinimizeOverlayList[id].transform.Find("focus button").GetComponent<Button>();
                    focusButton.onClick.AddListener(delegate
                    {
                        for (int jj = 0; jj < m_mpMinimizeStateList.Count; ++jj)
                        {
                            m_mpMinimizeStateList[jj] = (jj != id);
                            
                            for (int kk = 0; kk < m_camerasManager.GetComponent<CamerasManager>().GetNumberOfViews(false); ++kk)
                                m_camerasManager.GetComponent<CamerasManager>().get_camera(false, jj, kk).set_minimized_state(m_mpMinimizeStateList[jj]);

                            setçoverlay_state(m_mpMinimizeOverlayList[jj], m_mpMinimizeStateList[jj]); 
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
                for (int jj = 0; jj < m_camerasManager.GetComponent<CamerasManager>().GetNumberOfViews(false); ++jj)
                {
                    m_camerasManager.GetComponent<CamerasManager>().get_camera(false, ii, jj).set_minimized_state(m_mpMinimizeStateList[ii]);
                }
            }

            m_currentMpColumnsNb = columnsNb;
        }



        

        #endregion others
    }
}