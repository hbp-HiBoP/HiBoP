using HBP.Core.Data;
using HBP.UI.Tools;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.Main
{
    public class SingleTagFilterCreator : ObjectCreator<SingleTagFilter>
    {
        #region Properties

        protected List<object> m_FilteringObjects;

        public List<object> FilteringObjects
        {
            get => m_FilteringObjects;
            set => m_FilteringObjects = value;
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
