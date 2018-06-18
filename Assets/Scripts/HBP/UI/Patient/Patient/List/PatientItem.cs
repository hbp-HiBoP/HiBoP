using HBP.Data;
using UnityEngine;
using UnityEngine.UI;
using Tools.Unity.Lists;
using System.Linq;
using NewTheme.Components;
using System.Collections.Generic;

namespace HBP.UI.Anatomy
{
	public class PatientItem : ActionnableItem<Data.Patient>
	{
		#region Properties
		[SerializeField] Text m_NameText;		
		[SerializeField] Text m_PlaceText;
		[SerializeField] Text m_DateText;

		[SerializeField] Text m_MeshText;
        [SerializeField] LabelList m_MeshList;
		[SerializeField] Text m_MRIText;
        [SerializeField] LabelList m_MRIList;
        [SerializeField] Text m_ImplantationText;
        [SerializeField] LabelList m_ImplantationList;
        [SerializeField] Text m_ConnectivityText;
        [SerializeField] LabelList m_ConnectivityList;

        [SerializeField] State m_ErrorState;

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
                if (nbMesh == 0) m_MeshText.GetComponent<ThemeElement>().Set(m_ErrorState);
                else m_MeshText.GetComponent<ThemeElement>().Set();

                int nbMRI = value.Brain.MRIs.Count((m) => m.WasUsable);
                m_MRIText.text = nbMRI.ToString();
                if (nbMRI == 0) m_MRIText.GetComponent<ThemeElement>().Set(m_ErrorState);
                else m_MRIText.GetComponent<ThemeElement>().Set();

                int nbImplantation = value.Brain.Implantations.Count((i) => i.WasUsable);
                m_ImplantationText.text = nbImplantation.ToString();
                if (nbImplantation == 0) m_ImplantationText.GetComponent<ThemeElement>().Set(m_ErrorState);
                else m_ImplantationText.GetComponent<ThemeElement>().Set();
                
                int nbConnectivity = value.Brain.Connectivities.Count(c => c.WasUsable);
                m_ConnectivityText.text = nbConnectivity.ToString();
                if (nbConnectivity == 0) m_ConnectivityText.GetComponent<ThemeElement>().Set(m_ErrorState);
                else m_ConnectivityText.GetComponent<ThemeElement>().Set();
            }
        }
        #endregion


        #region Public Methods
        public void SetMeshes()
        {
            m_MeshList.Initialize();
            IEnumerable<string> labels = from mesh in m_Object.Brain.Meshes where mesh.WasUsable select mesh.Name;
            if (labels.Count() == 0) labels = new string[] { "No Mesh" };
            m_MeshList.Objects = labels.ToArray();
        }
        public void SetMRIs()
        {
            m_MRIList.Initialize();
            IEnumerable<string> labels = from mri in m_Object.Brain.MRIs where mri.WasUsable select mri.Name;
            if (labels.Count() == 0) labels = new string[] { "No MRI" };
            m_MRIList.Objects = labels.ToArray();
        }
        public void SetImplantations()
        {
            m_ImplantationList.Initialize();
            IEnumerable<string> labels = from implantation in m_Object.Brain.Implantations where implantation.WasUsable select implantation.Name;
            if (labels.Count() == 0) labels = new string[] { "No Implantation" };
            m_ImplantationList.Objects = labels.ToArray();
        }
        public void SetConnectivities()
        {
            m_ConnectivityList.Initialize();
            IEnumerable<string> labels = from connectivity in m_Object.Brain.Connectivities where connectivity.WasUsable select connectivity.Name;
            if (labels.Count() == 0) labels = new string[] { "No Connectivity" };
            m_ConnectivityList.Objects = labels.ToArray();
        }
        #endregion
    }
}