using System.Collections.Generic;
using Tools.Unity.Components;
using UnityEngine;

namespace HBP.UI.Experience.Protocol
{
    public class TreatmentListGestion : ListGestion<Data.Experience.Protocol.Treatment>
    {
        #region Properties
        [SerializeField] new TreatmentList List;
        public override List<Data.Experience.Protocol.Treatment> Items
        {
            get
            {
                return base.Items;
            }

            set
            {
                List.Initialize();
                base.Items = value;
                //List.SortByName(TreatmentList.Sorting.Descending);
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