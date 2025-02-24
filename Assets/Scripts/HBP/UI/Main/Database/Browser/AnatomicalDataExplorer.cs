using HBP.Core.Data;
using HBP.UI.Main;
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
        public void Set(Patient patient)
        {
            m_MeshListGestion.List.Set(patient.Meshes);
            m_MRIListGestion.List.Set(patient.MRIs);
            m_SiteListGestion.List.Set(patient.Sites);
            m_TagValueListGestion.List.Set(patient.Tags);
        }
        #endregion
    }
}