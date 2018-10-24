using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.Experience.Protocol
{
    public class BlocListGestion : Tools.Unity.Components.ListGestion<Data.Experience.Protocol.Bloc>
    {
        [SerializeField] new BlocList List;
        public override List<Data.Experience.Protocol.Bloc> Items
        {
            get
            {
                return base.Items;
            }

            set
            {
                List.Initialize();
                base.Items = value;
                List.SortByName(BlocList.Sorting.Descending);
            }
        }

        public override void Initialize()
        {
            base.List = List;
            base.Initialize();
        }
    }
}