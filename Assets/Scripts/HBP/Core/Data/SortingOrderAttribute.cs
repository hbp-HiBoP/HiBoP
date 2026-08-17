using System;

namespace HBP.Core
{
    public class SortingOrderAttribute : Attribute
    {
        public int Order { get; }

        public SortingOrderAttribute(int order)
        {
            Order = order;
        }
    }
}
