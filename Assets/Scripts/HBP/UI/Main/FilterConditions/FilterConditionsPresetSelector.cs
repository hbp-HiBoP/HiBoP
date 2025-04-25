using HBP.Core.Data;
using HBP.UI.Tools;
using HBP.UI.Tools.Lists;
using UnityEngine;

namespace HBP.UI.Main
{
    public class FilterConditionsPresetSelector : ObjectSelector<FilterConditionsPreset>
    {
        #region Properties
        [SerializeField] FilterConditionsPresetList m_List;
        protected override SelectableList<FilterConditionsPreset> List => m_List;
        #endregion
    }
}