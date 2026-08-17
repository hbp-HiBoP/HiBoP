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

        protected System.Collections.Generic.List<object> m_FilteringObjects;

        public System.Collections.Generic.List<object> FilteringObjects
        {
            get => m_FilteringObjects;
            set
            {
                m_FilteringObjects = value;
                m_ObjectCreator.FilteringObjects = value;
            }
        }

        #endregion

        #region Public Methods

        protected override ObjectModifier<FilterConditionsPreset> OpenModifier(FilterConditionsPreset obj)
        {
            var modifier = base.OpenModifier(obj) as FilterConditionsPresetModifier;
            modifier.FilteringObjects = m_FilteringObjects;
            return modifier;
        }

        #endregion
    }
}
