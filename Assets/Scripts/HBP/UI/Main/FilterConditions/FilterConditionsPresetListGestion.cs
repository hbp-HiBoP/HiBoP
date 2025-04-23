using HBP.Core.Data;
using HBP.UI.Tools;
using HBP.UI.Tools.Lists;
using UnityEngine;

namespace HBP.UI.Main
{
    public class FilterConditionsPresetListGestion : ListGestion<FilterConditionsPreset>
    {
        #region Properties
        [SerializeField] FilterConditionsPresetList m_List;
        public override ActionableList<FilterConditionsPreset> List => m_List;

        [SerializeField] FilterConditionsPresetCreator m_ObjectCreator;
        public override ObjectCreator<FilterConditionsPreset> ObjectCreator => m_ObjectCreator;
        #endregion
    }
}