using UnityEngine;

namespace HBP.UI.Module3D
{
    public class ConfigurationToolbar : Toolbar
    {
        #region Properties
        /// <summary>
        /// Tool that allows saving, loading and reseting the configuration of the selected scene
        /// </summary>
        [SerializeField] private Tools.ConfigurationLoaderSaver m_ConfigurationLoaderSaver;
        [SerializeField] private Tools.CopyVisualization m_CopyVisualization;
        #endregion

        #region Private Methods
        /// <summary>
        /// Link elements to the toolbar
        /// </summary>
        /// <param name="parent">Transform of the toolbar</param>
        protected override void AddTools()
        {
            m_Tools.Add(m_ConfigurationLoaderSaver);
            m_Tools.Add(m_CopyVisualization);
        }
        #endregion
    }
}