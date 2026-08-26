using System.Reflection;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.UI.Main;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HBP.Tests.PlayMode.UI
{
    public class FilterConditionsPlayModeTests
    {
        [Test]
        [Category("PlayMode.FilterConditions")]
        public void NumberTagFilterValueSubModifier_PreservesGreaterOrEqualWhenOpened()
        {
            GameObject root = new("Number Tag Filter Value SubModifier Test");

            try
            {
                NumberTagFilterValueSubModifier modifier = root.AddComponent<NumberTagFilterValueSubModifier>();
                Dropdown typeDropdown = CreateDropdown(root.transform);

                SetPrivateField(modifier, "m_TypeDropdown", typeDropdown);
                SetPrivateField(modifier, "m_ValueInputField", CreateInputField("Value", root.transform));
                SetPrivateField(modifier, "m_MinInputField", CreateInputField("Min", root.transform));
                SetPrivateField(modifier, "m_MaxInputField", CreateInputField("Max", root.transform));

                modifier.Initialize();

                NumberTagFilterValue filterValue = new()
                {
                    Type = NumberComparisonType.GreaterOrEqual,
                    Value = 42
                };

                modifier.Object = filterValue;

                Assert.That(typeDropdown.value, Is.EqualTo((int)NumberComparisonType.GreaterOrEqual));
                Assert.That(filterValue.Type, Is.EqualTo(NumberComparisonType.GreaterOrEqual));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Dropdown CreateDropdown(Transform parent)
        {
            GameObject dropdownObject = new("Type", typeof(RectTransform), typeof(Dropdown));
            dropdownObject.transform.SetParent(parent, false);
            return dropdownObject.GetComponent<Dropdown>();
        }

        private static InputField CreateInputField(string name, Transform parent)
        {
            GameObject container = new($"{name} Container", typeof(RectTransform));
            container.transform.SetParent(parent, false);

            GameObject inputObject = new(name, typeof(RectTransform), typeof(InputField));
            inputObject.transform.SetParent(container.transform, false);
            return inputObject.GetComponent<InputField>();
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName} on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
