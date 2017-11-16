// 
// HashPool.cs
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
	/// A generic class for storing a pool of hash sets.
	/// </summary>
	/// <typeparam name="T">The hash set element type.</typeparam>
	public static class HashPool<T>
	{
		/// <summary>
		/// A disposable struct that can be used in conjunction with the "using" keyword to retrieve a pooled hash set.
		/// </summary>
		public struct Scope : System.IDisposable
		{
			#region Backing Fields
			private HashSet<T> m_HashSet;
			#endregion

			/// <summary>
			/// Gets the pooled hash set.
			/// </summary>
			/// <value>The pooled hash set.</value>
			public HashSet<T> HashSet { get { return m_HashSet = m_HashSet == null ? s_Pool.Get() : m_HashSet; } }

			/// <summary>
			/// Releases all resource used by the <see cref="HashPool{T}.Scope"/> object.
			/// </summary>
			/// <remarks>
			/// Call <see cref="Dispose"/> when you are finished using the <see cref="HashPool{T}.Scope"/>. The
			/// <see cref="Dispose"/> method leaves the <see cref="HashPool{T}.Scope"/> in an unusable state. After
			/// calling <see cref="Dispose"/>, you must release all references to the <see cref="HashPool{T}.Scope"/>
			/// so the garbage collector can reclaim the memory that the <see cref="HashPool{T}.Scope"/> was
			/// occupying.
			/// </remarks>
			public void Dispose()
			{
				if (m_HashSet != null)
				{
					s_Pool.Release(m_HashSet);
				}
			}
		}

		/// <summary>
		/// An underlying <see cref="ObjectPool{T}"/>.
		/// </summary>
		private static readonly ObjectPool<HashSet<T>> s_Pool = new ObjectPool<HashSet<T>>(null, h => h.Clear());

		/// <summary>
		/// Gets an available hash set from the pool.
		/// </summary>
		/// <returns>An available hash set from the pool.</returns>
		public static HashSet<T> Get()
		{
			return s_Pool.Get();
		}

		/// <summary>
		/// Releases a hash set to the pool.
		/// </summary>
		/// <param name="hashSet">Hash set to release.</param>
		public static void Release(HashSet<T> hashSet)
		{
			s_Pool.Release(hashSet);
		}
	}
}
