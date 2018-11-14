using System.Collections.Generic;
using Tools.Unity.Components;

namespace HBP.UI.Anatomy
{
    public class GroupListGestion : ListGestion<Data.Group>
    {
        #region Properties
        public new GroupList List;
        public override List<Data.Group> Objects
        {
            get
            {
                return base.Objects;
            }

            set
            {
                List.Initialize();
                base.Objects = value;
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