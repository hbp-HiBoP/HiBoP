using UnityEngine;
using UnityEngine.UI;
using NewTheme.Components;

namespace HBP.UI
{
	/// <summary>
	/// Manage the project item.
	/// </summary>
	public class ProjectItem : Tools.Unity.Lists.ActionnableItem<Data.ProjectInfo> 
	{
        #region Properties
        [SerializeField] Text m_NameText;

        [SerializeField] Text m_PatientsText;
        [SerializeField] Text m_GroupsText;
        [SerializeField] Text m_ProtocolsText;
        [SerializeField] Text m_DatasetsText;
        [SerializeField] Text m_VisualizationsText;

        [SerializeField] State Error;

        public override Data.ProjectInfo Object
        {
            get
            {
                return base.Object;
            }

            set
            {
                base.Object = value;

                m_NameText.text = value.Settings.Name;

                m_PatientsText.text = value.Patients.ToString();
                if (value.Patients == 0) m_PatientsText.GetComponent<ThemeElement>().Set(Error);
                else m_PatientsText.GetComponent<ThemeElement>().Set();

                m_GroupsText.text = value.Groups.ToString();
                if (value.Groups == 0) m_GroupsText.GetComponent<ThemeElement>().Set(Error);
                else m_GroupsText.GetComponent<ThemeElement>().Set();

                m_ProtocolsText.text = value.Protocols.ToString();
                if (value.Protocols == 0) m_ProtocolsText.GetComponent<ThemeElement>().Set(Error);
                else m_ProtocolsText.GetComponent<ThemeElement>().Set();

                m_DatasetsText.text = value.Datasets.ToString();
                if (value.Datasets == 0) m_DatasetsText.GetComponent<ThemeElement>().Set(Error);
                else m_DatasetsText.GetComponent<ThemeElement>().Set();

                m_VisualizationsText.text = value.Visualizations.ToString();
                if (value.Visualizations == 0) m_VisualizationsText.GetComponent<ThemeElement>().Set(Error);
                else m_VisualizationsText.GetComponent<ThemeElement>().Set();
            }
        }
        #endregion
    }
}