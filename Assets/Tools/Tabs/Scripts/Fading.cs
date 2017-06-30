using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Fading : MonoBehaviour
{
    public void Fade(Graphic[] graphics, float alpha)
    {
        foreach (var graphic in graphics)
        {
            graphic.color = new Color(graphic.color.r, graphic.color.g, graphic.color.b, alpha);
        }
    }
    public IEnumerator Fade(Graphic[] graphics, float start, float end, float duration)
    {
        if(graphics.Length > 0)
        {
            float fading = start;
            float step = Mathf.Abs(start - end) / duration;
            if (start > end)
            {
                while (fading > end)
                {
                    fading -= step * Time.deltaTime;
                    foreach (var graphic in graphics)
                    {
                        graphic.color = new Color(graphic.color.r, graphic.color.g, graphic.color.b, fading);
                    }
                    yield return null;
                }
            }
            else
            {
                while (fading < end)
                {
                    fading += step * Time.deltaTime;
                    foreach (var graphic in graphics)
                    {
                        graphic.color = new Color(graphic.color.r, graphic.color.g, graphic.color.b, fading);
                    }
                    yield return null;
                }
            }
        }
        yield return null;
    }
}
