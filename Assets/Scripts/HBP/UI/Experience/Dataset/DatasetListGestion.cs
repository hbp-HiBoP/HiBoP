using System.Collections.Generic;
using Tools.Unity.Components;
using UnityEngine;

namespace HBP.UI.Experience.Dataset
{
    public class DatasetListGestion : ListGestion<Data.Experience.Dataset.Dataset>
    {
        #region Properties
        [SerializeField] new DatasetList List;
        public override List<Data.Experience.Dataset.Dataset> Objects
        {
            get
            {
                return base.Objects;
            }

            set
            {
                List.Initialize();
                base.Objects = value;
                List.SortByName(DatasetList.Sorting.Descending);
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
