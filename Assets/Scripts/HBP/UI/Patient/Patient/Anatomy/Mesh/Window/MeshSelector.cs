using HBP.UI.Anatomy;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    public class MeshSelector : ObjectSelector<Data.Anatomy.Mesh>
    {
        #region Properties
        [SerializeField] MeshList m_List;
        protected override SelectableList<Data.Anatomy.Mesh> List => throw new System.NotImplementedException();
        #endregion
    }
}