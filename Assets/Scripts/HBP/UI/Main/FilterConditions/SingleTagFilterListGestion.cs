using HBP.Core.Data;
using HBP.UI.Tools;
using HBP.UI.Tools.Lists;
using UnityEngine;

namespace HBP.UI.Main
{
    public class SingleTagFilterListGestion : ListGestion<SingleTagFilter>
    {
        #region Properties

        [SerializeField] protected SingleTagFilterList m_List;
        public override ActionableList<SingleTagFilter> List => m_List;

        [SerializeField] protected SingleTagFilterCreator m_ObjectCreator;
        public override ObjectCreator<SingleTagFilter> ObjectCreator => m_ObjectCreator;

        protected System.Collections.Generic.List<object> m_FilteringObjects;

        public System.Collections.Generic.List<object> FilteringObjects
        {
            get => m_FilteringObjects;
            set
            {
                m_FilteringObjects = value;
                m_ObjectCreator.FilteringObjects = FilteringObjects;
            }
        }

        #endregion

        #region Protected Methods

        protected override ObjectModifier<SingleTagFilter> OpenModifier(SingleTagFilter item)
        {
            SingleTagFilterModifier modifier = (SingleTagFilterModifier)base.OpenModifier(item);
            modifier.FilteringObjects = FilteringObjects;
            return modifier;
        }

        #endregion
    }
}
