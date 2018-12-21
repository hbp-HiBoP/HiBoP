using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class ProgressBar : MonoBehaviour
    {
        #region Properties
        [SerializeField] private RectTransform m_Fill;
        [SerializeField] private Text m_ProgressText;
        [SerializeField] private Text m_Message;
        
        private float m_Progress;
        private float m_LerpValue;
        #endregion

        #region Private Methods
        private void Update()
        {
            if (m_LerpValue < 2.0f)
            {
                float progress = Mathf.Lerp(m_Fill.anchorMax.x, m_Progress, m_LerpValue);
                m_Fill.anchorMax = new Vector2(progress, 1.0f);
                m_ProgressText.text = string.Format("{0}%", ((int)(progress * 100)).ToString());
                m_LerpValue += Time.deltaTime * 3;
            }
        }
        #endregion

        #region Public Methods
        public void Open()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                m_Fill.anchorMax = new Vector2(0.0f, 1.0f);
                m_Progress = 0;
            }
        }
        public void Close()
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }
        public void Progress(float progress, string message)
        {
            m_Message.text = message;
            m_Progress = progress;
            m_LerpValue = 0;
        }
        #endregion
    }
}