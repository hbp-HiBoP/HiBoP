using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NewTheme
{
    [CreateAssetMenu(menuName = "Theme/Settings/Color")]
    public class ColorGraphic : Settings
    {
        public Color Color;

        public override void Set(GameObject gameObject)
        {
            UnityEngine.UI.Image image = gameObject.GetComponent<UnityEngine.UI.Image>();
            if (image)
            {
                image.color = Color;
            }
        }
    }
}