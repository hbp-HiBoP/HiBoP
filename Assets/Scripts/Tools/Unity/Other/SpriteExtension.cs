using System.IO;
using System.Linq;
using UnityEngine;
using System.Drawing;

namespace Tools.Unity
{
    public static class SpriteExtension
    {
        static string[] EXTENSIONS = new string[] { "png", "jpg" };

        public static bool LoadSpriteFromFile(out Sprite sprite, string path)
        {
            sprite = Sprite.Create(new Texture2D(1,1),new Rect(), new Vector2());
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
                    //byte[] bytes = File.ReadAllBytes(path);
                    //System.Drawing
                    //Image image = Image.FromFile(path);
                    //texture = new Texture2D(image.Width, image.Height);
                    //image.Dispose();
                    //return texture.LoadImage(bytes);
                    return false;
                    // TODO.
                }
                else return false;
            }
            else return false;
        }

        public static bool IsFileLoadable(string path)
        {
            return File.Exists(path) && EXTENSIONS.Contains(new FileInfo(path).Extension.Substring(1));
        }
    }
}
