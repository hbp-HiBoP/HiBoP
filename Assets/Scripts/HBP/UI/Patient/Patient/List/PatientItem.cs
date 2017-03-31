using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Patient
{
	public class PatientItem : Tools.Unity.Lists.ListItemWithActions<Data.Patient>
	{
		#region Attributs
		#region UI Elements
		/// <summary>
		/// The name.
		/// </summary>
		[SerializeField]
		Text m_name;
		
		/// <summary>
		/// The place.
		/// </summary>
		[SerializeField]	
		Text m_place;
		
		/// <summary>
		/// The date.
		/// </summary>
		[SerializeField]	
		Text m_date;
		
		/// <summary>
		/// The mesh state.
		/// </summary>
		[SerializeField]	
		Text m_mesh_state;
		
		/// <summary>
		/// The IRM pre state.
		/// </summary>
		[SerializeField]	
		Text m_IRM_state;
		
		/// <summary>
		/// The IRM post state.
		/// </summary>
		[SerializeField]	
		Text m_transformBase_state;
		
		/// <summary>
		/// The Implantation state
		/// </summary>
		[SerializeField]	
		Text m_implantation_state;
		
		/// <summary>
		/// The color if enable
		/// </summary>
		[SerializeField]
		Color m_enable_color;
		
		/// <summary>
		/// The color if disable
		/// </summary>
		[SerializeField]
		Color m_disable_color;
        #endregion
        #endregion

        #region Private Methods
        /// <summary>
        /// Sets the text field of the patient panel.
        /// </summary>
        protected override void SetObject(Data.Patient objectToSet)
        {
            m_name.text = objectToSet.Name;
            m_place.text = objectToSet.Place;
            m_date.text = objectToSet.Date.ToString();

            if (objectToSet.Brain.LeftCerebralHemisphereMesh != "" && objectToSet.Brain.RightCerebralHemisphereMesh != "") { m_mesh_state.color = m_enable_color; }
            else { m_mesh_state.color = m_disable_color; }

            if (objectToSet.Brain.PreOperationMRI != "" && objectToSet.Brain.PostOperationMRI != "") { m_IRM_state.color = m_enable_color; }
            else { m_IRM_state.color = m_disable_color; }

            if (objectToSet.Brain.PreOperationReferenceFrameToScannerReferenceFrameTransformation != "") { m_transformBase_state.color = m_enable_color; }
            else { m_transformBase_state.color = m_disable_color; }

            if (objectToSet.Brain.PatientReferenceFrameImplantation != "") { m_implantation_state.color = m_enable_color; }
            else { m_implantation_state.color = m_disable_color; }
        }
		#endregion
	}
}