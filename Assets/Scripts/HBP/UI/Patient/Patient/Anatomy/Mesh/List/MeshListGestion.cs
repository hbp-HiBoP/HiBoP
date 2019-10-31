using d = HBP.Data.Anatomy;
using Tools.Unity.Components;
using UnityEngine;
using Tools.Unity.Lists;

namespace HBP.UI.Anatomy
{
    public class MeshListGestion : ListGestion<d.Mesh>
    {
        #region Properties
        [SerializeField] protected MeshList m_List;
        public override SelectableListWithItemAction<d.Mesh> List => m_List;

        [SerializeField] protected MeshCreator m_ObjectCreator;
        public override ObjectCreator<d.Mesh> ObjectCreator => m_ObjectCreator;
        #endregion
    }
}
