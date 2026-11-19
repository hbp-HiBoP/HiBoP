using UnityEngine;

namespace HBP.Theme
{
    /// <summary>Themes UI shadows and outlines using a shared color.</summary>
    [CreateAssetMenu(menuName = "Theme/Settings/Shadow")]
    public class Shadow : Settings
    {
        public Color Color;
        [Range(0, 1)] public float Alpha = 1;
        public Vector2 EffectDistance = new Vector2(1, -1);
        public bool UseGraphicAlpha = true;

        public override void Set(GameObject gameObject)
        {
            var shadow = gameObject.GetComponent<UnityEngine.UI.Shadow>();
            if (shadow && Color)
            {
                var color = Color.Value;
                color.a = Alpha;
                shadow.effectColor = color;
                shadow.effectDistance = EffectDistance;
                shadow.useGraphicAlpha = UseGraphicAlpha;
            }
        }
    }
}
