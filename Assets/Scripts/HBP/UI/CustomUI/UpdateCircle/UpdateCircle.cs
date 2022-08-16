using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI
{
    public class UpdateCircle : MonoBehaviour
    {
        #region Properties
        [SerializeField] private Image m_Image;
        [SerializeField] private RectTransform m_RectTransform;

        private bool m_Filling;
        private bool m_IsActive;

        private float m_RotateSpeed = 1;
        private float m_FillInSpeed = 0.75f;
        private float m_FillOutSpeed = 0.8785f;
        #endregion

        #region Private Methods
        private void Update()
        {
            if (m_IsActive)
            {
                m_RectTransform.Rotate(0, 0, m_RotateSpeed * Time.deltaTime * 270);
                if (m_Filling)
                {
                    m_Image.fillAmount += m_FillInSpeed * Time.deltaTime;
                    if (m_Image.fillAmount > 0.9f)
                    {
                        m_Image.fillAmount = 0.9f;
                        m_Filling = false;
                    }
                }
                else
                {
                    m_Image.fillAmount -= m_FillOutSpeed * Time.deltaTime;
                    if (m_Image.fillAmount < 0.1f)
                    {
                        m_Image.fillAmount = 0.1f;
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