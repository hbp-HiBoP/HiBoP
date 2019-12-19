using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class SiteConditionsProgressBar : MonoBehaviour
    {
        #region Properties
        [SerializeField] private RectTransform m_RectTransform;
        [SerializeField] private Text m_StartText;
        [SerializeField] private Text m_StopText;
        #endregion

        #region Public Methods
        public void Begin()
        {
            gameObject.SetActive(true);
            m_StartText.gameObject.SetActive(false);
            m_StopText.gameObject.SetActive(true);
            m_RectTransform.anchorMax = new Vector2(0.0f, 1.0f);
        }
        public void End()
        {
            m_StartText.gameObject.SetActive(true);
            m_StopText.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }
        public void Progress(float progress)
        {
            m_RectTransform.anchorMax = new Vector2(progress, 1.0f);
        }
        #endregion
    }
}