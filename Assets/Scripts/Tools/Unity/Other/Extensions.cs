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
            Rect rect = new Rect(rectTransform.position.x, rectTransform.position.y, size.x, size.y);
            rect.x -= (rectTransform.pivot.x * size.x);
            rect.y -= (rectTransform.pivot.y * size.y);
            return rect;
        }
        public static Vector2 GetRatioPosition(this RectTransform rectTransform,Vector2 position)
        {
            Vector2 localPosition = Input.mousePosition - rectTransform.position - (Vector3)rectTransform.rect.min;
            Vector2 ratio = new Vector2(localPosition.x / rectTransform.rect.width, localPosition.y / rectTransform.rect.height);
            Vector2 clampedRatio = new Vector2(Mathf.Clamp01(ratio.x), Mathf.Clamp01(ratio.y));
            return clampedRatio;
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
    }
}