

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
using HBP.Module3D.Cam;

namespace HBP.Module3D
{
    /// <summary>
    /// Controller for the minimize buttons in the columns
    /// </summary>
    public class MinimizeController : BothScenesOverlayController
    {
        #region Properties

        

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


        #endregion

        #region Public Methods

        /// <summary>
        /// Init the controller
        /// </summary>
        /// <param name="scenesManager"></param>
        public new void init(ScenesManager scenesManager)
        {
            base.Initialize(scenesManager);

            // associate transform canvas overlays
            GameObject overlayCanvas = new GameObject("minimize cameras");
            overlayCanvas.transform.SetParent(transform);
            minimizeControllerOverlay = overlayCanvas.transform;

            update_SP_minimize_button_nb(1);
            update_MP_minimize_button_nb(1);
        }

        /// <summary>
        /// Update the UI with the current mode and activity
        /// </summary>
        public override void UpdateUI()
        {
            // sp
            if (m_CurrentSinglePatientMode != null)
                m_spIsActive = m_IsVisibleFromSinglePatientScene && m_CurrentSinglePatientActivity;
  

            for (int ii = 0; ii < m_spMinimizeOverlayList.Count; ++ii)
                m_spMinimizeOverlayList[ii].SetActive(m_spIsActive);

            // mp
            if (m_CurrentMultiPatientsMode != null)
                m_mpIsActive = m_IsVisibleFromMultiPatientsScene && m_CurrentMultiPatientsActivity;

            for (int ii = 0; ii < m_mpMinimizeOverlayList.Count; ++ii)
                m_mpMinimizeOverlayList[ii].SetActive(m_mpIsActive);
        }


        /// <summary>
        /// Update the position of the UI in the scene
        /// </summary>
        public override void UpdatePosition()
        {
            int spMinimizedColumnsNb = 0;
            for (int ii = 0; ii < m_spMinimizeOverlayList.Count; ++ii)
            {
                if (m_CamerasManager.GetNumberOfColumns(SceneType.SinglePatient) < ii)
                    break;

                RectTransform rectTransfoCamera = m_CamerasManager.GetCameraRectTransform(SceneType.SinglePatient, ii, 0);
                Rect rectCamera = rectTransfoCamera.rect;

                RectTransform rectTransformMinimizeButton = m_spMinimizeOverlayList[ii].GetComponent<RectTransform>();
                rectTransformMinimizeButton.position = rectCamera.position + new Vector2(rectCamera.width, rectCamera.height);

                Vector2 size = m_spMinimizeOverlayList[ii].transform.Find("horizontal panel").GetComponent<RectTransform>().sizeDelta;
                size.x = rectCamera.width - 45;
                m_spMinimizeOverlayList[ii].transform.Find("horizontal panel").GetComponent<RectTransform>().sizeDelta = size;

                size = m_spMinimizeOverlayList[ii].transform.Find("vertical panel").GetComponent<RectTransform>().sizeDelta;

                int nbLines = m_CamerasManager.GetNumberOfLines(SceneType.SinglePatient);
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
                if (m_CamerasManager.GetNumberOfColumns(SceneType.MultiPatients) < ii)
                    break;

                RectTransform rectTransfoCamera = m_CamerasManager.GetCameraRectTransform(SceneType.MultiPatients, ii, 0);
                Rect rectCamera = rectTransfoCamera.rect;

                RectTransform rectTransformMinimizeButton = m_mpMinimizeOverlayList[ii].GetComponent<RectTransform>();
                rectTransformMinimizeButton.position = rectCamera.position + new Vector2(rectCamera.width, rectCamera.height);

                Vector2 size = m_mpMinimizeOverlayList[ii].transform.Find("horizontal panel").GetComponent<RectTransform>().sizeDelta;
                size.x = rectCamera.width - 45;
                m_mpMinimizeOverlayList[ii].transform.Find("horizontal panel").GetComponent<RectTransform>().sizeDelta = size;

                size = m_mpMinimizeOverlayList[ii].transform.Find("vertical panel").GetComponent<RectTransform>().sizeDelta;

                int nbLines = m_CamerasManager.GetNumberOfLines(SceneType.MultiPatients);
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
        public void set_column_name(SceneType type, int idColumn, string name)
        {
            switch (type)
            {
                case SceneType.SinglePatient:
                    if (idColumn >= m_spMinimizeOverlayList.Count)
                        return;

                    m_spMinimizeOverlayList[idColumn].transform.Find("horizontal panel").Find("Text").gameObject.GetComponent<Text>().text = name;
                    m_spMinimizeOverlayList[idColumn].transform.Find("vertical panel").Find("Text").gameObject.GetComponent<Text>().text = name;
                    break;
                case SceneType.MultiPatients:
                    if (idColumn >= m_mpMinimizeOverlayList.Count)
                        return;

                    m_mpMinimizeOverlayList[idColumn].transform.Find("horizontal panel").Find("Text").gameObject.GetComponent<Text>().text = name;
                    m_mpMinimizeOverlayList[idColumn].transform.Find("vertical panel").Find("Text").gameObject.GetComponent<Text>().text = name;
                    break;
                default:
                    break;
            }
        }

        public int columns_nb(SceneType type)
        {
            switch (type)
            {
                case SceneType.SinglePatient:
                    return m_currentSpColumnsNb;
                case SceneType.MultiPatients:
                    return m_currentMpColumnsNb;
                default:
                    return -1;
            }
        }

        /// <summary>
        /// Return the rect transform of the ui for the input scene and column 
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="idButton"></param>
        /// <returns></returns>
        public RectTransform minimized_button_rectT(SceneType type, int idButton)
        {
            switch (type)
            {
                case SceneType.SinglePatient:
                    return m_spMinimizeOverlayList[idButton].transform.Find("minimize_button").GetComponent<RectTransform>();
                case SceneType.MultiPatients:
                    return m_mpMinimizeOverlayList[idButton].transform.Find("minimize_button").GetComponent<RectTransform>();
                default:
                    return null;
                    break;
            }
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

                        for (int jj = 0; jj < m_CamerasManager.GetComponent<CamerasManager>().GetNumberOfLines(SceneType.SinglePatient); ++jj)
                            m_CamerasManager.GetComponent<CamerasManager>().GetCamera(SceneType.SinglePatient, id, jj).set_minimized_state(m_spMinimizeStateList[id]);

                        setçoverlay_state(m_spMinimizeOverlayList[id], m_spMinimizeStateList[id]);
                        m_minimizeStateSwitchEvent.Invoke(true);
                    });

                    Button focusButton = m_spMinimizeOverlayList[id].transform.Find("focus button").GetComponent<Button>();
                    focusButton.onClick.AddListener(delegate
                    {
                        for (int jj = 0; jj < m_spMinimizeStateList.Count; ++jj)
                        {
                            m_spMinimizeStateList[jj] = (jj != id);
                           
                            for (int kk = 0; kk < m_CamerasManager.GetComponent<CamerasManager>().GetNumberOfLines(SceneType.SinglePatient); ++kk)
                                m_CamerasManager.GetComponent<CamerasManager>().GetCamera(SceneType.SinglePatient, jj, kk).set_minimized_state(m_spMinimizeStateList[jj]);

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
                for (int jj = 0; jj < m_CamerasManager.GetComponent<CamerasManager>().GetNumberOfLines(SceneType.SinglePatient); ++jj)
                {
                    m_CamerasManager.GetComponent<CamerasManager>().GetCamera(SceneType.SinglePatient, ii, jj).set_minimized_state(m_spMinimizeStateList[ii]);
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

                        for (int jj = 0; jj < m_CamerasManager.GetComponent<CamerasManager>().GetNumberOfLines(SceneType.MultiPatients); ++jj)
                            m_CamerasManager.GetComponent<CamerasManager>().GetCamera(SceneType.MultiPatients, id, jj).set_minimized_state(m_mpMinimizeStateList[id]);

                        setçoverlay_state(m_mpMinimizeOverlayList[id], m_mpMinimizeStateList[id]);
                        m_minimizeStateSwitchEvent.Invoke(false);
                    });


                    Button focusButton = m_mpMinimizeOverlayList[id].transform.Find("focus button").GetComponent<Button>();
                    focusButton.onClick.AddListener(delegate
                    {
                        for (int jj = 0; jj < m_mpMinimizeStateList.Count; ++jj)
                        {
                            m_mpMinimizeStateList[jj] = (jj != id);
                            
                            for (int kk = 0; kk < m_CamerasManager.GetComponent<CamerasManager>().GetNumberOfLines(SceneType.MultiPatients); ++kk)
                                m_CamerasManager.GetComponent<CamerasManager>().GetCamera(SceneType.MultiPatients, jj, kk).set_minimized_state(m_mpMinimizeStateList[jj]);

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
                for (int jj = 0; jj < m_CamerasManager.GetComponent<CamerasManager>().GetNumberOfLines(SceneType.MultiPatients); ++jj)
                {
                    m_CamerasManager.GetComponent<CamerasManager>().GetCamera(SceneType.MultiPatients, ii, jj).set_minimized_state(m_mpMinimizeStateList[ii]);
                }
            }

            m_currentMpColumnsNb = columnsNb;
        }



        

        #endregion
    }
}