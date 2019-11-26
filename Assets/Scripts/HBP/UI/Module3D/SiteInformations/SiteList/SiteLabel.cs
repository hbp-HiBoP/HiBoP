using HBP.Module3D;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HBP.UI.Module3D
{
    public class SiteLabel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Properties
        private HBP.Module3D.Site m_Site;
        private bool m_IsInside;
        #endregion

        #region Private Methods
        private void Update()
        {
            if (m_IsInside)
            {
                ApplicationState.Module3D.OnDisplaySiteInformation.Invoke(new SiteInfo(m_Site, true, Input.mousePosition, Data.Enums.SiteInformationDisplayMode.Anatomy));
            }
        }
        #endregion

        #region Public Methods
        public void Initialize(HBP.Module3D.Site site)
        {
            m_Site = site;
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
        #endregion
    }
}