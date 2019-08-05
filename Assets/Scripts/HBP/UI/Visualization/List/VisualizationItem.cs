using UnityEngine;
using UnityEngine.UI;
using Tools.Unity.Lists;
using NewTheme.Components;
using Tools.Unity;
using System.Text;
using System.Linq;

namespace HBP.UI.Visualization
{
    public class VisualizationItem : ActionnableItem<Data.Visualization.Visualization>
    {
        #region Properties
        [SerializeField] Text m_NameText;

        [SerializeField] Text m_PatientsText;
        [SerializeField] Tooltip m_PatientsTooltip;

        [SerializeField] Text m_ColumnsText;
        [SerializeField] Tooltip m_ColumnsTooltip;

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

                // Name.
                m_NameText.text = value.Name;

                // Patients.
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("Patients: ");
                string[] patients = value.Patients.Select(b => b.Name).ToArray();
                for (int i = 0; i < patients.Length; i++)
                {
                    if (i < patients.Length - 1) stringBuilder.AppendLine("  \u2022 " + patients[i]);
                    else stringBuilder.Append("  \u2022 " + patients[i]);
                }
                if (patients.Length == 0)
                {
                    m_PatientsText.GetComponent<ThemeElement>().Set(m_ErrorState);
                    stringBuilder.Append("  \u2022 None");
                }
                else
                {
                    m_PatientsText.GetComponent<ThemeElement>().Set();
                }
                m_PatientsTooltip.Text = stringBuilder.ToString();
                m_PatientsText.text = patients.Length.ToString();


                // Columns.
                stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("Columns: ");
                string[] columns = value.Columns.Select(b => b.Name).ToArray();
                for (int i = 0; i < columns.Length; i++)
                {
                    if (i < columns.Length - 1) stringBuilder.AppendLine("  \u2022 " + columns[i]);
                    else stringBuilder.Append("  \u2022 " + columns[i]);
                }
                if (columns.Length == 0)
                {
                    m_ColumnsText.GetComponent<ThemeElement>().Set(m_ErrorState);
                    stringBuilder.Append("  \u2022 None");
                }
                else
                {
                    m_PatientsText.GetComponent<ThemeElement>().Set();
                }
                m_ColumnsTooltip.Text = stringBuilder.ToString();
                m_ColumnsText.text = columns.Length.ToString();
            }
        }
        #endregion
    }
}