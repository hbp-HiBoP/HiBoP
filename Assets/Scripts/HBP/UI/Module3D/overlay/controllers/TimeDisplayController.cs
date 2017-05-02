
/**
 * \file    TimeDisplayController.cs
 * \author  Lance Florian
 * \date    08/07/2016
 * \brief   Define TimeDisplayController class
 */

// system
using System;
using System.Collections.Generic;

// unity
using UnityEngine;
using UnityEngine.UI;

// hbp
using HBP.Module3D.Cam;

namespace HBP.Module3D
{

    /// <summary>
    ///  Controller for managing the display times overlays
    /// </summary>
    public class TimeDisplayController : IndividualSceneOverlayController
    {
        #region Properties

        private List<int> m_times = new List<int>();
        private Transform timeDisplayControllerOverlay = null;
        private List<GameObject> columnsTimeDisplay = new List<GameObject>();
        private List<UIOverlay> m_timeDisplayList = new List<UIOverlay>();
        private List<HBP.Data.Visualisation.TimeLine> m_iEEGTimelines = new List<Data.Visualisation.TimeLine>();
        private List<bool> m_minimized = new List<bool>();

        #endregion

        #region Public Methods
        /// <summary>
        /// Init the controller
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="camerasManager"></param>
        public new void init(Base3DScene scene, CamerasManager camerasManager)
        {
            base.Initialize(scene, camerasManager);

            // associate transform canvas overlays
            GameObject overlayCanvas = new GameObject("time display");
            overlayCanvas.transform.SetParent(transform);
            timeDisplayControllerOverlay = overlayCanvas.transform;

            // time display list
            m_timeDisplayList = new List<UIOverlay>();
            m_timeDisplayList.Add(new UIOverlay());
            m_timeDisplayList[0].mainUITransform = timeDisplayControllerOverlay;

            // columns icones display
            columnsTimeDisplay = new List<GameObject>();
            columnsTimeDisplay.Add(Instantiate(GlobalGOPreloaded.TimeDisplay));
            columnsTimeDisplay[0].name = "time_display_overlay_0";
            columnsTimeDisplay[0].transform.SetParent(timeDisplayControllerOverlay, false);
            columnsTimeDisplay[0].SetActive(true);

            // time
            m_times.Add(0);
            m_minimized.Add(false);
        }

        public void updateTime(int id, float value, bool global)
        {
            if (m_iEEGTimelines.Count == 0)
                return;

            float min = Single.MaxValue;
            float max = Single.MinValue;
            int size = m_iEEGTimelines[0].Lenght;
            string uniteMin = "";

            if (global)
            {
                for (int ii = 0; ii < m_iEEGTimelines.Count; ++ii)
                {
                    uniteMin = m_iEEGTimelines[ii].Start.Unite;

                    if (min > m_iEEGTimelines[ii].Start.Value)
                        min = m_iEEGTimelines[ii].Start.Value;

                    if (max < m_iEEGTimelines[ii].End.Value)
                        max = m_iEEGTimelines[ii].End.Value;
                }

                float diff = max - min;
                float offset = diff / (size - 1);
                decimal time = Math.Round((decimal)(min + value * offset), 2);
                for (int ii = 0; ii < columnsTimeDisplay.Count; ++ii)
                    columnsTimeDisplay[ii].transform.Find("time text").GetComponent<Text>().text = "" + time + " " + uniteMin;
            }
            else
            {
                min = m_iEEGTimelines[id].Start.Value;
                max = m_iEEGTimelines[id].End.Value;
                uniteMin = m_iEEGTimelines[id].Start.Unite;

                float diff = max - min;
                float offset = diff / (size - 1);
                decimal time = Math.Round((decimal)(min + value * offset), 2);
                columnsTimeDisplay[id].transform.Find("time text").GetComponent<Text>().text = "" + time + " " + uniteMin;
            }
        }

        /// <summary>
        /// Update the UI with the current mode and activity
        /// </summary>
        public override void UpdateUI()
        {
            bool activity = m_CurrentActivity && m_IsVisibleFromScene && m_IsEnoughtRoom;

            // set activity     
            for (int ii = 0; ii < m_timeDisplayList.Count; ++ii)
            {
                columnsTimeDisplay[ii].gameObject.SetActive(activity && !m_minimized[ii]);
            }
        }

        /// <summary>
        /// Update the position of the UI in the scene
        /// </summary>
        public override void UpdatePosition()
        {
            for (int ii = 0; ii < columnsTimeDisplay.Count; ++ii)
            {
                if (m_CamerasManager.GetNumberOfColumns(m_Scene.Type) < ii)
                    break;

                TrackBallCamera currentCamera = m_CamerasManager.GetCamera(m_Scene.Type, ii, 0);

                RectTransform rectTransfoCamera = m_CamerasManager.GetCameraRectTransform(m_Scene.Type, ii, 0);
                Rect rectCamera = rectTransfoCamera.rect;

                RectTransform rectTransformIcone = columnsTimeDisplay[ii].GetComponent<RectTransform>();
                if (!currentCamera.IsMinimized)
                {
                    rectTransformIcone.position = rectCamera.position + new Vector2(10, rectCamera.height -60);

                    bool previousMin = m_minimized[ii];
                    m_minimized[ii] = false;

                    bool previousRoom = m_IsEnoughtRoom;
                    m_IsEnoughtRoom = (rectCamera.width > 250) && (rectCamera.height > 300);

                    if (m_minimized[ii] != previousMin || previousRoom != m_IsEnoughtRoom)
                    {
                        UpdateUI();
                    }
                }
                else
                {
                    bool previousMin = m_minimized[ii];
                    m_minimized[ii] = true;


                    if (m_minimized[ii] != previousMin)
                    {
                        UpdateUI();
                    }
                }
            }
        }

        public void adaptNumber(int columnsNb)
        {
            if (columnsTimeDisplay.Count < columnsNb)
            {
                int diff = columnsNb - columnsTimeDisplay.Count;
                for (int ii = 0; ii < diff; ++ii)
                {
                    m_timeDisplayList.Add(new UIOverlay());
                    m_timeDisplayList[m_timeDisplayList.Count - 1].mainUITransform = timeDisplayControllerOverlay;

                    columnsTimeDisplay.Add(Instantiate(columnsTimeDisplay[columnsTimeDisplay.Count - 1]));
                    columnsTimeDisplay[columnsTimeDisplay.Count - 1].name = "time_display_overlay_" + (columnsTimeDisplay.Count - 1);
                    columnsTimeDisplay[columnsTimeDisplay.Count - 1].transform.SetParent(timeDisplayControllerOverlay, false);

                    m_times.Add(0);
                    m_minimized.Add(false);
                }
            }
            else if (columnsTimeDisplay.Count > columnsNb)
            {
                int diff = columnsTimeDisplay.Count - columnsNb;
                for (int ii = 0; ii < diff; ++ii)
                {
                    m_timeDisplayList.RemoveAt(m_timeDisplayList.Count - 1);

                    Destroy(columnsTimeDisplay[columnsTimeDisplay.Count - 1]);
                    columnsTimeDisplay.RemoveAt(columnsTimeDisplay.Count - 1);

                    m_times.RemoveAt(m_times.Count - 1);
                    m_minimized.RemoveAt(m_minimized.Count - 1);
                }
            }
        }

        /// <summary>
        /// Return the rect transform of the ui for the input scene and column 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public RectTransform getRectT(int id)
        {
            return columnsTimeDisplay[id].GetComponent<RectTransform>();
        }


        public void updateTimelinesUI(List<HBP.Data.Visualisation.TimeLine> iEEGTimelines)
        {
            m_iEEGTimelines = iEEGTimelines;
            updateTime(0, 0, true);
        }
        #endregion
    }
}