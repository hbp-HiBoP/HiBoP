using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Tools.Unity.Lists;
using NewTheme.Components;

namespace HBP.UI.Visualization
{
    public class VisualizationItem : ActionnableItem<Data.Visualization.Visualization>
    {
        #region Properties
        [SerializeField] Text m_NameText;
        [SerializeField] Text m_PatientsText;
        [SerializeField] Text m_ColumnsText;
        [SerializeField] State m_ErrorState;

        public override Data.Visualization.Visualization Object
        {
            get
            {
                return base.Object;
            }

            set
            {
                base.Object = value;

                m_NameText.text = value.Name;

                ThemeElement patientsThemeElement = m_PatientsText.GetComponent<ThemeElement>();
                int nbPatients = value.Patients.Count;
                m_PatientsText.text = nbPatients.ToString();
                if(nbPatients == 0) patientsThemeElement.Set(m_ErrorState);
                else patientsThemeElement.Set();

                // Columns.
                ThemeElement columnsThemeElement = m_ColumnsText.GetComponent<ThemeElement>();
                int nbColumns = value.Columns.Count;
                m_ColumnsText.text = nbColumns.ToString();
                if (nbColumns == 0) columnsThemeElement.Set(m_ErrorState);
                else columnsThemeElement.Set();
            }
        }
        #endregion

        #region Public Methods
        public void SetPatients()
        {
            //m_PatientsList.Initialize();
            //m_PatientsList.Objects = (from patient in m_Object.Patients select patient.Name).ToArray();
        }

        public void SetColumns()
        {
            //m_ColumnsList.Initialize();
            //m_ColumnsList.Objects = (from column in m_Object.Columns select (column.Data + " " + column.Protocol + " " + column.Bloc)).ToArray();
        }
        #endregion
    }
}