using UnityEngine;

namespace HBP.UI.Toolbar
{
    public class AtlasToolbar : Toolbar
    {
        #region Properties
        /// <summary>
        /// Change the state of the atlas
        /// </summary>
        [SerializeField] private AtlasState m_AtlasState;
        /// <summary>
        /// Change the parameters of the IBC contrasts
        /// </summary>
        [SerializeField] private FMRIAtlasParameters m_FMRIAtlasParameters;
        /// <summary>
        /// Change the contrast
        /// </summary>
        [SerializeField] private IBCSelector m_IBCSelector;

        [SerializeField] private DiFuMoSelector m_DiFuMoSelector;

        [SerializeField] private LocalizersSelector m_LocalizersSelector;

        [SerializeField] private LocalizersTimeline m_LocalizersTimeline;
        #endregion

        #region Private Methods
        /// <summary>
        /// Link elements to the toolbar
        /// </summary>
        /// <param name="parent">Transform of the toolbar</param>
        protected override void AddTools()
        {
            m_Tools.Add(m_AtlasState);
            m_Tools.Add(m_IBCSelector);
            m_Tools.Add(m_FMRIAtlasParameters);
            m_Tools.Add(m_DiFuMoSelector);
            m_Tools.Add(m_LocalizersSelector);
            m_Tools.Add(m_LocalizersTimeline);
        }
        #endregion
    }
}