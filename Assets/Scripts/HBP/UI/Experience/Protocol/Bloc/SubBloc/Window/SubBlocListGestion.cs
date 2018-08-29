using System.Collections.Generic;
using Tools.Unity.Components;
using HBP.Data.Experience.Protocol;

namespace HBP.UI.Experience.Protocol
{
    public class SubBlocListGestion : ListGestion<SubBloc>
    {
        public new SubBlocList List;
        public override List<SubBloc> Items
        {
            get
            {
                return base.Items;
            }

            set
            {
                List.Initialize();
                base.Items = value;
                List.SortByName(SubBlocList.Sorting.Descending);
            }
        }

        public override void Initialize()
        {
            base.List = List;
            base.Initialize();
        }
    }
}