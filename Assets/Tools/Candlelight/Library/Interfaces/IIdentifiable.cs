// 
// IIdentifiable.cs
// 
// Copyright (c) 2015, Candlelight Interactive, LLC
// All rights reserved.
// 
// This file is licensed according to the terms of the Unity Asset Store EULA:
// http://download.unity3d.com/assetstore/customer-eula.pdf
// 
// This file contains an interface to specify that an object can be identified.

namespace Candlelight
{
	/// <summary>
	/// An interface to specify an object has an identifier.
	/// </summary>
	/// <typeparam name="T">The type of the identifier.</typeparam>
	public interface IIdentifiable<T>
	{
		/// <summary>
		/// Gets the identifier.
		/// </summary>
		/// <value>The identifier.</value>
		T Identifier { get; }
	}
}