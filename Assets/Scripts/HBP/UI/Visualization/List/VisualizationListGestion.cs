using UnityEngine;
using HBP.UI.Lists;

namespace HBP.UI.Visualization
{
    public class VisualizationListGestion : ListGestion<Core.Data.Visualization>
    {
        #region Properties
        [SerializeField] VisualizationList m_List;
        public override ActionableList<Core.Data.Visualization> List => m_List;

        [SerializeField] VisualizationCreator m_ObjectCreator;
        public override ObjectCreator<Core.Data.Visualization> ObjectCreator => m_ObjectCreator;
        #endregion
    }
}