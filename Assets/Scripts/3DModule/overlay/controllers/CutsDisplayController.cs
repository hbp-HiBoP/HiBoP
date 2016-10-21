
/**
 * \file    CutsDisplayController.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define CutsDisplayController class
 */

// system
using System.IO;
using System.Collections;
using System.Collections.Generic;

// unity
using UnityEngine;
using UnityEngine.UI;

// hbp
using HBP.VISU3D.Cam;

namespace HBP.VISU3D
{
    /// <summary>
    /// A controller for the cuts display images
    /// </summary>
    public class CutsDisplayController : BothScenesOverlayController
    {

        #region members
        
        private UIOverlay displayImageCutControllerModuleOverlay = new UIOverlay();
        
        private bool m_displaySpScene = false; /**< display sp scene if true, else display mp scene*/
        private bool m_minimizedState = false;
        private bool m_spIsActive = false; /**< is sp scene active */
        private bool m_mpIsActive = false; /**< is mp scene active */
        private int m_currentWidth = 150;
        private int m_spPlanesNb; /**< sp nb plane cuts */
        private int m_mpPlanesNb; /**< mp nb plane cuts */

        public Transform m_parentImageDisplayObjectTransform;

        private Transform m_parentSpImageDisplayOverlayTransform;
        private Transform m_parentMpImageDisplayOverlayTransform;

        private List<Texture2D> m_texturesColumnsSp = new List<Texture2D>(); /**< sp columns textures */
        private List<Texture2D> m_texturesColumnsMp = new List<Texture2D>(); /**< mp columns textures */

        private List<GameObject> m_spCutsImagesOverlay; /**< gameobjects containing the sp scene cut textures to be displayed */
        private List<GameObject> m_mpCutsImagesOverlay; /**< gameobjects containing the mp scene cut textures to be displayed */
        private List<GameObject> m_cutImagesObject;

        #endregion members

        #region functions

        /// <summary>
        /// Init the contoller
        /// </summary>
        /// <param name="scenesManager"></param>
        public new void init(ScenesManager scenesManager)
        {
            base.init(scenesManager);

            // associate transform canvas overlays
            GameObject spCutImage = new GameObject("cut image");
            spCutImage.transform.SetParent(m_canvasOverlayParent.Find("SP"));

            GameObject mpCutImage = new GameObject("cut image");
            mpCutImage.transform.SetParent(m_canvasOverlayParent.Find("MP"));

            m_parentSpImageDisplayOverlayTransform = spCutImage.transform;
            m_parentMpImageDisplayOverlayTransform = mpCutImage.transform;

            // init visibility
            displayImageCutControllerModuleOverlay.mainUITransform = m_parentImageDisplayObjectTransform.parent;

            // retrieve image display objects
            m_cutImagesObject = new List<GameObject>();
            for (int ii = 0; ii < 5; ++ii)
            {
                m_cutImagesObject.Add(m_parentImageDisplayObjectTransform.Find("image_cut" + ii).gameObject);
            }

            m_spCutsImagesOverlay = new List<GameObject>();
            m_mpCutsImagesOverlay = new List<GameObject>();

            // init listeners
            m_parentImageDisplayObjectTransform.parent.Find("image options").Find("upsize button").GetComponent<Button>().onClick.AddListener(
                    delegate { upSize(); }
                );
            m_parentImageDisplayObjectTransform.parent.Find("image options").Find("donwsize button").GetComponent<Button>().onClick.AddListener(
                    delegate { downSize(); }
                );
            m_spScene.UpdateCutsInUI.AddListener((textures, idColumn, nbPlanes) =>
            {
                updateImagesToDisplay(true, textures, idColumn, nbPlanes);
            });
            m_mpScene.UpdateCutsInUI.AddListener((textures, idColumn, nbPlanes) =>
            {
                updateImagesToDisplay(false, textures, idColumn, nbPlanes);
            });
        }

        /// <summary>
        /// Update the UI with the current mode and activity
        /// </summary>
        public override void updateUI()
        {
            // sp
            if (currentSPMode != null)
            {
                m_spIsActive = isVisibleFromSPScene && currentSPActivity;

                for (int ii = 0; ii < m_spCutsImagesOverlay.Count; ++ii)
                    m_spCutsImagesOverlay[ii].SetActive(m_spIsActive && !m_minimizedState);
            }

            // mp
            if (currentSPMode != null)
            {
                m_mpIsActive = isVisibleFromMPScene && currentMPActivity;

                for (int ii = 0; ii < m_mpCutsImagesOverlay.Count; ++ii)
                    m_mpCutsImagesOverlay[ii].SetActive(m_mpIsActive && !m_minimizedState);
            }

            // set activity
            displayImageCutControllerModuleOverlay.setActivity(m_spIsActive || m_mpIsActive);
        }

        /// <summary>
        /// Update the position of the UI in the scene
        /// </summary>
        public override void updateUIPosition()
        {
            if (m_displaySpScene)
            {
                for (int ii = 0; ii < m_mpCutsImagesOverlay.Count; ++ii)
                    m_mpCutsImagesOverlay[ii].SetActive(false);

                for (int ii = 0; ii < m_cutImagesObject.Count; ++ii)
                {
                    if (ii < m_spPlanesNb)
                        m_cutImagesObject[ii].SetActive(true);
                    else
                        m_cutImagesObject[ii].SetActive(false);
                }

                for (int ii = 0; ii < m_spPlanesNb; ++ii)
                {
                    if(m_texturesColumnsSp[ii] != null)
                        if (m_texturesColumnsSp[ii].width > 0)
                        {                        
                            Rect rectCamera = CamerasManager.GetScreenRect(m_cutImagesObject[ii].gameObject.GetComponent<RectTransform>(), m_backGroundCamera);
                            RectTransform rectTransform = m_spCutsImagesOverlay[ii].gameObject.GetComponent<RectTransform>();
                            rectTransform.position = rectCamera.position + new Vector2(0, rectCamera.height);
                            rectTransform.sizeDelta = new Vector2(rectCamera.width, rectCamera.height);             
                        }
                }

                isEnoughtRoomSPScene = true;
            }
            else
            {
                for (int ii = 0; ii < m_spCutsImagesOverlay.Count; ++ii)
                    m_spCutsImagesOverlay[ii].SetActive(false);

                for (int ii = 0; ii < m_cutImagesObject.Count; ++ii)
                {
                    if (ii < m_mpPlanesNb)
                        m_cutImagesObject[ii].SetActive(true);
                    else
                        m_cutImagesObject[ii].SetActive(false);
                }

                for (int ii = 0; ii < m_mpPlanesNb; ++ii)
                {
                    if (m_texturesColumnsMp[ii] == null)
                        break;

                    if (m_texturesColumnsMp[ii] != null)
                        if (m_texturesColumnsMp[ii].width > 0)
                        {
                            Rect rectCamera = CamerasManager.GetScreenRect(m_cutImagesObject[ii].gameObject.GetComponent<RectTransform>(), m_backGroundCamera);
                            RectTransform rectTransform = m_mpCutsImagesOverlay[ii].gameObject.GetComponent<RectTransform>();
                            rectTransform.position = rectCamera.position + new Vector2(0, rectCamera.height);
                            rectTransform.sizeDelta = new Vector2(rectCamera.width, rectCamera.height);
                        }
                }

                isEnoughtRoomMPScene = true;
            }
        }

        /// <summary>
        /// Update the images to display
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="images"></param>
        /// <param name="columnId"></param>
        /// <param name="planeNb"></param>
        private void updateImagesToDisplay(bool spScene, List<Texture2D> images, int columnId, int planeNb)
        {
            if (spScene)
            {
                m_spPlanesNb = planeNb;

                // add / remove overlays images
                if (m_spCutsImagesOverlay.Count < planeNb)
                {
                    int diff = planeNb - m_spCutsImagesOverlay.Count;
                    for (int ii = 0; ii < diff; ++ii)
                    {
                        m_spCutsImagesOverlay.Add(Instantiate(BaseGameObjects.CutImageOverlay));
                        m_spCutsImagesOverlay[m_spCutsImagesOverlay.Count - 1].name = "image cut overlay " + (m_spCutsImagesOverlay.Count - 1);
                        m_spCutsImagesOverlay[m_spCutsImagesOverlay.Count - 1].transform.SetParent(m_parentSpImageDisplayOverlayTransform, false);
                        m_spCutsImagesOverlay[m_spCutsImagesOverlay.Count - 1].SetActive(true);
                    }
                }
                else if (m_spCutsImagesOverlay.Count > planeNb)
                {
                    int diff = m_spCutsImagesOverlay.Count - planeNb;
                    for (int ii = 0; ii < diff; ++ii)
                    {
                        Destroy(m_spCutsImagesOverlay[m_spCutsImagesOverlay.Count - 1]);
                        m_spCutsImagesOverlay.RemoveAt(m_spCutsImagesOverlay.Count - 1);
                    }
                }

                m_texturesColumnsSp = images;


                // create sprites
                for (int ii = 0; ii < m_texturesColumnsSp.Count; ++ii)
                {
                    if (ii < m_spCutsImagesOverlay.Count)
                    {
                        // clean previous sprite
                        Destroy(m_spCutsImagesOverlay[ii].gameObject.GetComponent<Image>().sprite);
                        m_spCutsImagesOverlay[ii].gameObject.GetComponent<Image>().sprite = Sprite.Create(m_texturesColumnsSp[ii],
                                    new Rect(0, 0, m_texturesColumnsSp[ii].width, m_texturesColumnsSp[ii].height), new Vector2(0, 0));
                    }
                }
            }
            else
            {
                m_mpPlanesNb = planeNb;

                // add / remove overlays images
                if (m_mpCutsImagesOverlay.Count < planeNb)
                {
                    int diff = planeNb - m_mpCutsImagesOverlay.Count;
                    for (int ii = 0; ii < diff; ++ii)
                    {
                        m_mpCutsImagesOverlay.Add(Instantiate(BaseGameObjects.CutImageOverlay));
                        m_mpCutsImagesOverlay[m_mpCutsImagesOverlay.Count - 1].name = "image cut overlay " + (m_mpCutsImagesOverlay.Count - 1);
                        m_mpCutsImagesOverlay[m_mpCutsImagesOverlay.Count - 1].transform.SetParent(m_parentMpImageDisplayOverlayTransform, false);
                        m_mpCutsImagesOverlay[m_mpCutsImagesOverlay.Count - 1].SetActive(true);
                    }
                }
                else if (m_mpCutsImagesOverlay.Count > planeNb)
                {
                    int diff = m_mpCutsImagesOverlay.Count - planeNb;
                    for (int ii = 0; ii < diff; ++ii)
                    {
                        Destroy(m_mpCutsImagesOverlay[m_mpCutsImagesOverlay.Count - 1]);
                        m_mpCutsImagesOverlay.RemoveAt(m_mpCutsImagesOverlay.Count - 1);
                    }
                }

                m_texturesColumnsMp = images;

                // create sprites
                for (int ii = 0; ii < m_texturesColumnsMp.Count; ++ii)
                {
                    if (ii < m_mpCutsImagesOverlay.Count)
                    {
                        // clean previous sprite
                        Destroy(m_mpCutsImagesOverlay[ii].gameObject.GetComponent<Image>().sprite);

                        m_mpCutsImagesOverlay[ii].gameObject.GetComponent<Image>().sprite = Sprite.Create(m_texturesColumnsMp[ii],
                                    new Rect(0, 0, m_texturesColumnsMp[ii].width, m_texturesColumnsMp[ii].height), new Vector2(0, 0));
                    }
                }

            }

            updateUI();
        }

        /// <summary>
        /// Set the current scene images cuts to display
        /// </summary>
        /// <param name="spScene"></param>
        public void setSceneToDisplay(bool spScene)
        {
            m_displaySpScene = spScene;
            updateUI();
        }

        /// <summary>
        /// Upsize the space of the displayed images cuts
        /// </summary>
        private void upSize()
        {
            Transform imageOptions = m_parentImageDisplayObjectTransform.parent.Find("image options");
            bool upsizeState = true, donwsizeState = true;

            if (m_currentWidth == 20)
            {
                m_currentWidth = 150;
                donwsizeState = true;
                upsizeState = true;
                m_minimizedState = false;
            }
            else if (m_currentWidth == 150)
            {
                m_currentWidth = 300;
                donwsizeState = true;
                upsizeState = true;
            }
            else if (m_currentWidth == 300)
            {
                m_currentWidth = 500;
                donwsizeState = true;
                upsizeState = false;
            }

            m_parentImageDisplayObjectTransform.parent.Find("image_displays_panel").GetComponent<LayoutElement>().minWidth = m_currentWidth;
            imageOptions.Find("donwsize button").GetComponent<Button>().interactable = donwsizeState;
            imageOptions.Find("upsize button").GetComponent<Button>().interactable = upsizeState;

            updateUI();
        }

        /// <summary>
        /// Downsize the space of the displayed images cuts
        /// </summary>
        private void downSize()
        {            
            Transform imageOptions = m_parentImageDisplayObjectTransform.parent.Find("image options");
            bool upsizeState = true, donwsizeState = true;

            if (m_currentWidth == 150)
            {
                m_currentWidth = 20;
                donwsizeState = false;
                upsizeState = true;
                m_minimizedState = true;
            }
            else if(m_currentWidth == 300)
            {
                m_currentWidth = 150;
                donwsizeState = true;
                upsizeState = true;
            }
            else if (m_currentWidth == 500)
            {
                m_currentWidth = 300;
                donwsizeState = true;
                upsizeState = true;
            }

            m_parentImageDisplayObjectTransform.parent.Find("image_displays_panel").GetComponent<LayoutElement>().minWidth = m_currentWidth;
            imageOptions.Find("donwsize button").GetComponent<Button>().interactable = donwsizeState;
            imageOptions.Find("upsize button").GetComponent<Button>().interactable = upsizeState;

            updateUI();
        }


        #endregion functions
    }
}