using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    /// <summary>
    /// Window to select meshes.
    /// </summary>
    public class MeshSelector : ObjectSelector<Data.BaseMesh>
    {
        #region Properties
        [SerializeField] MeshList m_List;
        /// <summary>
        /// UI meshes list.
        /// </summary>
        protected override SelectableList<Data.BaseMesh> List => m_List;
        #endregion
    }
}