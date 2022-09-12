using UnityEngine;
using HBP.Data.Preferences;

namespace HBP.Data.Tools
{
    public class PerformanceManager : MonoBehaviour
    {
        #region Properties
        [SerializeField] private GameObject m_DarkImage;
        private float m_TimeSinceLastAction = 0;
        #endregion

        #region Private Methods
        private void Update()
        {
            m_TimeSinceLastAction += Time.deltaTime;
            if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0 || Input.anyKey || Input.anyKeyDown)
            {
                m_TimeSinceLastAction = 0;
            }
            if (m_TimeSinceLastAction > PreferencesManager.UserPreferences.General.System.SleepModeAfter * 60)
            {
                Application.targetFrameRate = 1;
                m_DarkImage.SetActive(true);
            }
            else
            {
                Application.targetFrameRate = PreferencesManager.UserPreferences.General.System.TargetFramerate;
                m_DarkImage.SetActive(false);
            }
        }
        #endregion
    }
}