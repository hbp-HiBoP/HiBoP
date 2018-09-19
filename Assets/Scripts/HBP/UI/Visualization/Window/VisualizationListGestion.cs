using System.Collections.Generic;
using Tools.Unity.Components;

namespace HBP.UI.Visualization
{
    public class VisualizationListGestion : ListGestion<Data.Visualization.Visualization>
    {
        #region Properties
        public new VisualizationList List;
        public override List<Data.Visualization.Visualization> Items
        {
            get
            {
                return base.Items;
            }

            set
            {
                List.Initialize();
                base.Items = value;
                List.SortByName(VisualizationList.Sorting.Descending);
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