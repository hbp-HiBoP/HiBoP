

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
using HBP.Module3D.Cam;
using System;

namespace HBP.Module3D
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

        private Sprite m_colorMapSprite = null;

        #endregion members

        #region others

        /// <summary>
        /// Init the controller
        /// </summary>
        /// <param name="scenesManager"></param>
        public new void init(ScenesManager scenesManager)
        {
            base.Initialize(scenesManager);

            // associate transform canvas overlays
            GameObject overlayCanvas = new GameObject("colormap cameras");
            overlayCanvas.transform.SetParent(transform);
            m_colormapControllerOverlay = overlayCanvas.transform;

            update_SP_column_number(1);
            update_MP_column_number(1);

            // listeners
            //  update colors map values
            m_SinglePatientScene.SendColorMapValues.AddListener((minValue, middle, maxValue, id) =>
            {
                m_spColormapOverlayList[id].transform.Find("values panel").Find("minInf text").GetComponent<Text>().text = "" + minValue;
                m_spColormapOverlayList[id].transform.Find("values panel").Find("mid text").GetComponent<Text>().text = "" + middle;
                m_spColormapOverlayList[id].transform.Find("values panel").Find("maxInf text").GetComponent<Text>().text = "" + maxValue;
            });
            m_MultiPatientsScene.SendColorMapValues.AddListener((minValue, middle, maxValue, id) =>
            {
                m_mpColormapOverlayList[id].transform.Find("values panel").Find("minInf text").GetComponent<Text>().text = "" + minValue;
                m_mpColormapOverlayList[id].transform.Find("values panel").Find("mid text").GetComponent<Text>().text = "" + middle;
                m_mpColormapOverlayList[id].transform.Find("values panel").Find("maxInf text").GetComponent<Text>().text = "" + maxValue;
            });
        }

        /// <summary>
        /// Update the UI with the current mode and activity
        /// </summary>
        public override void UpdateUI()
        {
            // sp
            if (m_CurrentSinglePatientMode != null)
                m_spIsActive = m_IsVisibleFromSinglePatientScene && m_CurrentSinglePatientActivity && m_IsEnoughtRoomInSinglePatientScene;

            for (int ii = 0; ii < m_spColormapOverlayList.Count; ++ii)
                if (m_CamerasManager.GetNumberOfColumns(SceneType.SinglePatient) > ii)
                    m_spColormapOverlayList[ii].SetActive(m_spIsActive && !m_CamerasManager.GetCamera(SceneType.SinglePatient, ii, 0).IsMinimized);

            // mp
            if (m_CurrentMultiPatientsMode != null)
                m_mpIsActive = m_IsVisibleFromMultiPatientsScene && m_CurrentMultiPatientsActivity && m_IsEnoughtRoomInMultiPatientsScene;

            for (int ii = 0; ii < m_mpColormapOverlayList.Count; ++ii)
                if (m_CamerasManager.GetNumberOfColumns(SceneType.MultiPatients) > ii)
                    m_mpColormapOverlayList[ii].SetActive(m_mpIsActive && !m_CamerasManager.GetCamera(SceneType.MultiPatients, ii, 0).IsMinimized);
        }


        /// <summary>
        /// Update the position of the UI in the scene
        /// </summary>
        public override void UpdatePosition()
        {
            for (int ii = 0; ii < m_spColormapOverlayList.Count; ++ii)
            {
                if (m_CamerasManager.GetNumberOfColumns(SceneType.SinglePatient) < ii)
                    break;

                RectTransform rectTransfoCamera = m_CamerasManager.GetCameraRectTransform(SceneType.SinglePatient, ii, 0);
                Rect rectCamera = rectTransfoCamera.rect;
                colormap_rect_transform(SceneType.SinglePatient, ii).position = rectCamera.position + new Vector2(m_offsetX, rectCamera.height- m_offsetY);

                if(!m_CamerasManager.GetCamera(SceneType.SinglePatient, ii, 0).IsMinimized)
                {
                    // check if enought room
                    bool previous = m_IsEnoughtRoomInSinglePatientScene;
                    m_IsEnoughtRoomInSinglePatientScene = !(rectCamera.width < 200);
                    if (previous != m_IsEnoughtRoomInSinglePatientScene)
                    {
                        UpdateUI();
                    }
                }
            }

            for (int ii = 0; ii < m_mpColormapOverlayList.Count; ++ii)
            {
                if (m_CamerasManager.GetNumberOfColumns(SceneType.MultiPatients) < ii)
                    break;

                RectTransform rectTransfoCamera = m_CamerasManager.GetCameraRectTransform(SceneType.MultiPatients, ii, 0);
                Rect rectCamera = rectTransfoCamera.rect;

                colormap_rect_transform(SceneType.MultiPatients, ii).position = rectCamera.position + new Vector2(m_offsetX, rectCamera.height - m_offsetY);

                if (!m_CamerasManager.GetCamera(SceneType.MultiPatients, ii, 0).IsMinimized)
                {
                    // check if enought room
                    bool previous = m_IsEnoughtRoomInMultiPatientsScene;
                    m_IsEnoughtRoomInMultiPatientsScene = !(rectCamera.width < 200);
                    if (previous != m_IsEnoughtRoomInMultiPatientsScene)
                    {
                        UpdateUI();
                    }
                }
            }
        }

        /// <summary>
        /// Return the number of columns of the scene
        /// </summary>
        /// <param name="spScene"></param>
        /// <returns></returns>
        public int columns_number(SceneType type)
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
        /// <param name="id"></param>
        /// <returns></returns>
        public RectTransform colormap_rect_transform(SceneType type, int id)
        {
            switch (type)
            {
                case SceneType.SinglePatient:
                    return m_spColormapOverlayList[id].transform.GetComponent<RectTransform>();
                case SceneType.MultiPatients:
                    return m_mpColormapOverlayList[id].transform.GetComponent<RectTransform>();
                default:
                    return null;
            }
        }

        /// <summary>
        /// Add a new SP colormap
        /// </summary>
        public void add_SP_minimize_button()
        {
            update_SP_column_number(m_currentSpColumnsNb + 1);
        }

        /// <summary>
        /// Add a new MP colormap
        /// </summary>
        public void add_MP_minimize_button()
        {
            update_MP_column_number(m_currentMpColumnsNb + 1);
        }

        /// <summary>
        /// Remove the last SP colormap
        /// </summary>
        public void remove_SP_minimize_button()
        {
            update_SP_column_number(m_currentSpColumnsNb - 1);
        }

        /// <summary>
        /// Remove the last MP colormap
        /// </summary>
        public void remove_MP_minimize_button()
        {
            update_MP_column_number(m_currentMpColumnsNb - 1);
        }

        /// <summary>
        /// Update the number of colorMap for the SP scene
        /// </summary>
        /// <param name="columnsNb"></param>
        public void update_SP_column_number(int columnsNb)
        {
            int diff = m_currentSpColumnsNb - columnsNb;

            if (diff < 0)
            {
                for (int ii = 0; ii < -diff; ++ii)
                {
                    m_spColormapOverlayList.Add(Instantiate(GlobalGOPreloaded.ColormapDisplay));
                    int id = m_spColormapOverlayList.Count - 1;
                    m_spColormapOverlayList[id].name = "sp_colormap_control_overlay_" + id;
                    m_spColormapOverlayList[id].transform.SetParent(m_colormapControllerOverlay, false);
                    m_spColormapOverlayList[id].SetActive(m_spIsActive);
                    
                    Destroy(m_spColormapOverlayList[id].transform.Find("colorMap panel").GetComponent<Image>().sprite);
                    m_spColormapOverlayList[id].transform.Find("colorMap panel").GetComponent<Image>().sprite = m_colorMapSprite;
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
        public void update_MP_column_number(int columnsNb)
        {
            int diff = m_currentMpColumnsNb - columnsNb;

            if (diff < 0)
            {
                for (int ii = 0; ii < -diff; ++ii)
                {
                    m_mpColormapOverlayList.Add(Instantiate(GlobalGOPreloaded.ColormapDisplay));
                    int id = m_mpColormapOverlayList.Count - 1;
                    m_mpColormapOverlayList[id].name = "mp_colormap_control_overlay_" + id;
                    m_mpColormapOverlayList[id].transform.SetParent(m_colormapControllerOverlay, false);
                    m_mpColormapOverlayList[id].SetActive(m_mpIsActive);

                    Destroy(m_mpColormapOverlayList[id].transform.Find("colorMap panel").GetComponent<Image>().sprite);
                    m_mpColormapOverlayList[id].transform.Find("colorMap panel").GetComponent<Image>().sprite = m_colorMapSprite;
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


        public void update_colormap(ColorType color)
        {
            Texture2D tex2D = new Texture2D(1, 1);
            DLL.Texture tex = DLL.Texture.generate_1D_color_texture(color);
            tex.update_texture_2D(tex2D);

            m_colorMapSprite = Sprite.Create(tex2D, new Rect(0, 0, tex2D.width, tex2D.height), new Vector2(0, 0));
            for (int ii = 0; ii < m_spColormapOverlayList.Count; ++ii)
            {
                Destroy(m_spColormapOverlayList[ii].transform.Find("colorMap panel").GetComponent<Image>().sprite);
                m_spColormapOverlayList[ii].transform.Find("colorMap panel").GetComponent<Image>().sprite = m_colorMapSprite;
            }
            for (int ii = 0; ii < m_mpColormapOverlayList.Count; ++ii)
            {
                Destroy(m_mpColormapOverlayList[ii].transform.Find("colorMap panel").GetComponent<Image>().sprite);
                m_mpColormapOverlayList[ii].transform.Find("colorMap panel").GetComponent<Image>().sprite = m_colorMapSprite;
            }
        }

        #endregion others
    }
}