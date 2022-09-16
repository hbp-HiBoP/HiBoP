using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class FullSiteNameDisplayer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Properties
        /// <summary>
        /// GameObject under the site label to be displayed when the label is hovered
        /// </summary>
        [SerializeField] GameObject m_FullSiteNameGameObject;
        /// <summary>
        /// Initial label of the site
        /// </summary>
        [SerializeField] Text m_SiteName;
        /// <summary>
        /// Label of the site to be shown when displaying the full name
        /// </summary>
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