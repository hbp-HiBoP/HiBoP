using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using HBP.Data.Visualisation;

namespace HBP.UI.Visualisation
{
    public class SingleVisualisationItem : Tools.Unity.Lists.ListItemWithActions<SinglePatientVisualisation>
    {
        #region Attributs
        [SerializeField]
        private Text m_visualisationName;
        [SerializeField]
        private Text m_patientName;
        [SerializeField]
        private Text m_nbColumn;
        [SerializeField]
        private ColumnList m_columnList;
        #endregion

        #region Private Methods
        protected override void SetObject(SinglePatientVisualisation visualisation)
        {
            m_visualisationName.text = visualisation.Name;
            m_patientName.text = visualisation.Patient.Name;
            m_nbColumn.text = visualisation.Columns.Count.ToString();
            m_columnList.Display(visualisation.Columns.ToArray());
        }
        #endregion
    }
}
