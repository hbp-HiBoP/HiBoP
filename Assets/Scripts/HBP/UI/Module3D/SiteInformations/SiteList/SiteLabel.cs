using UnityEngine;
using UnityEngine.EventSystems;
using HBP.Core.Enums;

namespace HBP.UI.Module3D
{
    public class SiteLabel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Properties
        private Core.Object3D.Site m_Site;
        private bool m_IsInside;
        #endregion

        #region Private Methods
        private void Update()
        {
            if (m_IsInside)
            {
                ApplicationState.Module3D.OnDisplaySiteInformation.Invoke(new Core.Object3D.SiteInfo(m_Site, true, Input.mousePosition, SiteInformationDisplayMode.Anatomy));
            }
        }
        #endregion

        #region Public Methods
        public void Initialize(Core.Object3D.Site site)
        {
            m_Site = site;
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            m_IsInside = true;
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            ApplicationState.Module3D.OnDisplaySiteInformation.Invoke(new Core.Object3D.SiteInfo(null, false, Input.mousePosition));
            m_IsInside = false;
        }
        #endregion
    }
}