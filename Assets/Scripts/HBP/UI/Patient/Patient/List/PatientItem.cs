using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Anatomy
{
	public class PatientItem : Tools.Unity.Lists.ActionnableItem<Data.Patient>
	{
		#region Properties
		[SerializeField] Text m_NameText;		
		[SerializeField] Text m_PlaceText;
		[SerializeField] Text m_DateText;
		[SerializeField] Text m_MeshText;
		[SerializeField] Text m_MRIText;
		[SerializeField] Text m_TransformationText;
		[SerializeField] Text m_ImplantationText;
        [SerializeField] Text m_ConnectivityText;
		[SerializeField] Color m_enable_color;
        [SerializeField] Color m_disable_color;
        #endregion

        #region Private Methods
        /// <summary>
        /// Sets the text field of the patient panel.
        /// </summary>
        protected override void SetObject(Data.Patient objectToSet)
        {
            m_NameText.text = objectToSet.Name;
            m_PlaceText.text = objectToSet.Place;
            m_DateText.text = objectToSet.Date.ToString();

            int nbMesh = objectToSet.Brain.Meshes.FindAll((m) => m.isUsable).Count;
            m_MeshText.text = nbMesh.ToString();
            if (nbMesh == 0) m_MeshText.color = m_disable_color;
            else m_MeshText.color = m_enable_color;

            int nbMRI = objectToSet.Brain.MRIs.Count;
            m_MRIText.text = nbMRI.ToString();
            if (nbMRI == 0) m_MRIText.color = m_disable_color;
            else m_MRIText.color = m_enable_color;

            int nbImplantation = objectToSet.Brain.Implantations.Count;
            m_ImplantationText.text = nbImplantation.ToString();
            if (nbImplantation == 0) m_ImplantationText.color = m_disable_color;
            else m_ImplantationText.color = m_enable_color;

            int nbTransformation = objectToSet.Brain.Transformations.Count;
            m_TransformationText.text = nbTransformation.ToString();
            if (nbTransformation == 0) m_TransformationText.color = m_disable_color;
            else m_TransformationText.color = m_enable_color;

            int nbConnectivity = objectToSet.Brain.Connectivities.Count;
            m_ConnectivityText.text = nbConnectivity.ToString();
            if (nbConnectivity == 0) m_ConnectivityText.color = m_disable_color;
            else m_ConnectivityText.color = m_enable_color;
        }
		#endregion
	}
}