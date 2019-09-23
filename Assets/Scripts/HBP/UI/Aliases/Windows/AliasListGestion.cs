using System.Collections.Generic;
using Tools.Unity.Components;

namespace HBP.UI.Alias
{
    public class AliasListGestion : ListGestion<Data.Preferences.Alias>
    {
        #region Properties
        public new AliasList List;
        public override List<Data.Preferences.Alias> Objects
        {
            get
            {
                return base.Objects;
            }

            set
            {
                List.Initialize();
                base.Objects = value;
                List.SortByKey(AliasList.Sorting.Descending);
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