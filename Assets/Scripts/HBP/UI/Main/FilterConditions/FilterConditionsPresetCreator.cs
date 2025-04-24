using HBP.Core.Data;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class FilterConditionsPresetCreator : ObjectCreator<FilterConditionsPreset>
    {
        #region Properties
        public System.Collections.Generic.List<BaseData> FilteringObjects { get; set; }
        #endregion

        #region Private Methods
        protected override ObjectModifier<FilterConditionsPreset> OpenModifier(FilterConditionsPreset obj)
        {
            var modifier = base.OpenModifier(obj) as FilterConditionsPresetModifier;
            modifier.FilteringObjects = FilteringObjects;
            return modifier;
        }
        #endregion
    }
}