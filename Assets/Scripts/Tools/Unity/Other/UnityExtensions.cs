using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Tools.CSharp;
using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity
{
    public static class Vector2Extension
    {
        public static Vector2 MultiplyByElements(IEnumerable<Vector2> vectors)
        {
            float x = 1, y = 1;
            foreach (var vector in vectors)
            {
                x *= vector.x;
                y *= vector.y;
            }
            return new Vector2(x, y);
        }
        public static Vector2 MultiplyByElements(Vector2 v1, Vector2 v2)
        {
            return MultiplyByElements(new Vector2[] { v1, v2 });
        }
        public static Vector2 Abs(this Vector2 vector)
        {
            return new Vector2(Mathf.Abs(vector.x), Mathf.Abs(vector.y));
        }
        public static float Range(this Vector2 vector)
        {
            return vector.y - vector.x;
        }
    }

    public static class DropdownExtension
    {
        public static void Set(this Dropdown dropdown, Type enumType, int enumValue)
        {
            dropdown.options = Enum.GetNames(enumType).Select((name) => new Dropdown.OptionData(StringExtension.CamelCaseToWords(name))).ToList();
            dropdown.SetValue(enumValue);
            dropdown.RefreshShownValue();
        }
        public static Type[] Set(this Dropdown dropdown, Type parentType)
        {
            Type[] types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(t => t.IsSubclassOf(parentType)).ToArray();
            List<Type> displayedType = new List<Type>();
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            foreach (var type in types)
            {
                object[] displayNameAttributes = type.GetCustomAttributes(typeof(DisplayNameAttribute), false);
                if (displayNameAttributes.Length > 0)
                {
                    options.Add(new Dropdown.OptionData((displayNameAttributes[0] as DisplayNameAttribute).DisplayName));
                    displayedType.Add(type);
                }
            }
            dropdown.options = options;
            dropdown.RefreshShownValue();
            return displayedType.ToArray();
        }
        public static Type[] Set(this Dropdown dropdown, Type parentType, DataAttribute dataAttribute)
        {
            Type[] types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(t => t.IsSubclassOf(parentType)).ToArray();
            types = types.Where(t => t.GetCustomAttributes(true).Contains(dataAttribute)).ToArray();
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            foreach (var type in types)
            {
                object[] displayNameAttributes = type.GetCustomAttributes(typeof(DisplayNameAttribute), false);
                if (displayNameAttributes.Length > 0)
                {
                    options.Add(new Dropdown.OptionData((displayNameAttributes[0] as DisplayNameAttribute).DisplayName));
                }
                else
                {
                    options.Add(new Dropdown.OptionData(StringExtension.CamelCaseToWords(type.Name)));
                }
            }
            dropdown.options = options;
            dropdown.RefreshShownValue();
            return types;
        }
        public static void SetValue(this Dropdown dropdown, int value)
        {
            if (dropdown.value == value)
            {
                dropdown.onValueChanged.Invoke(value);
            }
            else
            {
                dropdown.value = value;
            }
        }
    }

    public static class TransformExtension
    {
        public static string GetFullName(this Transform transform)
        {
            StringBuilder stringBuilder = new StringBuilder();
            List<string> names = new List<string>();
            Transform tr = transform;
            while (tr != null)
            {
                names.Add(tr.name);
                tr = tr.parent;
            }
            int size = names.Count;
            for (int i = size - 1; i >= 0; i--)
            {
                stringBuilder.Append(names[i]);
                if (i > 0) stringBuilder.Append("/");
            }
            return stringBuilder.ToString();
        }
    }

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
        public static Vector2 GetRatioPosition(this RectTransform rectTransform, Vector2 position)
        {
            Vector2 localPosition = position - (Vector2)rectTransform.position - rectTransform.rect.min;
            Vector2 ratio = new Vector2(localPosition.x / rectTransform.rect.width, localPosition.y / rectTransform.rect.height);
            return ratio;
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

            // Restores previously active render texture
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

    public static class SpriteExtension
    {
        static string[] EXTENSIONS = new string[] { "png", "jpg" };
        public static bool LoadSpriteFromFile(out Sprite sprite, string path)
        {
            sprite = Sprite.Create(new Texture2D(1, 1), new Rect(), new Vector2());
            Texture2D texture;
            if (LoadTexture2DFromFile(out texture, path))
            {
                texture.Apply(true, false);
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                return true;
            }
            else return false;
        }
        public static bool LoadTexture2DFromFile(out Texture2D texture, string path)
        {
            texture = new Texture2D(0, 0);
            if (!string.IsNullOrEmpty(path))
            {
                FileInfo fileInfo = new FileInfo(path);
                if (fileInfo.Exists && (EXTENSIONS.Contains(fileInfo.Extension.Substring(1))))
                {
                    byte[] bytes = File.ReadAllBytes(path);
                    System.Drawing.Image image = System.Drawing.Image.FromFile(path);
                    texture = new Texture2D(image.Width, image.Height);
                    image.Dispose();
                    return texture.LoadImage(bytes);
                }
                else return false;
            }
            return false;
        }
        public static bool IsFileLoadable(string path)
        {
            return File.Exists(path) && EXTENSIONS.Contains(new FileInfo(path).Extension.Substring(1));
        }
    }

    public static class Texture2DExtension
    {
        public static bool LoadPNG(this Texture2D texture, string filePath)
        {
            if (filePath != string.Empty)
            {
                FileInfo l_fileInfo = new FileInfo(filePath);
                if (l_fileInfo.Exists && (l_fileInfo.Extension == ".png" || l_fileInfo.Extension == ".jpg"))
                {
                    return texture.LoadImage(File.ReadAllBytes(filePath));
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        public static void Rotate(this Texture2D textureToRotate)
        {
            Texture2D l_texture = new Texture2D(textureToRotate.height, textureToRotate.width);
            for (int y = 0; y < l_texture.height; y++)
            {
                for (int x = 0; x < l_texture.width; x++)
                {
                    l_texture.SetPixel(x, y, textureToRotate.GetPixel(y, x));
                }
            }
            l_texture.Apply();
            textureToRotate = l_texture;
        }
        public static Texture2D RotateTexture(this Texture2D textureToRotate)
        {
            Texture2D l_texture = new Texture2D(textureToRotate.height, textureToRotate.width);
            for (int y = 0; y < l_texture.height; y++)
            {
                for (int x = 0; x < l_texture.width; x++)
                {
                    l_texture.SetPixel(x, y, textureToRotate.GetPixel(y, x));
                }
            }
            l_texture.Apply();
            return l_texture;
        }
        public static Texture2D ScreenRectToTexture(Rect rect)
        {
            Texture2D texture = new Texture2D((int)rect.width, (int)rect.height);
            texture.ReadPixels(rect, 0, 0);
            texture.Apply();
            return texture;
        }
        public static void SaveToPNG(this Texture2D texture, string path)
        {
            byte[] pngBytes = texture.EncodeToPNG();
            File.WriteAllBytes(path, pngBytes);
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

            return localPath.ToPath();
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

            return localPath.ToPath();
        }
        public static string ToPath(this string path)
        {
            string result = path;
            result = result.Replace('\\', Path.DirectorySeparatorChar);
            result = result.Replace('/', Path.DirectorySeparatorChar);
            return result;
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
}