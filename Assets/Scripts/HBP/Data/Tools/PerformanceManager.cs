using HBP.Input;
using UnityEngine;
using HBP.Core.Preferences;
using HBP.Core.Tools;

namespace HBP.Data.Tools
{
    public class PerformanceManager : Manager<PerformanceManager>
    {
        #region Properties

        [SerializeField] private GameObject m_DarkImage;
        private float m_TimeSinceLastAction = 0;

        /// <summary>
        /// Temporarily prevents the application sleep mode from throttling the
        /// PlayerLoop. Intended for controlled operations such as rendering
        /// benchmarks; callers must restore the previous value afterwards.
        /// </summary>
        public bool SleepModeSuspended
        {
            get { return m_SleepModeSuspended; }
            set
            {
                m_SleepModeSuspended = value;
                m_TimeSinceLastAction = 0;
                Application.targetFrameRate = PersistentDataManager.UserPreferences.General.System.TargetFramerate;
                if (m_DarkImage != null)
                {
                    m_DarkImage.SetActive(false);
                }
            }
        }

        private bool m_SleepModeSuspended;

        #endregion

        #region Private Methods

        private void Update()
        {
            if (m_SleepModeSuspended)
            {
                return;
            }

            m_TimeSinceLastAction += Time.deltaTime;
            if (DesktopInput.MouseDelta.x != 0 || DesktopInput.MouseDelta.y != 0 || DesktopInput.IsAnyKeyPressed || DesktopInput.WasAnyKeyPressedThisFrame || DesktopInput.IsLeftMouseButtonPressed || DesktopInput.IsRightMouseButtonPressed || DesktopInput.IsMiddleMouseButtonPressed)
            {
                m_TimeSinceLastAction = 0;
            }

            if (m_TimeSinceLastAction > PersistentDataManager.UserPreferences.General.System.SleepModeAfter * 60)
            {
                Application.targetFrameRate = 1;
                m_DarkImage.SetActive(true);
            }
            else
            {
                Application.targetFrameRate = PersistentDataManager.UserPreferences.General.System.TargetFramerate;
                m_DarkImage.SetActive(false);
            }
        }

        #endregion
    }
}
