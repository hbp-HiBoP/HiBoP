

/**
 * \file    ColormapController.cs
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
    /// Controller for the colormap display in the columns
    /// </summary>
    public class ColormapController : BothScenesOverlayController
    {
        #region members

        public int m_offsetX = 10;
        public int m_offsetY = 50;
        
        private Transform m_colormapControllerOverlay = null;

        private List<GameObject> m_spColormapOverlayList = new List<GameObject>();
        private List<GameObject> m_mpColormapOverlayList = new List<GameObject>();

        private bool m_spIsActive = false;
        private bool m_mpIsActive = false;

        private int m_currentSpColumnsNb;
        private int m_currentMpColumnsNb;


        #endregion members

        #region mono_behaviour

        // ...

        #endregion mono_behaviour

        #region others

        /// <summary>
        /// Init the controller
        /// </summary>
        /// <param name="scenesManager"></param>
        public new void init(ScenesManager scenesManager)
        {
            base.init(scenesManager);

            // associate transform canvas overlays
            GameObject overlayCanvas = new GameObject("colormap cameras");
            overlayCanvas.transform.SetParent(m_canvasOverlayParent);
            m_colormapControllerOverlay = overlayCanvas.transform;

            updateSPColorMapNb(1);
            updateMPColorMapNb(1);

            // listeners
            //  update colors map values
            m_spScene.SendColorMapValues.AddListener((minValue, middle, maxValue, id) =>
            {
                m_spColormapOverlayList[id].transform.Find("values panel").Find("minInf text").GetComponent<Text>().text = "" + minValue;
                m_spColormapOverlayList[id].transform.Find("values panel").Find("mid text").GetComponent<Text>().text = "" + middle;
                m_spColormapOverlayList[id].transform.Find("values panel").Find("maxInf text").GetComponent<Text>().text = "" + maxValue;
            });
            m_mpScene.SendColorMapValues.AddListener((minValue, middle, maxValue, id) =>
            {
                m_mpColormapOverlayList[id].transform.Find("values panel").Find("minInf text").GetComponent<Text>().text = "" + minValue;
                m_mpColormapOverlayList[id].transform.Find("values panel").Find("mid text").GetComponent<Text>().text = "" + middle;
                m_mpColormapOverlayList[id].transform.Find("values panel").Find("maxInf text").GetComponent<Text>().text = "" + maxValue;
            });
        }

        /// <summary>
        /// Update the UI with the current mode and activity
        /// </summary>
        public override void updateUI()
        {
            // sp
            if (currentSPMode != null)
                m_spIsActive = isVisibleFromSPScene && currentSPActivity && isEnoughtRoomSPScene;

            for (int ii = 0; ii < m_spColormapOverlayList.Count; ++ii)
                if (m_camerasManager.getColumnsNumber(true) > ii)
                    m_spColormapOverlayList[ii].SetActive(m_spIsActive && !m_camerasManager.getCamera(true, ii, 0).isMinimized());

            // mp
            if (currentMPMode != null)
                m_mpIsActive = isVisibleFromMPScene && currentMPActivity && isEnoughtRoomMPScene;

            for (int ii = 0; ii < m_mpColormapOverlayList.Count; ++ii)
                if (m_camerasManager.getColumnsNumber(false) > ii)
                    m_mpColormapOverlayList[ii].SetActive(m_mpIsActive && !m_camerasManager.getCamera(false, ii, 0).isMinimized());
        }


        /// <summary>
        /// Update the position of the UI in the scene
        /// </summary>
        public override void updateUIPosition()
        {
            for (int ii = 0; ii < m_spColormapOverlayList.Count; ++ii)
            {
                if (m_camerasManager.getColumnsNumber(true) < ii)
                    break;

                RectTransform rectTransfoCamera = m_camerasManager.getCameraRect(true, ii, 0);
                Rect rectCamera = CamerasManager.GetScreenRect(rectTransfoCamera, m_backGroundCamera);                
                getColormapRectT(true, ii).position = rectCamera.position + new Vector2(m_offsetX, rectCamera.height- m_offsetY);

                if(!m_camerasManager.getCamera(true, ii, 0).isMinimized())
                {
                    // check if enought room
                    bool previous = isEnoughtRoomSPScene;
                    isEnoughtRoomSPScene = !(rectCamera.width < 200);
                    if (previous != isEnoughtRoomSPScene)
                    {
                        updateUI();
                    }
                }
            }

            for (int ii = 0; ii < m_mpColormapOverlayList.Count; ++ii)
            {
                if (m_camerasManager.getColumnsNumber(false) < ii)
                    break;

                RectTransform rectTransfoCamera = m_camerasManager.getCameraRect(false, ii, 0);
                Rect rectCamera = CamerasManager.GetScreenRect(rectTransfoCamera, m_backGroundCamera);

                getColormapRectT(false, ii).position = rectCamera.position + new Vector2(m_offsetX, rectCamera.height - m_offsetY);

                if (!m_camerasManager.getCamera(false, ii, 0).isMinimized())
                {
                    // check if enought room
                    bool previous = isEnoughtRoomMPScene;
                    isEnoughtRoomMPScene = !(rectCamera.width < 200);
                    if (previous != isEnoughtRoomMPScene)
                    {
                        updateUI();
                    }
                }
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
        /// <param name="id"></param>
        /// <returns></returns>
        public RectTransform getColormapRectT(bool spScene, int id)
        {
            if (spScene)
            {
                return m_spColormapOverlayList[id].transform.GetComponent<RectTransform>();
            }

            return m_mpColormapOverlayList[id].transform.GetComponent<RectTransform>();
        }

        /// <summary>
        /// Add a new SP colormap
        /// </summary>
        public void addSPMinimizeButton()
        {
            updateSPColorMapNb(m_currentSpColumnsNb + 1);
        }

        /// <summary>
        /// Add a new MP colormap
        /// </summary>
        public void addMPMinimizeButton()
        {
            updateMPColorMapNb(m_currentMpColumnsNb + 1);
        }

        /// <summary>
        /// Remove the last SP colormap
        /// </summary>
        public void removeSPMinimizeButton()
        {
            updateSPColorMapNb(m_currentSpColumnsNb - 1);
        }

        /// <summary>
        /// Remove the last MP colormap
        /// </summary>
        public void removeMPMinimizeButton()
        {
            updateMPColorMapNb(m_currentMpColumnsNb - 1);
        }

        /// <summary>
        /// Update the number of colorMap for the SP scene
        /// </summary>
        /// <param name="columnsNb"></param>
        public void updateSPColorMapNb(int columnsNb)
        {
            int diff = m_currentSpColumnsNb - columnsNb;

            if (diff < 0)
            {
                for (int ii = 0; ii < -diff; ++ii)
                {
                    m_spColormapOverlayList.Add(Instantiate(BaseGameObjects.ColormapDisplay));
                    int id = m_spColormapOverlayList.Count - 1;
                    m_spColormapOverlayList[id].name = "sp_colormap_control_overlay_" + id;
                    m_spColormapOverlayList[id].transform.SetParent(m_colormapControllerOverlay, false);
                    m_spColormapOverlayList[id].SetActive(m_spIsActive);
                }
            }
            else
            {
                for (int ii = 0; ii < diff; ++ii)
                {
                    int id = m_spColormapOverlayList.Count - 1;
                    Destroy(m_spColormapOverlayList[id]);
                    m_spColormapOverlayList.RemoveAt(id);
                }
            }


            m_currentSpColumnsNb = columnsNb;
        }

        /// <summary>
        /// Update the number of colormap for the MP scene
        /// </summary>
        /// <param name="columnsNb"></param>
        public void updateMPColorMapNb(int columnsNb)
        {
            int diff = m_currentMpColumnsNb - columnsNb;

            if (diff < 0)
            {
                for (int ii = 0; ii < -diff; ++ii)
                {
                    m_mpColormapOverlayList.Add(Instantiate(BaseGameObjects.ColormapDisplay));
                    int id = m_mpColormapOverlayList.Count - 1;
                    m_mpColormapOverlayList[id].name = "mp_colormap_control_overlay_" + id;
                    m_mpColormapOverlayList[id].transform.SetParent(m_colormapControllerOverlay, false);
                    m_mpColormapOverlayList[id].SetActive(m_mpIsActive);
                }
            }
            else
            {
                for (int ii = 0; ii < diff; ++ii)
                {
                    int id = m_mpColormapOverlayList.Count - 1;
                    Destroy(m_mpColormapOverlayList[id]);
                    m_mpColormapOverlayList.RemoveAt(id);
                }
            }


            m_currentMpColumnsNb = columnsNb;
        }





        #endregion others
    }
}