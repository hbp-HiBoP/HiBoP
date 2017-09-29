using System.IO;
using System.Linq;
using UnityEngine;

namespace Tools.Unity
{
    public static class SpriteExtension
    {
        static string[] EXTENSIONS = new string[] { "png", "jpg" };

        public static Sprite Load(string path)
        {
            Sprite result = null;
            if (!string.IsNullOrEmpty(path))
            {
                FileInfo fileInfo = new FileInfo(path);
                if (fileInfo.Exists && (EXTENSIONS.Contains(fileInfo.Extension.Substring(1))))
                {
                    byte[] bytes = File.ReadAllBytes(path);
                    Texture2D texture = new Texture2D(512, 512);
                    if (texture.LoadImage(bytes))
                    {
                        result = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    }
                }
            }
            return result;
        }
    }
}
