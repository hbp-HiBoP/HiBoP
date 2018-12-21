using HBP.Module3D;
using NewTheme.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class CutSite : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        #region Properties
        private bool m_IsInside;
        private Site m_Site;
        private Base3DScene m_Scene;
        [SerializeField] private Image m_Image;
        [SerializeField] private RectTransform m_RectTransform;
        [SerializeField] private GameObject m_SelectionPrefab;
        #endregion

        #region Private Methods
        private void Update()
        {
            if (m_IsInside)
            {
                ApplicationState.Module3D.OnDisplaySiteInformation.Invoke(new SiteInfo(m_Site, true, Input.mousePosition, Data.Enums.SiteInformationDisplayMode.Light));
            }
        }
        #endregion

        #region Public Methods
        public void Initialize(Base3DScene scene, Site site, Vector2 position)
        {
            m_Scene = scene;
            m_Site = site;
            m_RectTransform.anchorMin = position;
            m_RectTransform.anchorMax = position;
            m_RectTransform.anchoredPosition = Vector2.zero;
            Color color = m_Site.GetComponent<MeshRenderer>().sharedMaterial.color; // FIXME : Use ThemeElements after the merge
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
            m_IsInside = true;
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            ApplicationState.Module3D.OnDisplaySiteInformation.Invoke(new SiteInfo(null, false, Input.mousePosition));
            m_IsInside = false;
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            m_Site.IsSelected = true;
        }
        #endregion
    }
}