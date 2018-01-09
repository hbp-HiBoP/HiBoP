using UnityEngine;
using System.IO;
using UnityEngine.UI;

namespace Tools.Unity
{
    public static class RectTransformExtension
    {
        public static Rect ToScreenSpace(this RectTransform rectTransform)
        {
            Vector2 size = Vector2.Scale(rectTransform.rect.size, rectTransform.lossyScale);
            return new Rect((Vector2)rectTransform.position - (size * 0.5f), size);
        }
    }

    public static class RenderTextureExtension
    {
        public static Texture2D ToTexture2D(this RenderTexture renderTexture)
        {
            // Remember currently active render texture
            RenderTexture currentActiveRenderTexture = RenderTexture.active;

            // Set the supplied RenderTexture as the active one
            RenderTexture.active = renderTexture;

            // Create a new Texture2D and read the RenderTexture image into it
            Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height);
            texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);

            // Restorie previously active render texture
            RenderTexture.active = currentActiveRenderTexture;
            return texture;
        }
    }

    public static class NumberExtension
    {
        public static bool IsPowerOfTwo(this int x)
        {
            return (x != 0) && ((x & (x - 1)) == 0);
        }
    }
}