
/**
 * \file    IconesController.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define IconesController class
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
    ///  Controller for managing the tiemlines icons overlay
    /// </summary>
    public class IconesController : IndividualSceneOverlayController
    {    
        #region members
        
        private List<int> m_times = new List<int>();
        private List<HBP.Data.Visualisation.IconicScenario> m_iconicScenarioList;
        private List<List<Sprite>> m_sprites = new List<List<Sprite>>();
        private List<List<Texture2D>> m_textures= new List<List<Texture2D>>();

        private Transform iconeControllerOverlay;
        private List<GameObject> columnsIconeDisplay;
        private List<UIOverlay> m_iconesList;

        private int m_currentSize = 95;

        #endregion members

        #region mono_behaviour

        /// <summary>
        /// This function is called after all frame updates for the last frame of the object’s existence (the object might be destroyed in response to Object.Destroy or at the closure of a scene).
        /// </summary>
        void OnDestroy()
        {
            for (int ii = 0; ii < m_textures.Count; ii++)
            {
                for (int jj = 0; jj < m_textures[0].Count; jj++)
                {
                    Destroy(m_textures[ii][jj]);
                }
            }
        }

        #endregion mono_behaviour

        /// <summary>
        /// Init the controller
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="camerasManager"></param>
        public new void init(Base3DScene scene, CamerasManager camerasManager)
        {
            base.init(scene, camerasManager);

            // associate transform canvas overlays
            GameObject overlayCanvas = new GameObject("icone");
            overlayCanvas.transform.SetParent(m_canvasOverlayParent);
            iconeControllerOverlay = overlayCanvas.transform;

            // icones list
            m_iconesList = new List<UIOverlay>();
            m_iconesList.Add(new UIOverlay());
            m_iconesList[0].mainUITransform = iconeControllerOverlay;

            // columns icones display
            columnsIconeDisplay = new List<GameObject>();
            columnsIconeDisplay.Add(Instantiate(BaseGameObjects.IconeDisplay));
            columnsIconeDisplay[0].name = "icone_display_overlay_0";
            columnsIconeDisplay[0].transform.SetParent(iconeControllerOverlay, false);
            columnsIconeDisplay[0].transform.Find("donwsize button").GetComponent<Button>().onClick.AddListener(() =>
            {
                downSize();
            });

            columnsIconeDisplay[0].transform.Find("upsize button").GetComponent<Button>().onClick.AddListener(() =>
            {
                upSize();
            });


            // time
            m_times.Add(0);
        }

        /// <summary>
        /// Update the UI with the current mode and activity
        /// </summary>
        public override void updateUI()
        {
            bool activity = currentActivity && isVisibleFromScene && isEnoughtRoom;
            
            if (currentMode != null)
                if (currentMode.m_idMode == Mode.ModesId.NoPathDefined || currentMode.m_idMode == Mode.ModesId.MinPathDefined  ||
                    currentMode.m_idMode == Mode.ModesId.AllPathDefined || currentMode.m_idMode == Mode.ModesId.Error)
                {
                    activity = false;
                }

            // set activity     
            for (int ii = 0; ii < m_iconesList.Count; ++ii)
                m_iconesList[ii].setActivity(activity);
        }

        /// <summary>
        /// Update the position of the UI in the scene
        /// </summary>
        public override void updateUIPosition()
        {
            for (int ii = 0; ii < columnsIconeDisplay.Count; ++ii)
            {
                if (m_camerasManager.getColumnsNumber(m_isSPScene) < ii)
                    break;

                TrackBallCamera currentCamera = m_camerasManager.getCamera(m_isSPScene, ii, 0);

                RectTransform rectTransfoCamera = m_camerasManager.getCameraRect(m_isSPScene, ii, 0);
                Rect rectCamera = CamerasManager.GetScreenRect(rectTransfoCamera, m_backGroundCamera);

                RectTransform rectTransformIcone = columnsIconeDisplay[ii].GetComponent<RectTransform>();
                rectTransformIcone.sizeDelta = new Vector2(m_currentSize-20, m_currentSize);

                // display icons
                if (m_sprites.Count > 0)
                {
                    // retrieve the current displayed icon                
                    int t = m_times[ii];
                    int idIcon = -1;
                    for (int jj = 0; jj < m_iconicScenarioList[ii].Icons.Length; ++jj)
                    {
                        if (t >= m_iconicScenarioList[ii].Icons[jj].Min && t <= m_iconicScenarioList[ii].Icons[jj].Max)
                        {
                            idIcon = jj;
                            break;
                        }
                    }

                    if (idIcon != -1)
                    {
                        columnsIconeDisplay[ii].gameObject.SetActive(true);

                        // display image
                        if (m_sprites[ii][idIcon] != null)
                        {
                            columnsIconeDisplay[ii].transform.Find("icone_display_image").gameObject.SetActive(true);
                            columnsIconeDisplay[ii].transform.Find("icone_display_image").GetComponent<Image>().sprite = m_sprites[ii][idIcon];
                        }
                        else // no image to be displayed
                        {
                            columnsIconeDisplay[ii].transform.Find("icone_display_image").gameObject.SetActive(false);
                        }

                        // display label
                        columnsIconeDisplay[ii].transform.Find("time_display_text").GetComponent<Text>().text = m_iconicScenarioList[ii].Icons[idIcon].Label;
                    }
                    else // if no icon to be displayed
                    {
                        columnsIconeDisplay[ii].gameObject.SetActive(false);
                    }
                }


                if (!currentCamera.isMinimized())
                {
                    rectTransformIcone.position = rectCamera.position + new Vector2(rectCamera.width - m_currentSize, rectCamera.height - m_currentSize-20 - 25);

                    // check if enought room
                    bool previous = isEnoughtRoom;
                    isEnoughtRoom = (rectCamera.width > 100) && (rectCamera.height > 150);
                    if (previous != isEnoughtRoom)
                    {
                        updateUI();
                    }
                }
                else
                {
                    columnsIconeDisplay[ii].gameObject.SetActive(false);
                }
            }
        }




        public void setTime(bool global, int idColumn, int time)
        {
            if (global)
            {
                for (int ii = 0; ii < m_times.Count; ++ii)
                {
                    m_times[ii] = time;
                }
            }
            else
            {
                m_times[idColumn] = time;
            }
        }

        public void defineIconicScenario(List<HBP.Data.Visualisation.IconicScenario> iconicScenarioList)
        {
            m_iconicScenarioList = iconicScenarioList;

            // destroy previous textures
            for (int ii = 0; ii < m_textures.Count; ii++)
            {
                for (int jj = 0; jj < m_textures[ii].Count; jj++)
                {
                    Destroy(m_textures[ii][jj]);
                }
            }
            m_textures.Clear();

            m_sprites.Clear();            
            for (int ii = 0; ii < m_iconicScenarioList.Count; ++ii)
            {
                m_sprites.Add(new List<Sprite>());
                m_textures.Add(new List<Texture2D>());

                for (int jj = 0; jj < m_iconicScenarioList[ii].Icons.Length; ++jj)
                {
                    string path = m_iconicScenarioList[ii].Icons[jj].Path;
                    if (path.Length > 0)
                    {
                        DLL.Texture loadedTexture = DLL.Texture.load(m_iconicScenarioList[ii].Icons[jj].Path);
                        m_textures[ii].Add(loadedTexture.getTexture2D());
                        m_sprites[ii].Add(Sprite.Create(m_textures[ii][jj], new Rect(0, 0, m_textures[ii][jj].width, m_textures[ii][jj].height), new Vector2(0, 0)));
                    }
                    else
                    {
                        m_textures[ii].Add(null);
                        m_sprites[ii].Add(null);
                    }

                }
            }
        }

        public void updateImage(Sprite sprite, int columnId)
        {
            columnsIconeDisplay[columnId].transform.Find("icone_display_image").GetComponent<Image>().sprite = sprite;
        }

        public void updateTimeText(string text, int columnId)
        {
            columnsIconeDisplay[columnId].transform.Find("time_display_text").GetComponent<Text>().text = text;
        }

        public void adaptIconesNb(int columnsNb)
        {
            if (columnsIconeDisplay.Count < columnsNb)
            {
                int diff = columnsNb - columnsIconeDisplay.Count;
                for (int ii = 0; ii < diff; ++ii)
                {
                    m_iconesList.Add(new UIOverlay());
                    m_iconesList[m_iconesList.Count - 1].mainUITransform = iconeControllerOverlay;

                    columnsIconeDisplay.Add(Instantiate(columnsIconeDisplay[columnsIconeDisplay.Count - 1]));
                    columnsIconeDisplay[columnsIconeDisplay.Count - 1].name = "icone_display_overlay_" + (columnsIconeDisplay.Count - 1);
                    columnsIconeDisplay[columnsIconeDisplay.Count - 1].transform.SetParent(iconeControllerOverlay, false);

                    columnsIconeDisplay[columnsIconeDisplay.Count - 1].transform.Find("donwsize button").GetComponent<Button>().onClick.AddListener(() =>
                    {
                        downSize();
                    });

                    columnsIconeDisplay[columnsIconeDisplay.Count - 1].transform.Find("upsize button").GetComponent<Button>().onClick.AddListener(() =>
                    {
                        upSize();
                    });

                    m_times.Add(0);
                }
            }
            else if (columnsIconeDisplay.Count > columnsNb)
            {
                int diff = columnsIconeDisplay.Count - columnsNb;
                for (int ii = 0; ii < diff; ++ii)
                {
                    m_iconesList.RemoveAt(m_iconesList.Count - 1);

                    Destroy(columnsIconeDisplay[columnsIconeDisplay.Count - 1]);
                    columnsIconeDisplay.RemoveAt(columnsIconeDisplay.Count - 1);

                    m_times.RemoveAt(m_times.Count - 1);
                }
            }
        }

        /// <summary>
        /// Upsize the icone display
        /// </summary>
        private void upSize()
        {
            bool upsizeState = true, downsizeState = true;

            if (m_currentSize == 95)
            {
                m_currentSize = 150;
                upsizeState = false;
                downsizeState = true;
            }

            for (int ii = 0; ii < columnsIconeDisplay.Count; ++ii)
            {
                columnsIconeDisplay[ii].transform.Find("donwsize button").GetComponent<Button>().interactable = downsizeState;
                columnsIconeDisplay[ii].transform.Find("upsize button").GetComponent<Button>().interactable = upsizeState;
            }

            updateUI();
        }

        /// <summary>
        /// Downsize the icone display
        /// </summary>
        private void downSize()
        {
            bool upsizeState = true, downsizeState = true;

            if (m_currentSize == 150)
            {
                m_currentSize = 95;
                upsizeState = true;
                downsizeState = false;
            }

            for (int ii = 0; ii < columnsIconeDisplay.Count; ++ii)
            {
                columnsIconeDisplay[ii].transform.Find("donwsize button").GetComponent<Button>().interactable = downsizeState;
                columnsIconeDisplay[ii].transform.Find("upsize button").GetComponent<Button>().interactable = upsizeState;
            }

            updateUI();
        }
    }
}