using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    public class MeshSelector : ObjectSelector<Data.BaseMesh>
    {
        #region Properties
        [SerializeField] MeshList m_List;
        protected override SelectableList<Data.BaseMesh> List => m_List;
        #endregion
    }
}