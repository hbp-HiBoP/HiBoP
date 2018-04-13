using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.Module3D
{
    public class SiteConditionsProgressBar : MonoBehaviour
    {
        #region Properties
        [SerializeField] private RectTransform m_RectTransform;
        #endregion

        #region Public Methods
        public void Begin()
        {
            gameObject.SetActive(true);
            m_RectTransform.anchorMax = new Vector2(0.0f, 1.0f);
        }
        public void End()
        {
            gameObject.SetActive(false);
        }
        public void Progress(float progress)
        {
            m_RectTransform.anchorMax = new Vector2(progress, 1.0f);
        }
        #endregion
    }
}