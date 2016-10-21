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
		Text m_name;

        /// <summary>
        /// The number of patients textField.
        /// </summary>
        [SerializeField]
        Text m_patients;

        /// <summary>
        /// The number of protocols textField.
        /// </summary>
        [SerializeField]
        Text m_protocols;

        /// <summary>
        /// The number of groups textField.
        /// </summary>
        [SerializeField]
        Text m_groups;

        /// <summary>
        /// The number of experiences textField.
        /// </summary>
        [SerializeField]
        Text m_experiences;

        /// <summary>
        /// The number of visualisations textField.
        /// </summary>
        [SerializeField]
        Text m_visualisations;
        #endregion

        #region Protected Methods
        protected override void SetObject(ProjectInfo objectToSet)
        {
            m_object = objectToSet;
            m_name.text = m_object.Name;
            m_patients.text = m_object.Patients.ToString();
            m_groups.text = m_object.Groups.ToString();
            m_protocols.text = m_object.Protocols.ToString();
            m_experiences.text = m_object.Datasets.ToString();
            m_visualisations.text = m_object.Visualisations.ToString();
        }
        #endregion
    }
}