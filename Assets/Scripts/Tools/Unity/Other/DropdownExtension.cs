using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Tools.CSharp;
using UnityEngine.UI;

namespace Tools.Unity
{
    public static class DropdownExtension
    {
        public static void Set(this Dropdown dropdown, Type enumType, int enumValue)
        {
            dropdown.options = Enum.GetNames(enumType).Select((name) => new Dropdown.OptionData(StringExtension.CamelCaseToWords(name))).ToList();
            dropdown.SetValue(enumValue);
            dropdown.RefreshShownValue();
        }

        public static Type[] Set(this Dropdown dropdown, Type parentType)
        {
            Type[] types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(t => t.IsSubclassOf(parentType)).ToArray();
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            foreach (var type in types)
            {
                object[] hideAttributes = type.GetCustomAttributes(typeof(Hide), false);
                if(hideAttributes.Length == 0)
                {
                    object[] displayNameAttributes = type.GetCustomAttributes(typeof(DisplayNameAttribute), false);
                    if (displayNameAttributes.Length > 0)
                    {
                        options.Add(new Dropdown.OptionData((displayNameAttributes[0] as DisplayNameAttribute).DisplayName));
                    }
                    else
                    {
                        options.Add(new Dropdown.OptionData(StringExtension.CamelCaseToWords(type.Name)));
                    }
                }
            }
            dropdown.options = options;
            dropdown.RefreshShownValue();
            return types;
        }

        public static Type[] Set(this Dropdown dropdown, Type parentType, DataAttribute dataAttribute)
        {
            Type[] types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(t => t.IsSubclassOf(parentType)).ToArray();
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            foreach (var type in types)
            {
                object[] displayNameAttributes = type.GetCustomAttributes(typeof(DisplayNameAttribute), false);
                if (displayNameAttributes.Length > 0)
                {
                    options.Add(new Dropdown.OptionData((displayNameAttributes[0] as DisplayNameAttribute).DisplayName));
                }
                else
                {
                    options.Add(new Dropdown.OptionData(StringExtension.CamelCaseToWords(type.Name)));
                }
            }
            dropdown.options = options;
            dropdown.RefreshShownValue();
            return types;
        }

        public static void SetValue(this Dropdown dropdown, int value)
        {
            if (dropdown.value == value)
            {
                dropdown.onValueChanged.Invoke(value);
            }
            else
            {
                dropdown.value = value;
            }
        }
    }
}