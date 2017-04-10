using UnityEngine;

namespace Candlelight.Examples
{
	// Custom classes need to inherit from Candlelight.BackingFieldCompatibleObject or manually implement
	// Candlelight.IPropertyBackingFieldCompatible.
	[System.Serializable]
	public class Character : Candlelight.BackingFieldCompatibleObject
	{
		[SerializeField, PropertyBackingField]
		private string m_Name = "";		
		[SerializeField, PropertyBackingField]
		private float m_MaxHealth = 0f;
		public string Name
		{
			get { return m_Name = m_Name ?? string.Empty; }
			set
			{
				if (m_Name != value)
				{
					m_Name = string.IsNullOrEmpty(value) ? string.Empty : value;
					Debug.Log(string.Format("set Name: {0}", m_Name));
				}
			}
		}
		public float MaxHealth
		{
			get { return m_MaxHealth; }
			set
			{
				if (m_MaxHealth != value)
				{
					m_MaxHealth = Mathf.Clamp01(value);
					Debug.Log(string.Format("set MaxHealth: {0}", m_MaxHealth));
				}
			}
		}

		private Character() {}

		public Character(string name, float maxHealth = 1f)
		{
			this.Name = name;
			this.MaxHealth = maxHealth;
		}

		public override object Clone()
		{
			// Call parameterless constructor here, since it does not invoke setters.
			Character clone = new Character();
			clone.m_MaxHealth = this.MaxHealth;
			clone.m_Name = this.Name;
			return clone;
		}

		public override int GetSerializedPropertiesHash()
		{
			// Only generate a hash code from values that will be serialized.
			return ObjectX.GenerateHashCode(m_MaxHealth.GetHashCode(), this.Name.GetHashCode());
		}

		public override string ToString()
		{
			return string.Format("[Character: MaxHealth={0}, Name={1}]", MaxHealth, Name);
		}
	}

	// Custom structs need to manually implement Candlelight.IPropertyBackingFieldCompatible.
	// Use generic version of interface as a handy reminder to implement IEquatable<T>.
	[System.Serializable]
	public struct OrdinalName : Candlelight.IPropertyBackingFieldCompatible<OrdinalName>
	{
		[SerializeField]
		private int m_Index;
		[SerializeField]
		private string m_Name;
		public int Index { get { return m_Index; } }
		public string Name { get { return m_Name = m_Name ?? string.Empty; } }

		public OrdinalName(int index, string name) : this()
		{
			m_Index = index;
			m_Name = name ?? string.Empty;
		}

		object System.ICloneable.Clone()
		{
			return this;
		}

		public override bool Equals(object obj)
		{
			return (obj == null || !(obj is OrdinalName)) ? false : Equals((OrdinalName)obj);
		}

		public bool Equals(OrdinalName other)
		{
			return m_Index == other.m_Index && string.Equals(this.Name, other.Name);
		}

		public override int GetHashCode()
		{
			return ObjectX.GenerateHashCode(m_Index.GetHashCode(), this.Name.GetHashCode());
		}

		int Candlelight.IPropertyBackingFieldCompatible.GetSerializedPropertiesHash()
		{
			// Only generate a hash code from values that will be serialized.
			// NOTE: All fields on this type are serialized.
			return GetHashCode();
		}

		public override string ToString()
		{
			return string.Format("[OrdinalName: Index={0}, Name={1}]", Index, Name);
		}
	}
}