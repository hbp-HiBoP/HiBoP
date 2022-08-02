using UnityEngine;
using Tools.Unity.Lists;

namespace HBP.UI
{
    public class MeshListGestion : ListGestion<Core.Data.BaseMesh>
    {
        #region Properties
        [SerializeField] protected MeshList m_List;
        public override ActionableList<Core.Data.BaseMesh> List => m_List;

        [SerializeField] protected MeshCreator m_ObjectCreator;
        public override ObjectCreator<Core.Data.BaseMesh> ObjectCreator => m_ObjectCreator;
        #endregion
    }
}
