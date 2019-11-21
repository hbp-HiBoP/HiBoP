using Tools.Unity.Components;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI.Visualization
{
    public class VisualizationListGestion : ListGestion<Data.Visualization.Visualization>
    {
        #region Properties
        [SerializeField] VisualizationList m_List;
        public override ActionableList<Data.Visualization.Visualization> List => m_List;

        [SerializeField] VisualizationCreator m_ObjectCreator;
        public override ObjectCreator<Data.Visualization.Visualization> ObjectCreator => m_ObjectCreator;
        #endregion
    }
}