using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HBP.Core.Enums;
using HBP.Display.Module3D;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// Class representing a site on the cut image in the cuts panel
    /// </summary>
    public class CutSite : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        #region Properties
        /// <summary>
        /// True if the cursor is currently inside the site
        /// </summary>
        private bool m_IsCursorInside;
        /// <summary>
        /// Corresponding site in the 3D scene
        /// </summary>
        private Core.Object3D.Site m_Site;
        /// <summary>
        /// Parent scene of the cuts panel
        /// </summary>
        private Base3DScene m_Scene;
        /// <summary>
        /// Image used to display the site on the texture
        /// </summary>
        [SerializeField] private Image m_Image;
        /// <summary>
        /// Reference to the RectTransform of this gameObject
        /// </summary>
        [SerializeField] private RectTransform m_RectTransform;
        /// <summary>
        /// Prefab to show feedback of the selected site on the cut image
        /// </summary>
        [SerializeField] private GameObject m_SelectionPrefab;
        #endregion

        #region Private Methods
        private void Update()
        {
            if (m_IsCursorInside)
            {
                HBP3DModule.OnDisplaySiteInformation.Invoke(new Core.Object3D.SiteInfo(m_Site, true, Input.mousePosition, SiteInformationDisplayMode.Anatomy));
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the cut site
        /// </summary>
        /// <param name="scene">Parent scene of the cut parameter controller</param>
        /// <param name="site">Site to display on the cut image</param>
        /// <param name="position">Position ratio of the cut site on the image</param>
        public void Initialize(Base3DScene scene, Core.Object3D.Site site, Vector2 position)
        {
            m_Scene = scene;
            m_Site = site;
            m_RectTransform.anchorMin = position;
            m_RectTransform.anchorMax = position;
            m_RectTransform.anchoredPosition = Vector2.zero;
            Color color = m_Site.GetComponent<MeshRenderer>().sharedMaterial.color;
            m_Image.color = color;
            if (site.IsSelected)
            {
                RectTransform selectedRectTransform = Instantiate(m_SelectionPrefab, m_RectTransform).GetComponent<RectTransform>();
                selectedRectTransform.anchorMin = Vector2.zero;
                selectedRectTransform.anchorMax = Vector2.one;
                selectedRectTransform.anchoredPosition = Vector2.zero;
                selectedRectTransform.sizeDelta = new Vector2(6, 6);
            }
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            m_IsCursorInside = true;
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            HBP3DModule.OnDisplaySiteInformation.Invoke(new Core.Object3D.SiteInfo(null, false, Input.mousePosition));
            m_IsCursorInside = false;
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            m_Site.IsSelected = true;
        }
        #endregion
    }
}