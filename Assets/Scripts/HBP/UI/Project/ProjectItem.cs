using UnityEngine;
using HBP.Data.General;
using UnityEngine.UI;

namespace HBP.UI
{
	/// <summary>
	/// Manage the project item.
	/// </summary>
	public class ProjectItem : Tools.Unity.Lists.ListItemWithActions<ProjectInfo> 
	{
        #region UI
        /// <summary>
        /// The name textField.
        /// </summary>
        [SerializeField]
		Text m_Name;

        /// <summary>
        /// The number of patients textField.
        /// </summary>
        [SerializeField]
        Text m_Patients;

        /// <summary>
        /// The number of protocols textField.
        /// </summary>
        [SerializeField]
        Text m_Protocols;

        /// <summary>
        /// The number of groups textField.
        /// </summary>
        [SerializeField]
        Text m_Groups;

        /// <summary>
        /// The number of experiences textField.
        /// </summary>
        [SerializeField]
        Text m_Datasets;

        /// <summary>
        /// The number of visualizations textField.
        /// </summary>
        [SerializeField]
        Text m_Visualizations;
        #endregion

        #region Protected Methods
        protected override void SetObject(ProjectInfo objectToSet)
        {
            m_object = objectToSet;
            m_Name.text = m_object.Settings.Name;
            m_Patients.text = m_object.Patients.ToString();
            m_Groups.text = m_object.Groups.ToString();
            m_Protocols.text = m_object.Protocols.ToString();
            m_Datasets.text = m_object.Datasets.ToString();
            m_Visualizations.text = m_object.Visualizations.ToString();
        }
        #endregion
    }
}