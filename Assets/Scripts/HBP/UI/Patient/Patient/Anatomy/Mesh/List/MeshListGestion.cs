using System.Collections.Generic;
using Tools.Unity.Components;
using UnityEngine;

namespace HBP.UI.Anatomy
{
    public class MeshListGestion : ListGestion<Data.Anatomy.Mesh>
    {
        #region Properties
        [SerializeField] new MeshList List;
        public override List<Data.Anatomy.Mesh> Items
        {
            get
            {
                return base.Items;
            }

            set
            {
                List.Initialize();
                base.Items = value;
                List.SortByName(MeshList.Sorting.Descending);
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.List = List;
            base.Initialize();
        }
        #endregion
    }
}
