// 
// IdentifiableBackingFieldCompatibleObjectWrapper.cs
// 
// Copyright (c) 2015-2016, Candlelight Interactive, LLC
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 
// 1. Redistributions of source code must retain the above copyright notice,
// this list of conditions and the following disclaimer.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.

using UnityEngine;

namespace Candlelight
{
	/// <summary>
	/// <para>Base class for a wrapper around serializable, backing field compatible objects that should have a unique
	/// identifier. This class is provided as a means for serializing some dictionary types.</para>
	/// <para>Make sure your subclass adds <see cref="System.SerializableAttribute"/>.</para>
	/// </summary>
	/// <remarks>See also <see cref="BackingFieldUtility"/>.</remarks>
	public abstract class IdentifiableBackingFieldCompatibleObjectWrapper : BackingFieldCompatibleObject
	{
		/// <summary>
		/// Inspector display mode.
		/// </summary>
		public enum InspectorDisplayMode
		{
			/// <summary>
			/// Default setting, which displays the identifier followed by the data.
			/// </summary>
			MultiLine,
			/// <summary>
			/// Displays the identifier field in place of a label and the data field laid out horizontally next to it.
			/// </summary>
			SingleLine,
			/// <summary>
			/// Displays only the identifier field on a single line with no label. Use this for lists that display the
			/// data for the current selection elsewhere.
			/// </summary>
			IdentifierOnly
		}

		/// <summary>
		/// Gets the data label for the inspector.
		/// </summary>
		/// <value>The data label for the inspector.</value>
		protected virtual GUIContent DataLabel { get { return new GUIContent("Data"); } }
		/// <summary>
		/// Gets a manually specified property attribute for the data backing field, if any. Override this property to
		/// specify a custom <see cref="PropertyAttribute"/> that should be indicated for the inspector.
		/// </summary>
		/// <value>The data property attribute.</value>
		protected virtual PropertyAttribute DataPropertyAttribute { get { return null; } }
		/// <summary>
		/// Gets the identifier label for the inspector.
		/// </summary>
		/// <value>The identifier label for the inspector.</value>
		protected virtual GUIContent IdentifierLabel { get { return new GUIContent("Identifier"); } }
		/// <summary>
		/// Gets a manually specified property attribute for the identifier backing field, if any. Override this
		/// property to specify a custom <see cref="PropertyAttribute"/> that should be indicated for the inspector.
		/// </summary>
		/// <value>The identifier property attribute.</value>
		protected virtual PropertyAttribute IdentifierPropertyAttribute { get { return null; } }
		/// <summary>
		/// Gets the display mode for this instance in the inspector.
		/// </summary>
		/// <value>The display mode for this instance in the inspector.</value>
		protected virtual InspectorDisplayMode DisplayMode { get { return InspectorDisplayMode.MultiLine; } }
	}

	/// <summary>
	/// <para>Base class for a wrapper around serializable, backing field compatible objects that should have a unique
	/// identifier. This class is provided as a means for serializing some dictionary types.</para>
	/// <para>Make sure your subclass adds <see cref="System.SerializableAttribute"/>.</para>
	/// </summary>
	/// <remarks>See also <see cref="BackingFieldUtility"/>.</remarks>
	/// <typeparam name="TId">
	/// The identifier type. Currently supported types are <see cref="System.Int32"/> and <see cref="System.String"/>.
	/// </typeparam>
	/// <typeparam name="T">The type of data being wrapped.</typeparam>
	[System.Serializable]
	public abstract class IdentifiableBackingFieldCompatibleObjectWrapper<TId, T> :
		IdentifiableBackingFieldCompatibleObjectWrapper, IIdentifiable<TId>
	{
		#region Backing Fields
		[SerializeField]
		private TId m_Identifier;
		[SerializeField]
		private T m_Data;
		#endregion

		#region IPropertyBackingFieldCompatible
		/// <summary>
		/// Clone this instance.
		/// </summary>
		/// <returns>A clone of this instance.</returns>
		public override object Clone()
		{
			object clone = System.Activator.CreateInstance(this.GetType(), new object[] { m_Identifier, m_Data });
			return clone;
		}

		/// <summary>
		/// Gets a hash value that is based on the values of the serialized properties of this instance.
		/// </summary>
		/// <returns>The serialized properties hash.</returns>
		public override int GetSerializedPropertiesHash()
		{
			return ObjectX.GenerateHashCode(
				this.Identifier.GetHashCode(), this.Data == null ? typeof(T).GetHashCode() : this.Data.GetHashCode()
			);
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the data.
		/// </summary>
		/// <value>The data.</value>
		public T Data
		{
			get { return m_Data; }
			protected set { SetData(value); }
		}
		/// <summary>
		/// Gets the identifier.
		/// </summary>
		/// <value>The identifier.</value>
		public TId Identifier
		{
			get
			{
				return m_Identifier = typeof(TId) == typeof(string) ?
					(m_Identifier == null ? (TId)((object)"") : m_Identifier) : m_Identifier;
			}
			protected set { SetIdentifier(value); }
		}
		#endregion

		#region Protected Properties
		/// <summary>
		/// Initializes a new instance of the <see cref="IdentifiableBackingFieldCompatibleObjectWrapper{TId, T}"/>
		/// class.
		/// </summary>
		/// <param name="id">Identifier.</param>
		/// <param name="data">Data.</param>
		protected IdentifiableBackingFieldCompatibleObjectWrapper(TId id, T data)
		{
			this.Identifier = id;
			this.Data = data;
		}

		/// <summary>
		/// Sets the data backing field.
		/// </summary>
		/// <returns>
		/// <see langword="true"/>, if the <paramref name="data"/> value differs from that in the backing field;
		/// otherwise, <see langword="false"/>.
		/// </returns>
		/// <param name="data">Data.</param>
		protected virtual bool SetData(T data)
		{
			bool changed = data == null ? (m_Data != null) : !data.Equals(m_Data);
			m_Data = data;
			return changed;
		}

		/// <summary>
		/// Sets the identifier backing field.
		/// </summary>
		/// <returns>
		/// <see langword="true"/>, if the <paramref name="identifier"/> value differs from that in the backing field;
		/// otherwise, <see langword="false"/>.
		/// </returns>
		/// <param name="identifier">Identifier.</param>
		protected virtual bool SetIdentifier(TId identifier)
		{
			bool changed = identifier == null ? (m_Identifier != null) : !identifier.Equals(m_Identifier);
			m_Identifier = identifier;
			return changed;
		}
		#endregion
	}
}