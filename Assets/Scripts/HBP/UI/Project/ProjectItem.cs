using UnityEngine;
using HBP.Data.General;
using UnityEngine.UI;

namespace HBP.UI
{
	/// <summary>
	/// Manage the project item.
	/// </summary>
	public class ProjectItem : Tools.Unity.Lists.ActionnableItem<ProjectInfo> 
	{
        #region Properties
        /// <summary>
        /// The name textField.
        /// </summary>
        [SerializeField] Text m_NameText;
        /// <summary>
        /// The number of patients textField.
        /// </summary>
        [SerializeField] Text m_PatientsText;
        /// <summary>
        /// The number of groups textField.
        /// </summary>
        [SerializeField] Text m_GroupsText;
        /// <summary>
        /// The number of protocols textField.
        /// </summary>
        [SerializeField] Text m_ProtocolsText;
        /// <summary>
        /// The number of experiences textField.
        /// </summary>
        [SerializeField] Text m_DatasetsText;
        /// <summary>
        /// The number of visualizations textField.
        /// </summary>
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
                m_GroupsText.text = value.Groups.ToString();
                m_ProtocolsText.text = value.Protocols.ToString();
                m_DatasetsText.text = value.Datasets.ToString();
                m_VisualizationsText.text = value.Visualizations.ToString();
            }
        }
        #endregion
    }
}