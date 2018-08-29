using System.Collections.Generic;
using Tools.Unity.Components;
using UnityEngine;

namespace HBP.UI.Anatomy
{
    public class ImplantationListGestion : ListGestion<Data.Anatomy.Implantation>
    {
        #region Properties
        [SerializeField] new ImplantationList List;
        public override List<Data.Anatomy.Implantation> Items
        {
            get
            {
                return base.Items;
            }

            set
            {
                List.Initialize();
                base.Items = value;
                List.SortByName(ImplantationList.Sorting.Descending);
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