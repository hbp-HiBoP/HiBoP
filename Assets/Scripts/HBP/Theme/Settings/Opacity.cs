using UnityEngine;

namespace HBP.Theme
{
    /// <summary>Applies opacity after the shared graphic color setting.</summary>
    [CreateAssetMenu(menuName = "Theme/Settings/Opacity")]
    public class Opacity : Settings
    {
        [Range(0, 1)] public float Alpha = 1;

        public override void Set(GameObject gameObject)
        {
            var graphic = gameObject.GetComponent<UnityEngine.UI.Graphic>();
            if (graphic)
            {
                var color = graphic.color;
                color.a = Alpha;
                graphic.color = color;
            }
        }
    }
}
