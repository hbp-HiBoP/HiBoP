
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
using HBP.Module3D.Cam;

namespace HBP.Module3D
{
    public class CutsDisplayMenu : MonoBehaviour
    {
        private int m_maxPlaneNb = 5;
        private int m_currentPlanesNb = 0;
        private int m_currentWidth = 150;
        //private bool m_minimizedState = false;

        private List<Texture2D> m_textures = null; /**< cuts textures */
        private List<GameObject> m_texturesOverlay = new List<GameObject>(); /**< cuts textures overlays */
        private List<GameObject> m_texturesGOContainer = new List<GameObject>(); /**< ... */

        private Transform m_imagesParent;
        private Camera m_backGroundCamera = null;

        public void init(Camera backGroundCamera, Transform imagesParent, Transform overlayParent)
        {
            m_backGroundCamera = backGroundCamera;
            m_imagesParent = imagesParent;

            // retrieve image display objects
            for (int ii = 0; ii < m_maxPlaneNb; ++ii)
            {
                m_texturesGOContainer.Add(m_imagesParent.Find("image_displays_panel").Find("image_cut" + ii).gameObject);
                m_imagesParent.Find("image_displays_panel").Find("image_cut" + ii).gameObject.SetActive(false);

                GameObject cutOverlay = Instantiate(GlobalGOPreloaded.CutImageOverlay);
                cutOverlay.name = "image cut overlay " + ii;
                cutOverlay.transform.SetParent(overlayParent, false);

                cutOverlay.SetActive(false);
                m_texturesOverlay.Add(cutOverlay);
            }

            set_listeners();

        }

        private void set_listeners()
        {
            // init listeners
            m_imagesParent.Find("image options").Find("upsize button").GetComponent<Button>().onClick.AddListener(
                    delegate { upsize(); }
                );
            m_imagesParent.Find("image options").Find("donwsize button").GetComponent<Button>().onClick.AddListener(
                    delegate { donwsize(); }
                );
        }

        /// <summary>
        /// Upsize the space of the displayed images cuts
        /// </summary>
        private void upsize()
        {
            Transform imageOptions = m_imagesParent.Find("image options");
            bool upsizeState = true, donwsizeState = true;

            if (m_currentWidth == 20)
            {
                m_currentWidth = 150;
                donwsizeState = true;
                upsizeState = true;
                //m_minimizedState = false;
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

            m_imagesParent.Find("image_displays_panel").GetComponent<LayoutElement>().minWidth = m_currentWidth;
            imageOptions.Find("donwsize button").GetComponent<Button>().interactable = donwsizeState;
            imageOptions.Find("upsize button").GetComponent<Button>().interactable = upsizeState;
        }

        /// <summary>
        /// Downsize the space of the displayed images cuts
        /// </summary>
        private void donwsize()
        {
            Transform imageOptions = m_imagesParent.Find("image options");
            bool upsizeState = true, donwsizeState = true;

            if (m_currentWidth == 150)
            {
                m_currentWidth = 20;
                donwsizeState = false;
                upsizeState = true;
                //m_minimizedState = true;
            }
            else if (m_currentWidth == 300)
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

            m_imagesParent.Find("image_displays_panel").GetComponent<LayoutElement>().minWidth = m_currentWidth;
            imageOptions.Find("donwsize button").GetComponent<Button>().interactable = donwsizeState;
            imageOptions.Find("upsize button").GetComponent<Button>().interactable = upsizeState;
        }

        public void update_images_to_display(List<Texture2D> images, int columnId, int planeNb)
        {
            m_currentPlanesNb = planeNb;
            m_textures = images;

            for(int ii = 0; ii < m_textures.Count; ++ii)
            {
                Destroy(m_texturesOverlay[ii].gameObject.GetComponent<Image>().sprite);

                if (m_textures[ii] != null)
                {
                    m_texturesOverlay[ii].gameObject.GetComponent<Image>().sprite = Sprite.Create(m_textures[ii],
                        new Rect(0, 0, m_textures[ii].width, m_textures[ii].height), new Vector2(0, 0));
                }
            }
        }

        public void update_visiblity(bool visibility)
        {
            for(int ii = 0; ii < m_maxPlaneNb; ++ii)
            {
                bool enabled = (ii < m_currentPlanesNb) && visibility;  
                m_texturesOverlay[ii].SetActive(enabled);
                m_texturesGOContainer[ii].SetActive(enabled);
            }
        }

        public void update_UI_position()
        {
            for (int ii = 0; ii < m_currentPlanesNb; ++ii)
            {
                if (m_textures[ii].width > 0)
                {
                    Rect rectCamera = CamerasManager.GetScreenRectangle(m_texturesGOContainer[ii].gameObject.GetComponent<RectTransform>(), m_backGroundCamera);
                    RectTransform rectTransform = m_texturesOverlay[ii].gameObject.GetComponent<RectTransform>();
                    rectTransform.position = rectCamera.position + new Vector2(0, rectCamera.height);
                    rectTransform.sizeDelta = new Vector2(rectCamera.width, rectCamera.height);
                }
            }
        }
    }

    /// <summary>
    /// A controller for the cuts display images
    /// </summary>
    public class CutsDisplayController : MonoBehaviour
    {
        #region members       

        private bool m_spIsActive = false; /**< is sp scene active */
        private bool m_mpIsActive = false; /**< is mp scene active */

        private CutsDisplayMenu m_spMenu = null;
        private CutsDisplayMenu m_mpMenu = null;

        public Transform m_imagesParent;
        public Transform m_overlayParent;

        #endregion members

        #region functions

        /// <summary>
        /// Init the contoller
        /// </summary>
        /// <param name="scenesManager"></param>
        public void init(ScenesManager scenesManager)
        {            
            GameObject spMenuGO = new GameObject("sp cut display menu");
            spMenuGO.gameObject.AddComponent<CutsDisplayMenu>();
            spMenuGO.transform.SetParent(transform);
            m_spMenu = spMenuGO.GetComponent<CutsDisplayMenu>();
            //m_spMenu.init(scenesManager.CamerasManager.background_camera(), m_imagesParent.Find("sp cut images display panel"), m_overlayParent.Find("SP").Find("image cut display"));

            GameObject mpMenuGO = new GameObject("mp cut display menu");
            mpMenuGO.gameObject.AddComponent<CutsDisplayMenu>();
            mpMenuGO.transform.SetParent(transform);
            m_mpMenu = mpMenuGO.GetComponent<CutsDisplayMenu>();
            //m_mpMenu.init(scenesManager.CamerasManager.background_camera(), m_imagesParent.Find("mp cut images display panel"), m_overlayParent.Find("MP").Find("image cut display"));

            update_UI();
            /*
            scenesManager.SinglePatientScene.UpdateCutsInUI.AddListener((textures, idColumn, nbPlanes) =>
            {                
                m_spIsActive = true;
                m_mpIsActive = false;
                m_spMenu.update_images_to_display(textures, idColumn, nbPlanes);
                update_UI();
            });
            scenesManager.MultiPatientsScene.UpdateCutsInUI.AddListener((textures, idColumn, nbPlanes) =>
            {
                m_spIsActive = false;
                m_mpIsActive = true;
                m_mpMenu.update_images_to_display(textures, idColumn, nbPlanes);
                update_UI();
            });
            */
        }

        /// <summary>
        /// Update the UI with the current mode and activity
        /// </summary>
        public void update_UI()
        {
            m_spMenu.update_visiblity(m_spIsActive);
            m_imagesParent.Find("sp cut images display panel").gameObject.SetActive(m_spIsActive);

            m_mpMenu.update_visiblity(m_mpIsActive);
            m_imagesParent.Find("mp cut images display panel").gameObject.SetActive(m_mpIsActive);
        }

        /// <summary>
        /// Update the position of the UI in the scene
        /// </summary>
        public void update_UI_position()
        {
            if(m_spIsActive)
                m_spMenu.update_UI_position();
            if(m_mpIsActive)
                m_mpMenu.update_UI_position();            
        }

        /// <summary>
        /// Update the images to display
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="images"></param>
        /// <param name="columnId"></param>
        /// <param name="planeNb"></param>
        private void update_images_to_display(bool spScene, List<Texture2D> images, int columnId, int planeNb)
        {
            UnityEngine.Profiling.Profiler.BeginSample("TEST- update_images_to_display - images");

            if (spScene)
                m_spMenu.update_images_to_display(images, columnId, planeNb);
            else
                m_mpMenu.update_images_to_display(images, columnId, planeNb);

            UnityEngine.Profiling.Profiler.EndSample();
        }

        /// <summary>
        /// Set the current scene images cuts to display and its visibility
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="visibility"></param>
        public void update_UI_visibility(bool spScene, bool visibility)
        {
            set_scene_to_display(spScene);
            m_spIsActive = m_spIsActive && visibility;
            m_mpIsActive = m_mpIsActive && visibility;
            update_UI();
        }        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="spScene"></param>
        public void set_scene_to_display(bool spScene)
        {
            m_spIsActive = spScene;
            m_mpIsActive = !m_spIsActive;
        }

        #endregion functions
    }
}