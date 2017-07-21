using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace HBP.UI.Visualization
{
    public class VisualizationItem : Tools.Unity.Lists.ActionnableItem<Data.Visualization.Visualization>
    {
        #region Properties
        [SerializeField] Text m_VisualizationName;
        [SerializeField] Text m_NbPatient;
        [SerializeField] PatientNameList m_PatientList;
        [SerializeField] Text m_NbColumn;
        [SerializeField] ColumnList m_ColumnList;
        public override Data.Visualization.Visualization Object
        {
            get
            {
                return base.Object;
            }

            set
            {
                base.Object = value;
                m_VisualizationName.text = value.Name;
                m_PatientList.Objects = value.Patients.ToArray();
                m_NbPatient.text = value.Patients.Count.ToString();
                m_ColumnList.Objects = value.Columns.ToArray();
                m_NbColumn.text = value.Columns.Count.ToString();
            }
        }
        #endregion
    }
}