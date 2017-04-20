
/**
 * \file    GlobalMenuContrller.cs
 * \author  Lance Florian
 * \date    24/01/2017
 * \brief   Define GlobalMenuContrller class
 */

// unity
using UnityEngine;
using UnityEngine.UI;

namespace HBP.VISU3D
{
    /// <summary>
    /// ...
    /// </summary>
    public class GlobalMenuController : MonoBehaviour
    {
        #region members

        public bool m_isColormapMinimized = true;
        public bool m_isBrainColorMinimized = true;
        public bool m_isCutColorMinimized = true;
        public Transform m_transform;  /**< scene ratio transform */
        private GameObject m_globalMenuGO = null;

        public ColormapController m_colorMapController = null;

        #endregion members

        #region functions

        /// <summary>
        /// Init the controller
        /// </summary>
        /// <param name="scenesManager"></param>
        public void init(ScenesManager scenesManager)
        {
            m_globalMenuGO = Instantiate(GlobalGOPreloaded.globalLeftMenu);
            m_globalMenuGO.transform.SetParent(m_transform);
            m_globalMenuGO.transform.localPosition = new Vector3(0, 0, 0);
            m_globalMenuGO.transform.localScale = new Vector3(1, 1, 1);
            m_globalMenuGO.name = "Global menu";

            m_globalMenuGO.transform.Find("title panel").Find("close button").GetComponent<Button>().onClick.AddListener(() =>
            {
                switch_UI_Visibility();
            });

            Transform baseButtons = m_globalMenuGO.transform.Find("panel").Find("colormap buttons");
            for(int ii = 1; ii < 15; ++ii)
            {
                int id = ii;
                baseButtons.Find("colormap" + id + " button").GetComponent<Button>().onClick.AddListener(() =>
                {
                    m_colorMapController.update_colormap(id - 1);
                    scenesManager.SPScene.update_colormap(id - 1);
                    scenesManager.MPScene.update_colormap(id - 1);                    
                });
            }
            Button minimizeColorMapButton = m_globalMenuGO.transform.Find("panel").Find("Colormap parent").Find("Colormap button").GetComponent<Button>();
            minimizeColorMapButton.onClick.AddListener(delegate
            {
                m_isColormapMinimized = !m_isColormapMinimized;
                m_globalMenuGO.transform.Find("panel").Find("Colormap parent").Find("expand image").gameObject.SetActive(m_isColormapMinimized);
                m_globalMenuGO.transform.Find("panel").Find("Colormap parent").Find("minimize image").gameObject.SetActive(!m_isColormapMinimized);
                m_globalMenuGO.transform.Find("panel").Find("colormap buttons").gameObject.SetActive(!m_isColormapMinimized);
            });


            baseButtons = m_globalMenuGO.transform.Find("panel").Find("braincolor buttons");
            baseButtons.Find("braincolor1 button").GetComponent<Button>().onClick.AddListener(() =>
            {
                scenesManager.SPScene.update_brain_surface_color(15);
                scenesManager.MPScene.update_brain_surface_color(15);
            });
            baseButtons.Find("braincolor2 button").GetComponent<Button>().onClick.AddListener(() =>
            {
                scenesManager.SPScene.update_brain_surface_color(16);
                scenesManager.MPScene.update_brain_surface_color(16);
            });
            baseButtons.Find("braincolor3 button").GetComponent<Button>().onClick.AddListener(() =>
            {
                scenesManager.SPScene.update_brain_surface_color(17);
                scenesManager.MPScene.update_brain_surface_color(17);
            });
            baseButtons.Find("braincolor4 button").GetComponent<Button>().onClick.AddListener(() =>
            {
                scenesManager.SPScene.update_brain_surface_color(14);
                scenesManager.MPScene.update_brain_surface_color(14);
            });

            Button minimizeBrainColorButton = m_globalMenuGO.transform.Find("panel").Find("Braincolor parent").Find("Braincolor button").GetComponent<Button>();
            minimizeBrainColorButton.onClick.AddListener(delegate
            {
                m_isBrainColorMinimized = !m_isBrainColorMinimized;
                m_globalMenuGO.transform.Find("panel").Find("Braincolor parent").Find("expand image").gameObject.SetActive(m_isBrainColorMinimized);
                m_globalMenuGO.transform.Find("panel").Find("Braincolor parent").Find("minimize image").gameObject.SetActive(!m_isBrainColorMinimized);
                m_globalMenuGO.transform.Find("panel").Find("braincolor buttons").gameObject.SetActive(!m_isBrainColorMinimized);
            });


            baseButtons = m_globalMenuGO.transform.Find("panel").Find("cutcolor buttons");
            baseButtons.Find("cutcolor1 button").GetComponent<Button>().onClick.AddListener(() =>
            {
                scenesManager.SPScene.update_brain_cut_color(14);
                scenesManager.MPScene.update_brain_cut_color(14);
            });
            baseButtons.Find("cutcolor2 button").GetComponent<Button>().onClick.AddListener(() =>
            {
                scenesManager.SPScene.update_brain_cut_color(0);
                scenesManager.MPScene.update_brain_cut_color(0);
            });

            Button minimizeCutColorButton = m_globalMenuGO.transform.Find("panel").Find("Cutcolor parent").Find("Cutcolor button").GetComponent<Button>();
            minimizeCutColorButton.onClick.AddListener(delegate
            {
                m_isCutColorMinimized = !m_isCutColorMinimized;
                m_globalMenuGO.transform.Find("panel").Find("Cutcolor parent").Find("expand image").gameObject.SetActive(m_isCutColorMinimized);
                m_globalMenuGO.transform.Find("panel").Find("Cutcolor parent").Find("minimize image").gameObject.SetActive(!m_isCutColorMinimized);
                m_globalMenuGO.transform.Find("panel").Find("cutcolor buttons").gameObject.SetActive(!m_isCutColorMinimized);
            });

            // set default colormap
            m_colorMapController.update_colormap(13);
        }

        public void switch_UI_Visibility()
        {
            m_globalMenuGO.SetActive(!m_globalMenuGO.activeSelf);
        }



        #endregion functions
    }

}