using System;

namespace HBP.Core.Tools
{
    public class TypedAttribute : Attribute
    {
        public Type Type { get; }

        public TypedAttribute(Type type)
        {
            Type = type;
        }
    }
}