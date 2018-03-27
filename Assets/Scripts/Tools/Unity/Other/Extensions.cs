using UnityEngine;
using System.IO;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

namespace Tools.Unity
{
    public static class RectTransformExtension
    {
        public static Rect ToScreenSpace(this RectTransform rectTransform)
        {
            Vector2 size = Vector2.Scale(rectTransform.rect.size, rectTransform.lossyScale);
            Rect rect = new Rect(rectTransform.position.x, rectTransform.position.y, size.x, size.y);
            rect.x -= (rectTransform.pivot.x * size.x);
            rect.y -= (rectTransform.pivot.y * size.y);
            return rect;
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

    public static class ColorExtension
    {
        public static string ToHexString(this Color color)
        {
            return "#" + ((int)(color.r * 255)).ToString("X2") + ((int)(color.g * 255)).ToString("X2") + ((int)(color.b * 255)).ToString("X2");
        }
    }

    public static class NumberExtension
    {
        public static bool IsPowerOfTwo(this int x)
        {
            return (x != 0) && ((x & (x - 1)) == 0);
        }

        public static bool AreMultiples(this List<int> numbers)
        {
            return numbers.Contains(numbers.GCD());
        }

        public static int GCD(this List<int> numbers)
        {
            return numbers.Aggregate(GCD);
        }

        public static int GCD(int a, int b)
        {
            return b == 0 ? a : GCD(b, a % b);
        }
    }
}