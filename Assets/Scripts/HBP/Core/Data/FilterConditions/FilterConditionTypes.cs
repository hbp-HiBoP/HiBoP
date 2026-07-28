using HBP.Core.Tools;
using System;

namespace HBP.Core.Data
{
    public class FilterConditionAttribute : TypedAttribute
    {
        public FilterConditionAttribute() : base()
        {
        }

        public FilterConditionAttribute(params Type[] type) : base(type)
        {
        }
    }
}
