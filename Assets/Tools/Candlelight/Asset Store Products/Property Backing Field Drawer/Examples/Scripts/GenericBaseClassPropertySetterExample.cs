using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Candlelight.Examples
{
	public abstract class GenericBaseClassPropertySetterExample<T> : MonoBehaviour
	{
		// specify open generic type in base class attribute
		[SerializeField, PropertyBackingField]
		private T m_Single;
		[SerializeField, PropertyBackingField]
		private List<T> m_Array;

		public T Single
		{
			get { return m_Single; }
			set
			{
				if (!value.Equals(m_Single))
				{
					m_Single = value;
					Debug.Log(string.Format("set Single ({0}): {1}", typeof(T), m_Single));
				}
			}
		}

		public T[] GetArray()
		{
			return m_Array.ToArray();
		}

		public void SetArray(T[] value)
		{
			value = value ?? new T[0];
			if (m_Array == null || !m_Array.SequenceEqual(value))
			{
				m_Array = new List<T>(value);
				Debug.Log(
					string.Format(
						"SetArray ({0}): [{1}]",
						typeof(T),
						string.Join(", ", (from element in m_Array select element.ToString()).ToArray())
					)
				);
			}
		}
	}
}