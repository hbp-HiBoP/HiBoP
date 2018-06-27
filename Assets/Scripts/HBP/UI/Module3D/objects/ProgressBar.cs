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
        #endregion

        #region Public Methods
        public void Open()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                m_Fill.anchorMax = new Vector2(0.0f, 1.0f);
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
            m_Fill.anchorMax = new Vector2(progress, 1.0f);
            m_ProgressText.text = string.Format("{0}%", ((int)(progress * 100)).ToString());
            m_Message.text = message;
        }
        #endregion
    }
}