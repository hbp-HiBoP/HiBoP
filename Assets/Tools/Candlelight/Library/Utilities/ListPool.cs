// 
// ListPool.cs
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

using System.Collections.Generic;

namespace Candlelight
{
	/// <summary>
	/// A generic class for storing a pool of lists.
	/// </summary>
	/// <typeparam name="T">The list element type.</typeparam>
	public static class ListPool<T>
	{
		/// <summary>
		/// A disposable struct that can be used in conjunction with the "using" keyword to retrieve a pooled list.
		/// </summary>
		public struct Scope : System.IDisposable
		{
			#region Backing Fields
			private List<T> m_List;
			#endregion

			/// <summary>
			/// Gets the pooled list.
			/// </summary>
			/// <value>The pooled list.</value>
			public List<T> List { get { return m_List = m_List == null ? s_Pool.Get() : m_List; } }

			/// <summary>
			/// Releases all resource used by the <see cref="ListPool{T}.Scope"/> object.
			/// </summary>
			/// <remarks>
			/// Call <see cref="Dispose"/> when you are finished using the <see cref="ListPool{T}.Scope"/>. The
			/// <see cref="Dispose"/> method leaves the <see cref="ListPool{T}.Scope"/> in an unusable state. After
			/// calling <see cref="Dispose"/>, you must release all references to the <see cref="ListPool{T}.Scope"/>
			/// so the garbage collector can reclaim the memory that the <see cref="ListPool{T}.Scope"/> was
			/// occupying.
			/// </remarks>
			public void Dispose()
			{
				if (m_List != null)
				{
					s_Pool.Release(m_List);
				}
			}
		}

		/// <summary>
		/// An underlying <see cref="ObjectPool{T}"/>.
		/// </summary>
		private static readonly ObjectPool<List<T>> s_Pool = new ObjectPool<List<T>>(null, l => l.Clear());

		/// <summary>
		/// Gets an available list from the pool.
		/// </summary>
		/// <returns>An available list from the pool.</returns>
		public static List<T> Get()
		{
			return s_Pool.Get();
		}

		/// <summary>
		/// Releases a list to the pool.
		/// </summary>
		/// <param name="list">List to release.</param>
		public static void Release(List<T> list)
		{
			s_Pool.Release(list);
		}
	}
}
