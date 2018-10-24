using System.Collections.Generic;
using Tools.Unity.Components;

namespace HBP.UI.Anatomy
{
    public class GroupListGestion : ListGestion<Data.Group>
    {
        #region Properties
        public new GroupList List;
        public override List<Data.Group> Items
        {
            get
            {
                return base.Items;
            }

            set
            {
                List.Initialize();
                base.Items = value;
                List.SortByName(GroupList.Sorting.Descending);
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