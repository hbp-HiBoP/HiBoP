// 
// ObjectPool.cs
// 
// Copyright (c) 2015, Candlelight Interactive, LLC
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
using System.Collections.Generic;

namespace Candlelight
{
	/// <summary>
	/// A generic class for storing a pool of objects.
	/// </summary>
	/// <typeparam name="T">A type with a default constructor.</typeparam>
	internal class ObjectPool<T> where T : new()
	{
		/// <summary>
		/// The stack.
		/// </summary>
		private readonly Stack<T> m_Stack = new Stack<T>();
		/// <summary>
		/// Action to perform when getting an available item from the pool.
		/// </summary>
		private readonly System.Action<T> m_ActionOnGet;
		/// <summary>
		/// Action to perform when releasing an item to the pool.
		/// </summary>
		private readonly System.Action<T> m_ActionOnRelease;

		/// <summary>
		/// Gets the total number of items in the pool.
		/// </summary>
		/// <value>The total number of items in the pool.</value>
		public int Count { get; private set; }
		/// <summary>
		/// Gets the number of items currently in use.
		/// </summary>
		/// <value>The number of items currently in use.</value>
		public int CountActive { get { return this.Count - this.CountInactive; } }
		/// <summary>
		/// Gets the number of items currently available for use.
		/// </summary>
		/// <value>The number of items currently available for use.</value>
		public int CountInactive { get { return m_Stack.Count; } }

		/// <summary>
		/// Initializes a new instance of the <see cref="Candlelight.ObjectPool{T}"/> class.
		/// </summary>
		/// <param name="actionOnGet">Action to perform when getting an item.</param>
		/// <param name="actionOnRelease">Action to perform when releasing an item.</param>
		public ObjectPool(System.Action<T> actionOnGet, System.Action<T> actionOnRelease)
		{
			m_ActionOnGet = actionOnGet;
			m_ActionOnRelease = actionOnRelease;
		}

		/// <summary>
		/// Gets an available item from the pool.
		/// </summary>
		public T Get()
		{
			T element;
			if (m_Stack.Count == 0)
			{
				element = new T();
				++this.Count;
			}
			else
			{
				element = m_Stack.Pop();
			}
			if (m_ActionOnGet != null)
			{
				m_ActionOnGet(element);
			}
			return element;
		}

		/// <summary>
		/// Releases an item to the pool.
		/// </summary>
		/// <param name="element">Element to release.</param>
		public void Release(T element)
		{
			if (m_Stack.Count > 0 && ReferenceEquals(m_Stack.Peek(), element))
			{
				Debug.LogError("Internal error. Trying to destroy object that is already released to pool.");
			}
			if (m_ActionOnRelease != null)
			{
				m_ActionOnRelease(element);
			}
			m_Stack.Push(element);
		}
	}
}
