
/**
 * \file    GlobalMenuContrller.cs
 * \author  Lance Florian
 * \date    24/01/2017
 * \brief   Define GlobalMenuContrller class
 */

// unity
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Module3D
{
    /// <summary>
    /// ...
    /// </summary>
    public class GlobalMenuController : MonoBehaviour
    {
        #region Properties

        public bool m_isColormapMinimized = true;
        public bool m_isBrainColorMinimized = true;
        public bool m_isCutColorMinimized = true;
        public Transform m_transform;  /**< scene ratio transform */
        private GameObject m_globalMenuGO = null;

        public ColormapController m_colorMapController = null;

        #endregion

        #region Public Methods

        /// <summary>
        /// Init the controller
        /// </summary>
        /// <param name="scenesManager"></param>
        public void Initialize(ScenesManager scenesManager)
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
            for(int ii = 1; ii < 15; ++ii) // FIXME : Add listeners manually with the corresponding color (not a for loop) : better stability
            {
                int id = ii;
                baseButtons.Find("colormap" + id + " button").GetComponent<Button>().onClick.AddListener(() =>
                {
                    ColorType color = (ColorType)(id - 1); // FIXME : remove this fixme when the for loop is no more
                    m_colorMapController.update_colormap(color);
                    foreach (Base3DScene scene in scenesManager.Scenes)
                    {
                        scene.UpdateColormap(color);
                    }                
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
                foreach (Base3DScene scene in scenesManager.Scenes)
                {
                    scene.UpdateBrainSurfaceColor(ColorType.BrainColor);
                }
            });
            baseButtons.Find("braincolor2 button").GetComponent<Button>().onClick.AddListener(() =>
            {
                foreach (Base3DScene scene in scenesManager.Scenes)
                {
                    scene.UpdateBrainSurfaceColor(ColorType.White);
                }
            });
            baseButtons.Find("braincolor3 button").GetComponent<Button>().onClick.AddListener(() =>
            {
                foreach (Base3DScene scene in scenesManager.Scenes)
                {
                    scene.UpdateBrainSurfaceColor(ColorType.Grayscale);
                }
            });
            baseButtons.Find("braincolor4 button").GetComponent<Button>().onClick.AddListener(() =>
            {
                foreach (Base3DScene scene in scenesManager.Scenes)
                {
                    scene.UpdateBrainSurfaceColor(ColorType.Default);
                }
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
                foreach (Base3DScene scene in scenesManager.Scenes)
                {
                    scene.UpdateBrainCutColor(ColorType.Default);
                }
            });
            baseButtons.Find("cutcolor2 button").GetComponent<Button>().onClick.AddListener(() =>
            {
                foreach (Base3DScene scene in scenesManager.Scenes)
                {
                    scene.UpdateBrainCutColor(ColorType.Grayscale);
                }
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
            m_colorMapController.update_colormap(ColorType.MatLab);
        }

        public void switch_UI_Visibility()
        {
            m_globalMenuGO.SetActive(!m_globalMenuGO.activeSelf);
        }



        #endregion
    }

}