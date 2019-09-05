using System.Collections.Generic;
using Tools.Unity.Components;
using UnityEngine;

namespace HBP.UI.Anatomy
{
    public class ImplantationListGestion : ListGestion<Data.Anatomy.Implantation>
    {
        #region Properties
        [SerializeField] new ImplantationList List;
        public override List<Data.Anatomy.Implantation> Objects
        {
            get
            {
                return base.Objects;
            }

            set
            {
                List.Initialize();
                base.Objects = value;
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