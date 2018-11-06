using System;
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
            dropdown.value = enumValue;
            dropdown.RefreshShownValue();
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