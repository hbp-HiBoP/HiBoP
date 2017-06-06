using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Visualization;

namespace HBP.UI.Visualization
{
    public class SingleVisualizationItem : Tools.Unity.Lists.ListItemWithActions<SinglePatientVisualization>
    {
        #region Attributs
        [SerializeField]
        private Text m_visualizationName;
        [SerializeField]
        private Text m_patientName;
        [SerializeField]
        private Text m_nbColumn;
        [SerializeField]
        private ColumnList m_columnList;
        #endregion

        #region Private Methods
        protected override void SetObject(SinglePatientVisualization visualization)
        {
            m_visualizationName.text = visualization.Name;
            m_patientName.text = visualization.Patient.Name;
            m_nbColumn.text = visualization.Columns.Count.ToString();
            m_columnList.Display(visualization.Columns.ToArray());
        }
        #endregion
    }
}
