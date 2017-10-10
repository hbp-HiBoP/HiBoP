using UnityEngine;
using HBP.Data.General;
using UnityEngine.UI;
using HBP.UI.Anatomy;

namespace HBP.UI
{
	/// <summary>
	/// Manage the project item.
	/// </summary>
	public class ProjectItem : Tools.Unity.Lists.ActionnableItem<ProjectInfo> 
	{
        #region Properties
        [SerializeField] Text m_NameText;

        [SerializeField] Text m_PatientsText;
        [SerializeField] Text m_GroupsText;
        [SerializeField] Text m_ProtocolsText;
        [SerializeField] Text m_DatasetsText;
        [SerializeField] Text m_VisualizationsText;

        public override ProjectInfo Object
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
                if (value.Patients == 0) m_PatientsText.color = ApplicationState.GeneralSettings.Theme.General.Error;
                else m_PatientsText.color = ApplicationState.GeneralSettings.Theme.Window.Content.Text.Color;

                m_GroupsText.text = value.Groups.ToString();
                if (value.Groups == 0) m_GroupsText.color = ApplicationState.GeneralSettings.Theme.General.Error;
                else m_GroupsText.color = ApplicationState.GeneralSettings.Theme.Window.Content.Text.Color;

                m_ProtocolsText.text = value.Protocols.ToString();
                if (value.Protocols == 0) m_ProtocolsText.color = ApplicationState.GeneralSettings.Theme.General.Error;
                else m_ProtocolsText.color = ApplicationState.GeneralSettings.Theme.Window.Content.Text.Color;

                m_DatasetsText.text = value.Datasets.ToString();
                if (value.Datasets == 0) m_DatasetsText.color = ApplicationState.GeneralSettings.Theme.General.Error;
                else m_DatasetsText.color = ApplicationState.GeneralSettings.Theme.Window.Content.Text.Color;

                m_VisualizationsText.text = value.Visualizations.ToString();
                if (value.Visualizations == 0) m_VisualizationsText.color = ApplicationState.GeneralSettings.Theme.General.Error;
                else m_VisualizationsText.color = ApplicationState.GeneralSettings.Theme.Window.Content.Text.Color;
            }
        }
        #endregion
    }
}