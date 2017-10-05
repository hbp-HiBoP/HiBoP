using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Tools.Unity.Lists;

namespace HBP.UI.Visualization
{
    public class VisualizationItem : ActionnableItem<Data.Visualization.Visualization>
    {
        #region Properties
        [SerializeField] Text m_NameText;
        [SerializeField] Text m_PatientsText;
        [SerializeField] Button m_PatientsButton;
        [SerializeField] LabelList m_PatientsList;
        [SerializeField] Text m_ColumnsText;
        [SerializeField] Button m_ColumnsButton;
        [SerializeField] LabelList m_ColumnsList;

        public override Data.Visualization.Visualization Object
        {
            get
            {
                return base.Object;
            }

            set
            {
                base.Object = value;

                // Name.
                m_NameText.text = value.Name;

                // Patients.
                int nbPatients = value.Patients.Count;
                m_PatientsText.text = nbPatients.ToString();
                if (nbPatients == 0)
                {
                    m_PatientsText.color = ApplicationState.Theme.General.Error;
                    m_PatientsButton.interactable = false;
                }
                else
                {
                    m_PatientsText.color = ApplicationState.Theme.Window.Content.Text.Color;
                    m_PatientsButton.interactable = true;
                }

                // Columns.
                int nbColumns = value.Columns.Count;
                m_ColumnsText.text = nbColumns.ToString();
                if (nbColumns == 0)
                {
                    m_ColumnsText.color = ApplicationState.Theme.General.Error;
                    m_ColumnsButton.interactable = false;
                }
                else
                {
                    m_ColumnsText.color = ApplicationState.Theme.Window.Content.Text.Color;
                    m_ColumnsButton.interactable = true;
                }
            }
        }
        #endregion

        #region Public Methods
        public void SetPatients()
        {
            m_PatientsList.Objects = (from patient in m_Object.Patients select patient.Name).ToArray();
        }

        public void SetColumns()
        {
            m_ColumnsList.Objects = (from column in m_Object.Columns select (column.Data + " " + column.Protocol + " " + column.Bloc)).ToArray();
        }
        #endregion
    }
}