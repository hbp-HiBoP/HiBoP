using HBP.UI.Anatomy;
using UnityEngine;

namespace HBP.UI
{
    public class MeshSelector : ObjectSelector<Data.Anatomy.Mesh>
    {
        #region Properties
        [SerializeField] MeshList m_MeshList;
        #endregion

        #region Public Methods
        protected override void Initialize()
        {
            m_List = m_MeshList;
            base.Initialize();
        }
        #endregion
    }
}