using HBP.Data;
using UnityEngine;
using UnityEngine.UI;
using Tools.Unity.Lists;
using System.Linq;
using NewTheme.Components;

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

        [SerializeField] State Error;

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
                
                int nbMesh = value.Brain.Meshes.Count((m) => m.WasUsable);
                m_MeshText.text = nbMesh.ToString();
                if (nbMesh == 0)
                {
                    m_MeshText.GetComponent<ThemeElement>().Set(Error);
                    m_MeshButton.interactable = false;
                }
                else
                {
                    m_MeshText.GetComponent<ThemeElement>().Set();
                    m_MeshButton.interactable = true;
                }

                int nbMRI = value.Brain.MRIs.Count((m) => m.WasUsable);
                m_MRIText.text = nbMRI.ToString();
                if (nbMRI == 0)
                {
                    m_MRIText.GetComponent<ThemeElement>().Set(Error);
                    m_MRIButton.interactable = false;
                }
                else
                {
                    m_MRIText.GetComponent<ThemeElement>().Set();
                    m_MRIButton.interactable = true;
                }

                int nbImplantation = value.Brain.Implantations.Count((i) => i.WasUsable);
                m_ImplantationText.text = nbImplantation.ToString();
                if (nbImplantation == 0)
                {
                    m_ImplantationText.GetComponent<ThemeElement>().Set(Error);
                    m_ImplantationButton.interactable = false;
                }
                else
                {
                    m_ImplantationText.GetComponent<ThemeElement>().Set();
                    m_ImplantationButton.interactable = true;
                }
                
                int nbConnectivity = value.Brain.Connectivities.Count(c => c.WasUsable);
                m_ConnectivityText.text = nbConnectivity.ToString();
                if (nbConnectivity == 0)
                {
                    m_ConnectivityText.GetComponent<ThemeElement>().Set(Error);
                    m_ConnectivityButton.interactable = false;
                }
                else
                {
                    m_ConnectivityText.GetComponent<ThemeElement>().Set();
                    m_ConnectivityButton.interactable = true;
                }
            }
        }
        #endregion


        #region Public Methods
        public void SetMeshes()
        {
            m_MeshList.Initialize();
            m_MeshList.Objects = (from mesh in m_Object.Brain.Meshes where mesh.WasUsable select mesh.Name).ToArray();
        }
        public void SetMRIs()
        {
            m_MRIList.Initialize();
            m_MRIList.Objects = (from mri in m_Object.Brain.MRIs where mri.WasUsable select mri.Name).ToArray();
        }
        public void SetImplantations()
        {
            m_ImplantationList.Initialize();
            m_ImplantationList.Objects = (from implantation in m_Object.Brain.Implantations where implantation.WasUsable select implantation.Name).ToArray();
        }
        public void SetConnectivities()
        {
            m_ConnectivityList.Initialize();
            m_ConnectivityList.Objects = (from connectivity in m_Object.Brain.Connectivities where connectivity.WasUsable select connectivity.Name).ToArray();
        }
        #endregion
    }
}