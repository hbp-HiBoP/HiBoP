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
        [SerializeField] private global::Tools.Unity.UpdateCircle m_UpdateCircle;

        private float m_PreviousProgress;
        private float m_Progress;
        private float m_LerpValue;
        private float m_Speed;
        #endregion

        #region Private Methods
        private void Update()
        {
            if (m_LerpValue < 2.0f)
            {
                float progress = Mathf.Lerp(m_PreviousProgress, m_Progress, m_LerpValue);
                m_Fill.anchorMax = new Vector2(progress, 1.0f);
                m_ProgressText.text = string.Format("{0}%", ((int)(progress * 100)).ToString());
                m_LerpValue += Time.deltaTime * m_Speed;
            }
        }
        #endregion

        #region Public Methods
        public void Open()
        {
            if (!gameObject.activeSelf)
            {
                m_UpdateCircle.StartAnimation();
                gameObject.SetActive(true);
                m_Fill.anchorMax = new Vector2(0.0f, 1.0f);
                m_Progress = 0;
                m_PreviousProgress = 0;
            }
        }
        public void Close()
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
                m_UpdateCircle.StopAnimation();
            }
        }
        public void Progress(float progress, string message, float speed)
        {
            m_PreviousProgress = m_Fill.anchorMax.x;
            m_Message.text = message;
            m_Progress = progress;
            m_Speed = speed;
            m_LerpValue = 0;
        }
        #endregion
    }
}