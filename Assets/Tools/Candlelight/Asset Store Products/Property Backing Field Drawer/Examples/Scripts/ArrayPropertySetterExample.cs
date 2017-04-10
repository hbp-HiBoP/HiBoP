using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Candlelight.Examples
{
	public class ArrayPropertySetterExample : MonoBehaviour
	{
		// Serializable IList properties X should implement GetX and SetX methods.
		[SerializeField, PropertyBackingField]
		private int[] m_ArrayProperty = new int[1];

		public int[] GetArrayProperty()
		{
			return (int[])m_ArrayProperty.Clone();
		}

		public void SetArrayProperty(int[] value)
		{
			value = value ?? new int[0];
			if (m_ArrayProperty == null || !m_ArrayProperty.SequenceEqual(value))
			{
				m_ArrayProperty = (int[])value.Clone();
				Debug.Log(
					string.Format(
						"SetArrayProperty: [{0}]",
						string.Join(", ", (from element in m_ArrayProperty select element.ToString()).ToArray())
					)
				);
			}
		}

		// List<T> backing fields can work with Get/Set methods that take corresponding array types (or vice versa).
		[SerializeField, PropertyBackingField]
		private List<int> m_ListProperty = new List<int>(new int[1]);
		
		public int[] GetListProperty()
		{
			return m_ListProperty.ToArray();
		}

		public void SetListProperty(int[] value)
		{
			value = value ?? new int[0];
			if (m_ListProperty == null || !m_ListProperty.SequenceEqual(value))
			{
				m_ListProperty = new List<int>(value);
				Debug.Log(
					string.Format(
						"SetListProperty: [{0}]",
						string.Join(", ", (from element in m_ListProperty select element.ToString()).ToArray())
					)
				);
			}
		}

		// You can also include multiple setters. The inspector will call the one whose parameter matches the getter's
		// return type.
		[SerializeField, PropertyBackingField]
		private List<int> m_AnotherListProperty = new List<int>(new int[1]);
		
		public int[] GetAnotherListProperty()
		{
			return m_AnotherListProperty.ToArray();
		}

		// inspector will call this setter
		private void SetAnotherListProperty(int[] value)
		{
			SetAnotherListProperty((IEnumerable<int>)value);
		}

		public void SetAnotherListProperty(IEnumerable<int> value)
		{
			if (m_AnotherListProperty == null || value == null || !m_AnotherListProperty.SequenceEqual(value))
			{
				m_AnotherListProperty = value == null ? new List<int>() : new List<int>(value);
				Debug.Log(
					string.Format(
						"SetAnotherListProperty: [{0}]",
						string.Join(", ", (from element in m_AnotherListProperty select element.ToString()).ToArray())
					)
				);
			}
		}
	}
}