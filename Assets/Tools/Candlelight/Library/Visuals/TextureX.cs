// 
// TextureX.cs
// 
// Copyright (c) 2014-2015, Candlelight Interactive, LLC
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
	/// Extension methods for <see cref="UnityEngine.Texture"/> and subclasses.
	/// </summary>
	public static class TextureX
	{
		/// <summary>
		/// A temporary render texture allocation.
		/// </summary>
		private static RenderTexture s_TempRenderTex;

		/// <summary>
		/// Gets a readable copy of the source <see cref="UnityEngine.Texture2D"/>.
		/// </summary>
		/// <returns>A readable copy of the source <see cref="UnityEngine.Texture2D"/>.</returns>
		/// <param name="source">Source.</param>
		public static Texture2D GetReadableCopy(this Texture2D source)
		{
			if (source == null)
			{
				return null;
			}
			Texture2D result = new Texture2D(source.width, source.height, source.format, false);
			result.hideFlags = HideFlags.DontSave;
#if !UNITY_4_6 || UNITY_PRO_LICENSE
			s_TempRenderTex = RenderTexture.GetTemporary(source.width, source.height);
			RenderTexture.active = s_TempRenderTex;
			Graphics.Blit(source, s_TempRenderTex);
			result.ReadPixels(new Rect(0f, 0f, result.width, result.height), 0 , 0);
			RenderTexture.ReleaseTemporary(s_TempRenderTex);
			RenderTexture.active = null;
#else
			try
			{
				result.SetPixels32(source.GetPixels32());
			}
			catch (UnityException)
			{
				result = null;
			}
#endif
			return result;
		}

        /// <summary>
        /// Flood fill the specified block on the specified <see cref="UnityEngine.Texture2D"/> using the specified
        /// color.
		/// </summary>
		/// <param name="texture">The <see cref="UnityEngine.Texture2D"/> to flood fill.</param>
		/// <param name="x">The x coordinate of the block.</param>
		/// <param name="y">The y coordinate of the block.</param>
		/// <param name="width">Width of the block.</param>
		/// <param name="height">Height of the block.</param>
		/// <param name="color">Color to apply to pixels in the block.</param>
		/// <param name="apply">If set to <see langword="true"/> apply results.</param>
		public static void FloodFill(
			this Texture2D texture, int x, int y, int width, int height, Color color, bool apply = true
		)
		{
			int size = width*height;
			Color[] colors = new Color[size];
			for (int i=0; i<size; ++i)
			{
				colors[i] = color;
			}
			try
			{
				texture.SetPixels(x, y, width, height, colors);
				if (apply)
				{
					texture.Apply();
				}
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
			}
		}
	}
}