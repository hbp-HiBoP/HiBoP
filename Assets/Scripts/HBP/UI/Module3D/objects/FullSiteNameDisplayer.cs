using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class FullSiteNameDisplayer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Properties
        [SerializeField] GameObject m_FullSiteNameGameObject;
        [SerializeField] Text m_SiteName;
        [SerializeField] Text m_FullSiteName;
        #endregion

        #region Public Methods
        public void OnPointerEnter(PointerEventData data)
        {
            m_FullSiteName.text = m_SiteName.text;
            m_FullSiteNameGameObject.SetActive(true);
        }
        public void OnPointerExit(PointerEventData data)
        {
            m_FullSiteNameGameObject.SetActive(false);
        }
        #endregion
    }
}