using UnityEngine;
using UnityEngine.Events;

public class TextureRotator : MonoBehaviour
{
    public Texture2D Texture2D
    {
        set
        {
            Texture2D texture = new Texture2D(value.height, value.width);
            //Color[] colors = new Color[texture.width * texture.height];
            //for (int x = 0; x < value.width; x++)
            //{
            //    for (int y = 0; y < value.height; y++)
            //    {
            //        colors[x * value.height + y] = value.GetPixel(x, y);
            //    }
            //}
            //texture.SetPixels(colors);
            //texture.Apply();
            //OnChangeTexture.Invoke(texture);
        }
    }
    public Texture2DEvent OnChangeTexture = new Texture2DEvent();

}
