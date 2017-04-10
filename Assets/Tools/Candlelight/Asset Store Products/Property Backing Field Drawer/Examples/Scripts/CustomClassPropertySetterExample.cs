using UnityEngine;
using System.Linq;

namespace Candlelight.Examples
{
	public class CustomClassPropertySetterExample : MonoBehaviour
	{
		// Define a property attribute for elements that will be drawn in a reorderable list inspector.
		public class TwoLineReorderableListElement : UnityEngine.PropertyAttribute {}

		[Header("Custom Struct")]
		[SerializeField, PropertyBackingField]
		private OrdinalName m_OrdinalName = new OrdinalName(1, "first");
		
		public OrdinalName OrdinalName
		{
			get { return m_OrdinalName; }
			set
			{
				if (!value.Equals(m_OrdinalName))
				{
					m_OrdinalName = value;
					Debug.Log(string.Format("set OrdinalName: {0}", m_OrdinalName));
				}
			}
		}

		[SerializeField, PropertyBackingField(typeof(TwoLineReorderableListElement))]
		private OrdinalName[] m_OrdinalNames = new OrdinalName[0];
		
		public OrdinalName[] GetOrdinalNames()
		{
			return (OrdinalName[])m_OrdinalNames.Clone();
		}
		
		public void SetOrdinalNames(OrdinalName[] value)
		{
			value = value ?? new OrdinalName[0];
			// Sequence comparisons for custom classes or structs should compare
			// IPropertyBackingFieldCompatible.GetSerializedPropertiesHash(); in this case you can supply
			// Candlelight.BackingFieldUtility<T>.Comparer to System.Linq.SequenceEqual().
			if (
				m_OrdinalNames == null ||
				!m_OrdinalNames.SequenceEqual(value, BackingFieldUtility<OrdinalName>.Comparer)
			)
			{
				m_OrdinalNames = (OrdinalName[])value.Clone();
				Debug.Log(
					string.Format(
						"SetOrdinalNames: [{0}]",
						string.Join(", ", (from element in m_OrdinalNames select element.ToString()).ToArray())
					)
				);
			}
		}

		[Header("Custom Class")]
		[SerializeField, PropertyBackingField]
		private Character m_Character = null;
		
		public Character Character
		{
			get { return m_Character; }
			set
			{
				if (m_Character == null && value == null)
				{
					return;
				}
				else if (
					m_Character == null ||
					value == null ||
					m_Character.GetSerializedPropertiesHash() != value.GetSerializedPropertiesHash()
				)
				{
					m_Character = value;
					Debug.Log(string.Format("set Character: {0}", m_Character));
				}
			}
		}

		[SerializeField, PropertyBackingField(typeof(TwoLineReorderableListElement))]
		private Character[] m_Characters = new Character[0];

		public Character[] GetCharacters()
		{
			return (Character[])m_Characters.Clone();
		}
		
		public void SetCharacters(Character[] value)
		{
			value = value ?? new Character[0];
			if (m_Characters == null || !m_Characters.SequenceEqual(value, BackingFieldUtility<Character>.Comparer))
			{
				m_Characters = (Character[])value.Clone();
				Debug.Log(
					string.Format(
						"SetCharacters: [{0}]",
						string.Join(", ", (from element in m_Characters select element.ToString()).ToArray())
					)
				);
			}
		}
	}
}