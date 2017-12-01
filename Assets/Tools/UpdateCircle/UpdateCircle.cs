using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity
{
    public class UpdateCircle : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        private Image m_Image;
        [SerializeField]
        private RectTransform m_RectTransform;

        private bool m_Filling;
        private bool m_IsActive;
        #endregion

        #region Private Methods
        private void Update()
        {
            if (m_IsActive)
            {
                m_RectTransform.Rotate(0, 0, Time.deltaTime * 270);
                if (m_Filling)
                {
                    m_Image.fillAmount += Time.deltaTime;
                    if (m_Image.fillAmount > 0.95f)
                    {
                        m_Image.fillAmount = 0.95f;
                        m_Filling = false;
                    }
                }
                else
                {
                    m_Image.fillAmount -= Time.deltaTime;
                    if (m_Image.fillAmount < 0.05f)
                    {
                        m_Image.fillAmount = 0.05f;
                        m_Filling = true;
                    }
                }
            }
        }
        #endregion

        #region Public Methods
        public void StartAnimation()
        {
            gameObject.SetActive(true);
            m_IsActive = true;
        }
        public void StopAnimation()
        {
            gameObject.SetActive(false);
            m_IsActive = false;
        }
        #endregion
    }
}