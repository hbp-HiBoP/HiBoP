using System;
using System.ComponentModel;

namespace Tools.CSharp
{
    public static class TypeExtension
    {
        public static string GetDisplayName(this Type type)
        {
            object[] displayNameAttributes = type.GetCustomAttributes(typeof(DisplayNameAttribute), false);
            if(displayNameAttributes.Length > 0)
            {
                return (displayNameAttributes[0] as DisplayNameAttribute).DisplayName;
            }
            else
            {
                return type.Name;
            }
        }
    }
}