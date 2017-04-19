using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using HBP.Data.Visualisation;

namespace HBP.UI.Visualisation
{
    public class MultiVisualisationItem : Tools.Unity.Lists.ListItemWithActions<MultiPatientsVisualisation>
    {
        #region Properties
        [SerializeField]
        private Text m_visualisationName;
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
        protected override void SetObject(MultiPatientsVisualisation visualisation)
        {
            m_visualisationName.text = visualisation.Name;
            m_patientList.Display(visualisation.Patients.ToArray());
            m_nbPatient.text = visualisation.Patients.Count.ToString();
            m_columnList.Display(visualisation.Columns.ToArray());
            m_nbColumn.text = visualisation.Columns.Count.ToString();
        }
        #endregion
    }
}