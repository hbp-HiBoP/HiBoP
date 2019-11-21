using Tools.Unity.Components;
using UnityEngine;
using Tools.Unity.Lists;

namespace HBP.UI
{
    public class MeshListGestion : ListGestion<Data.BaseMesh>
    {
        #region Properties
        [SerializeField] protected MeshList m_List;
        public override ActionableList<Data.BaseMesh> List => m_List;

        [SerializeField] protected MeshCreator m_ObjectCreator;
        public override ObjectCreator<Data.BaseMesh> ObjectCreator => m_ObjectCreator;
        #endregion
    }
}
