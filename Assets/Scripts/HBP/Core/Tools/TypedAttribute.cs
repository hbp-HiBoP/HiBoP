using System;

namespace HBP.Core.Tools
{
    public class TypedAttribute : Attribute
    {
        public Type[] Types { get; protected set; }

        public TypedAttribute()
        {
            Types = new Type[] { typeof(object) };
        }

        public TypedAttribute(params Type[] type)
        {
            Types = type;
        }
    }
}
