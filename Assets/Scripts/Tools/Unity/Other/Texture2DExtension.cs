using UnityEngine;
using System.IO;

namespace Tools.Unity
{
    public static class Texture2DExtension
    {
        public static bool LoadPNG(this Texture2D texture, string filePath)
        {
            if(filePath != string.Empty)
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

        public static Texture2D RotateTexture(Texture2D textureToRotate)
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
}