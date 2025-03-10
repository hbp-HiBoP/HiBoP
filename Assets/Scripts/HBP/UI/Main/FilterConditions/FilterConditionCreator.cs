using HBP.Core.Data;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class FilterConditionCreator : ObjectCreator<BaseFilterCondition>
    {
        #region Properties
        protected System.Collections.Generic.List<BaseData> m_FilteringObjects;
        public System.Collections.Generic.List<BaseData> FilteringObjects
        {
            get => m_FilteringObjects;
            set
            {
                m_FilteringObjects = value;
            }
        }
        #endregion

        #region Public Methods
        public override void CreateFromScratch()
        {
            OpenModifier(new NameFilterCondition());
        }
        protected override ObjectModifier<BaseFilterCondition> OpenModifier(BaseFilterCondition obj)
        {
            var modifier = base.OpenModifier(obj) as FilterConditionModifier;
            modifier.FilteringObjects = m_FilteringObjects;
            return modifier;
        }
        #endregion
    }
}