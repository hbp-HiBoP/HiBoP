using HBP.Core.Tools;
using System;

namespace HBP.Core.Data
{
    public class FilterConditionAttribute : TypedAttribute
    {
        public FilterConditionAttribute(Type type) : base(type)
        {
        }
    }
}