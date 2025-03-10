using HBP.Core.Data;
using HBP.UI.Tools;
using System.Collections.Generic;

namespace HBP.UI.Main
{
    public class DateFilterConditionSubModifier : SubModifier<DateFilterCondition>
    {
        #region Properties


        protected List<BaseData> m_FilteringObjects;
        public List<BaseData> FilteringObjects
        {
            get => m_FilteringObjects;
            set
            {
                m_FilteringObjects = value;
            }
        }
        #endregion
    }
}