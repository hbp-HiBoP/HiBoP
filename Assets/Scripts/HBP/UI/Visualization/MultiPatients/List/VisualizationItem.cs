using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace HBP.UI.Visualization
{
    public class VisualizationItem : Tools.Unity.Lists.ActionnableItem<Data.Visualization.Visualization>
    {
        #region Properties
        [SerializeField]
        private Text m_visualizationName;
        [SerializeField]
        private Text m_nbPatient;
        [SerializeField]
        private PatientNameList m_patientList;
        [SerializeField]
        private Text m_nbColumn;
        [SerializeField]
        private ColumnList m_columnList;
        #endregion

        #region Private Methods
        protected override void SetObject(Data.Visualization.Visualization visualization)
        {
            m_visualizationName.text = visualization.Name;
            m_patientList.Display(visualization.Patients.ToArray());
            m_nbPatient.text = visualization.Patients.Count.ToString();
            m_columnList.Display(visualization.Columns.ToArray());
            m_nbColumn.text = visualization.Columns.Count.ToString();
        }
        #endregion
    }
}