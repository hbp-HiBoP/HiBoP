using HBP.Core.Data;
using HBP.Data.Preferences;
using HBP.UI.Main;
using HBP.UI.Tools;
using System.Linq;
using UnityEngine;

namespace HBP.UI.Database
{
    public class AnatomicalDataExplorer : MonoBehaviour
    {
        #region Properties
        [SerializeField] private MeshListGestion m_MeshListGestion;
        [SerializeField] private MRIListGestion m_MRIListGestion;
        [SerializeField] private SiteListGestion m_SiteListGestion;
        [SerializeField] private TagValueListGestion m_TagValueListGestion;
        #endregion

        #region Public Methods
        public void Initialize(WindowsReferencer windowsReferencer)
        {
            m_MeshListGestion.WindowsReferencer.OnOpenWindow.AddListener(windowsReferencer.Add);
            m_MRIListGestion.WindowsReferencer.OnOpenWindow.AddListener(windowsReferencer.Add);
            m_SiteListGestion.WindowsReferencer.OnOpenWindow.AddListener(windowsReferencer.Add);
            m_TagValueListGestion.WindowsReferencer.OnOpenWindow.AddListener(windowsReferencer.Add);
        }
        public void SetFields()
        {

        }
        public void Set(Patient patient)
        {
            m_MeshListGestion.List.Set(patient.Meshes);
            m_MRIListGestion.List.Set(patient.MRIs);
            m_SiteListGestion.List.Set(patient.Sites);
            m_TagValueListGestion.Tags = PersistentDataManager.Tags.PatientsTags.Concat(PersistentDataManager.Tags.GeneralTags).ToArray();
            m_TagValueListGestion.List.Set(patient.Tags);
        }
        #endregion
    }
}