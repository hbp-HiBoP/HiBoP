// 
// EditorUtilityX.cs
// 
// Copyright (c) 2013-2014, Candlelight Interactive, LLC
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
// 
// This file contains a static class for editor utility extensions.

using UnityEngine;
using UnityEditor;

namespace Candlelight
{
	/// <summary>
	/// Editor utility extension class.
	/// </summary>
	public static class EditorUtilityX
	{
		/// <summary>
		/// Marks target objects as dirty.
		/// </summary>
		/// <param name="objects">
		/// Objects to dirty.</param>
		/// <typeparam name="T">A type of <see cref="UnityEngine.Object"/>.</typeparam>
		public static void SetDirty<T>(System.Collections.Generic.IEnumerable<T> objects) where T : Object
		{
			foreach (T obj in objects)
			{
				if (obj != null)
				{
					EditorUtility.SetDirty(obj);
				}
			}
		}
	}
}