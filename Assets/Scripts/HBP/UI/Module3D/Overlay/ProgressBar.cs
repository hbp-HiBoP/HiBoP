using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// Progress bar used to have feedback when computing the activity on the brain
    /// </summary>
    public class ProgressBar : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Image that fills the progress bar
        /// </summary>
        [SerializeField] private RectTransform m_Fill;
        /// <summary>
        /// Exact percentage of the current progress
        /// </summary>
        [SerializeField] private Text m_ProgressText;
        /// <summary>
        /// Text to display on the progress bar
        /// </summary>
        [SerializeField] private Text m_Message;
        /// <summary>
        /// Small circle that shows the computing is still in progress
        /// </summary>
        [SerializeField] private global::Tools.Unity.UpdateCircle m_UpdateCircle;

        /// <summary>
        /// Previous value of the progress
        /// </summary>
        private float m_PreviousProgress;
        /// <summary>
        /// Target value of the progress
        /// </summary>
        private float m_Progress;
        /// <summary>
        /// Time since the last call to a progress update
        /// </summary>
        private float m_TimeSinceLastCall;
        /// <summary>
        /// Total duration of the current progress step
        /// </summary>
        private float m_TotalTime;
        #endregion

        #region Private Methods
        private void Update()
        {
            float lerpValue = m_TimeSinceLastCall / m_TotalTime;
            if (lerpValue < 2.0f)
            {
                float progress = Mathf.Lerp(m_PreviousProgress, m_Progress, lerpValue);
                m_Fill.anchorMax = new Vector2(progress, 1.0f);
                m_ProgressText.text = string.Format("{0}%", ((int)(progress * 100)).ToString());
                m_TimeSinceLastCall += Time.deltaTime;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Open the progress bar and reset all members
        /// </summary>
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
        /// <summary>
        /// Close the progress bar
        /// </summary>
        public void Close()
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
                m_UpdateCircle.StopAnimation();
            }
        }
        /// <summary>
        /// Request a new progress step to the progress bar
        /// </summary>
        /// <param name="progress">Target progress of this step</param>
        /// <param name="message">Message to display for this step</param>
        /// <param name="duration">Duration of this step</param>
        public void Progress(float progress, string message, float duration)
        {
            m_PreviousProgress = m_Fill.anchorMax.x;
            m_Message.text = message;
            m_Progress = progress;
            m_TimeSinceLastCall = 0;
            m_TotalTime = duration;
        }
        #endregion
    }
}