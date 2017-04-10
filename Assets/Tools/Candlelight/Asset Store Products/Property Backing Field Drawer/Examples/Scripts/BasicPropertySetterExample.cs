using UnityEngine;

namespace Candlelight.Examples
{
	public class BasicPropertySetterExample : MonoBehaviour
	{
		// Use Candlelight.PropertyBackingField on a field whose property name matches after a "m_" or "_" prefix.
		[SerializeField, PropertyBackingField]
		private bool m_Bool;
		
		public bool Bool
		{
			get { return m_Bool; }
			set
			{
				if (m_Bool != value)
				{
					m_Bool = value;
					Debug.Log(string.Format("set Bool: {0}", m_Bool));
				}
			}
		}

		// Specify the property name if it does not match after a "m_" or "_" prefix.
		[SerializeField, PropertyBackingField("SomeInt")]
		private int m_Int = 0;

		public int SomeInt
		{
			get { return m_Int; }
			set
			{
				if (m_Int != value)
				{
					m_Int = value;
					Debug.Log(string.Format("set SomeInt: {0}", m_Int));
		 		}
			}
		}

		// You can also specify the attribute type or field type of a different drawer you wish to use.
		[SerializeField, PropertyBackingField(
			typeof(RangeAttribute), 0f, 1f // override attribute type and its constructor args
		)]
		private float m_Float;

		public float Float
		{
			get { return m_Float; }
			set
			{
				if (m_Float != value)
				{
					m_Float = value;
					Debug.Log(string.Format("set Float: {0}", m_Float));
				}
			}
		}
	}
}