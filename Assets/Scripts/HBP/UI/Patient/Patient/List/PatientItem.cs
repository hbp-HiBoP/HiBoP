using HBP.Data;
using UnityEngine;
using UnityEngine.UI;
using Tools.Unity.Lists;
using System.Linq;

namespace HBP.UI.Anatomy
{
	public class PatientItem : ActionnableItem<Data.Patient>
	{
		#region Properties
		[SerializeField] Text m_NameText;		
		[SerializeField] Text m_PlaceText;
		[SerializeField] Text m_DateText;

		[SerializeField] Text m_MeshText;
        [SerializeField] Button m_MeshButton;
        [SerializeField] LabelList m_MeshList;

		[SerializeField] Text m_MRIText;
        [SerializeField] Button m_MRIButton;
        [SerializeField] LabelList m_MRIList;

        [SerializeField] Text m_ImplantationText;
        [SerializeField] Button m_ImplantationButton;
        [SerializeField] LabelList m_ImplantationList;

        [SerializeField] Text m_ConnectivityText;
        [SerializeField] Button m_ConnectivityButton;
        [SerializeField] LabelList m_ConnectivityList;

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
                if (nbMesh == 0)
                {
                    m_MeshText.color = ApplicationState.Theme.Color.DisableLabel;
                    m_MeshButton.interactable = false;
                }
                else
                {
                    m_MeshText.color = ApplicationState.Theme.Color.ContentNormalLabel;
                    m_MeshButton.interactable = true;
                }

                int nbMRI = value.Brain.MRIs.FindAll((m) => m.isUsable).Count;
                m_MRIText.text = nbMRI.ToString();
                if (nbMRI == 0)
                {
                    m_MRIText.color = ApplicationState.Theme.Color.DisableLabel;
                    m_MRIButton.interactable = false;
                }
                else
                {
                    m_MRIText.color = ApplicationState.Theme.Color.ContentNormalLabel;
                    m_MRIButton.interactable = true;
                }

                int nbImplantation = value.Brain.Implantations.FindAll((i) => i.isUsable).Count;
                m_ImplantationText.text = nbImplantation.ToString();
                if (nbImplantation == 0)
                {
                    m_ImplantationText.color = ApplicationState.Theme.Color.DisableLabel;
                    m_ImplantationButton.interactable = false;
                }
                else
                {
                    m_ImplantationText.color = ApplicationState.Theme.Color.ContentNormalLabel;
                    m_ImplantationButton.interactable = true;
                }

                int nbConnectivity = value.Brain.Connectivities.FindAll(c => c.isUsable).Count;
                m_ConnectivityText.text = nbConnectivity.ToString();
                if (nbConnectivity == 0)
                {
                    m_ConnectivityText.color = ApplicationState.Theme.Color.DisableLabel;
                    m_ConnectivityButton.interactable = false;
                }
                else
                {
                    m_ConnectivityText.color = ApplicationState.Theme.Color.ContentNormalLabel;
                    m_ConnectivityButton.interactable = true;
                }
            }
        }
        #endregion


        #region Public Methods
        public void SetMeshes()
        {
            m_MeshList.Objects = (from mesh in m_Object.Brain.Meshes where mesh.isUsable select mesh.Name).ToArray();
        }
        public void SetMRIs()
        {
            m_MRIList.Objects = (from mri in m_Object.Brain.MRIs where mri.isUsable select mri.Name).ToArray();
        }
        public void SetImplantations()
        {
            m_ImplantationList.Objects = (from implantation in m_Object.Brain.Implantations where implantation.isUsable select implantation.Name).ToArray();
        }
        public void SetConnectivities()
        {
            m_ConnectivityList.Objects = (from connectivity in m_Object.Brain.Connectivities where connectivity.isUsable select connectivity.Name).ToArray();
        }
        #endregion
    }
}