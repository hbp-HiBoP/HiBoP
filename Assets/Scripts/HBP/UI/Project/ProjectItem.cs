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
        #region UI
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
        #endregion

        #region Protected Methods
        protected override void SetObject(ProjectInfo objectToSet)
        {
            m_Object = objectToSet;
            m_NameText.text = m_Object.Settings.Name;
            m_PatientsText.text = m_Object.Patients.ToString();
            m_GroupsText.text = m_Object.Groups.ToString();
            m_ProtocolsText.text = m_Object.Protocols.ToString();
            m_DatasetsText.text = m_Object.Datasets.ToString();
            m_VisualizationsText.text = m_Object.Visualizations.ToString();
        }
        #endregion
    }
}