using UnityEngine;

namespace HBP.Theme
{
    [CreateAssetMenu(menuName = "Theme/Settings/Color")]
    public class Color : Settings
    {
        public UnityEngine.Color Value;

        public override void Set(GameObject gameObject)
        {
            UnityEngine.UI.Graphic graphic = gameObject.GetComponent<UnityEngine.UI.Graphic>();
            if (graphic)
            {
                graphic.color = Value;
            }
        }
    }
}