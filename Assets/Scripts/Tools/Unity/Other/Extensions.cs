using UnityEngine;
using System.IO;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using Tools.CSharp;
using System;

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

    public static class PathExtension
    {
        public const string PROJECT_TOKEN = ".";

        public static string ConvertToFullPath(this string path)
        {
            string localPath = path;
            
            if (localPath.StartsWith(PROJECT_TOKEN))
            {
                localPath = path.Remove(0, PROJECT_TOKEN.Length);
                localPath = ApplicationState.ProjectLoadedTMPFullPath + localPath;
            }

            foreach (var alias in ApplicationState.ProjectLoaded.Settings.Aliases)
            {
                alias.ConvertKeyToValue(ref localPath);
            }

            return localPath;
        }
        public static string ConvertToShortPath(this string path)
        {
            string localPath = path;

            if (localPath.StartsWith(ApplicationState.ProjectLoadedTMPFullPath))
            {
                localPath = PROJECT_TOKEN + path.Remove(0, ApplicationState.ProjectLoadedTMPFullPath.Length);
            }

            foreach (var alias in ApplicationState.ProjectLoaded.Settings.Aliases)
            {
                alias.ConvertValueToKey(ref localPath);
            }

            return localPath;
        }
    }

    public static class TextExtension
    {
        public static void SetLayoutElementMinimumWidthToContainWholeText(this Text text)
        {
            LayoutElement layoutElement = text.GetComponent<LayoutElement>();
            if (layoutElement)
            {
                int totalWidth = 0;
                CharacterInfo charInfo = new CharacterInfo();
                char[] stringArray = text.text.ToCharArray();
                foreach (var c in stringArray)
                {
                    text.font.GetCharacterInfo(c, out charInfo, text.fontSize);
                    totalWidth += charInfo.advance;
                }
                text.GetComponent<LayoutElement>().minWidth = totalWidth;
            }
        }
    }

    public static class ArrayExtensions
    {
        // Inspired from several Stack Overflow discussions and an implementation by David Walker at http://coding.grax.com/2011/11/initialize-array-to-value-in-c-very.html
        public static void Fill<T>(this T[] destinationArray, params T[] value)
        {
            if (destinationArray.Length == 0)
                return;

            if (destinationArray == null)
            {
                throw new ArgumentNullException("destinationArray");
            }

            if (value.Length > destinationArray.Length)
            {
                throw new ArgumentException("Length of value array must not be more than length of destination " + value.Length + " " + destinationArray.Length);
            }

            // set the initial array value
            Array.Copy(value, destinationArray, value.Length);

            int copyLength, nextCopyLength;

            for (copyLength = value.Length; (nextCopyLength = copyLength << 1) < destinationArray.Length; copyLength = nextCopyLength)
            {
                Array.Copy(destinationArray, 0, destinationArray, copyLength, copyLength);
            }

            Array.Copy(destinationArray, 0, destinationArray, copyLength, destinationArray.Length - copyLength);
        }
    }

    public static class FileSystemExtensions
    {
        public static void CopyFilesRecursively(this DirectoryInfo source, DirectoryInfo target)
        {
            if (!source.Exists) return;

            if (!target.Exists) Directory.CreateDirectory(target.FullName);

            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);
        }

        public static string CopyToDirectory(this string path, DirectoryInfo targetDirectory, bool overwrite = false)
        {
            if (string.IsNullOrEmpty(path)) return "";

            FileInfo file = new FileInfo(path);

            if (!file.Exists) return path;
            if (!targetDirectory.Exists) return path;

            string newFilePath = Path.Combine(targetDirectory.FullName, file.Name);
            if (new FileInfo(newFilePath).Exists) return newFilePath;

            File.Copy(file.FullName, newFilePath, overwrite);
            return newFilePath;
        }
    }
}