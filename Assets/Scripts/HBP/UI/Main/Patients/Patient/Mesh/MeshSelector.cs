using HBP.UI.Tools.Lists;
using UnityEngine;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    /// <summary>
    /// Window to select meshes.
    /// </summary>
    public class MeshSelector : ObjectSelector<Core.Data.BaseMesh>
    {
        #region Properties
        [SerializeField] MeshList m_List;
        /// <summary>
        /// UI meshes list.
        /// </summary>
        protected override SelectableList<Core.Data.BaseMesh> List => m_List;
        #endregion
    }
}