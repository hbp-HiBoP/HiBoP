using HBP.UI.Visualization;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    public class VisualizationSelector : ObjectSelector<Data.Visualization.Visualization>
    {
        #region Properties
        [SerializeField] VisualizationList m_List;
        protected override SelectableList<Data.Visualization.Visualization> List => m_List;
        #endregion
    }
}