using d = HBP.Data.Anatomy;
using System.Collections.Generic;
using Tools.Unity.Components;
using UnityEngine;

namespace HBP.UI.Anatomy
{
    public class MeshListGestion : ListGestion<d.Mesh>
    {
        #region Properties
        [SerializeField] new MeshList List;
        public override List<d.Mesh> Objects
        {
            get
            {
                return base.Objects;
            }

            set
            {
                List.Initialize();
                base.Objects = value;
                List.SortByName(MeshList.Sorting.Descending);
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            List.OnUpdateObject.AddListener((mesh, index) => m_Objects[index] = mesh);
            base.List = List;
            base.Initialize();
        }
        protected override void OnSaveCreator(CreatorWindow creatorWindow)
        {
            Data.Enums.CreationType type = creatorWindow.Type;
            d.Mesh item = new d.LeftRightMesh();
            switch (type)
            {
                case Data.Enums.CreationType.FromScratch:
                    OpenModifier(item, true);
                    break;
                case Data.Enums.CreationType.FromExistingItem:
                    OpenSelector();
                    break;
                case Data.Enums.CreationType.FromFile:
                    if (LoadFromFile(out item))
                    {
                        OpenModifier(item, true);
                    }
                    break;
            }
        }
        #endregion
    }
}
