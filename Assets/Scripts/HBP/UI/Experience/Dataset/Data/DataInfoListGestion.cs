using System.Collections.Generic;
using Tools.Unity.Components;
using UnityEngine;

namespace HBP.UI.Experience.Dataset
{
    public class DataInfoListGestion : ListGestion<Data.Experience.Dataset.DataInfo>
    {
        #region Properties
        [SerializeField] new DataInfoList List;
        public override List<Data.Experience.Dataset.DataInfo> Items
        {
            get
            {
                return base.Items;
            }

            set
            {
                List.Initialize();
                base.Items = value;
                List.SortByName(DataInfoList.Sorting.Descending);
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