using HBP.Data;
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
		[SerializeField] Color m_Enable_color;
        [SerializeField] Color m_Disable_color;
        public override Patient Object
        {
            get
            {
                return base.Object;
            }

            set
            {
                base.Object = value;
                m_NameText.text = value.Name;
                m_PlaceText.text = value.Place;
                m_DateText.text = value.Date.ToString();

                int nbMesh = value.Brain.Meshes.FindAll((m) => m.isUsable).Count;
                m_MeshText.text = nbMesh.ToString();
                if (nbMesh == 0) m_MeshText.color = m_Disable_color;
                else m_MeshText.color = m_Enable_color;

                int nbMRI = value.Brain.MRIs.Count;
                m_MRIText.text = nbMRI.ToString();
                if (nbMRI == 0) m_MRIText.color = m_Disable_color;
                else m_MRIText.color = m_Enable_color;

                int nbImplantation = value.Brain.Implantations.Count;
                m_ImplantationText.text = nbImplantation.ToString();
                if (nbImplantation == 0) m_ImplantationText.color = m_Disable_color;
                else m_ImplantationText.color = m_Enable_color;

                int nbTransformation = value.Brain.Transformations.Count;
                m_TransformationText.text = nbTransformation.ToString();
                if (nbTransformation == 0) m_TransformationText.color = m_Disable_color;
                else m_TransformationText.color = m_Enable_color;

                int nbConnectivity = value.Brain.Connectivities.Count;
                m_ConnectivityText.text = nbConnectivity.ToString();
                if (nbConnectivity == 0) m_ConnectivityText.color = m_Disable_color;
                else m_ConnectivityText.color = m_Enable_color;
            }
        }
        #endregion
	}
}