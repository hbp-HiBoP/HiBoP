using HBP.UI.Visualization;
using UnityEngine;

namespace HBP.UI
{
    public class VisualizationSelector : ObjectSelector<Data.Visualization.Visualization>
    {
        #region Properties
        [SerializeField] VisualizationList m_VisualizationList;
        #endregion

        #region Public Methods
        protected override void Initialize()
        {
            m_List = m_VisualizationList;
            base.Initialize();
        }
        #endregion
    }
}