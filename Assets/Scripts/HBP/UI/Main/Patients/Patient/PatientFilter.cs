using HBP.Core.Data;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class PatientFilter : ListFilter
    {
        #region Private Methods
        protected override bool CheckConditions(BaseData obj)
        {
            bool result = true;
            foreach (var condition in m_ListGestion.List.Objects)
            {
                result &= condition.Check(obj);
            }
            return result;
        }
        #endregion
    }
}