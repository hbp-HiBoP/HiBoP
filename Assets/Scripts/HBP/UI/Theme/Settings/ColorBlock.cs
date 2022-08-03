using UnityEngine;

namespace HBP.Theme
{
    [CreateAssetMenu(menuName = "Theme/Settings/Color Block")]
    public class ColorBlock : Settings
    {
        public UnityEngine.Color NormalColor;
        public UnityEngine.Color HighlightedColor;
        public UnityEngine.Color PressedColor;
        public UnityEngine.Color DisabledColor;
        [Range(1, 5)] public float ColorMultiplier;
        public float FadeDuration;
        public UnityEngine.UI.ColorBlock Colors
        {
            get
            {
                UnityEngine.UI.ColorBlock result = new UnityEngine.UI.ColorBlock();
                result.normalColor = NormalColor;
                result.highlightedColor = HighlightedColor;
                result.selectedColor = HighlightedColor;
                result.pressedColor = PressedColor;
                result.disabledColor = DisabledColor;
                result.colorMultiplier = ColorMultiplier;
                result.fadeDuration = FadeDuration;
                return result;
            }
        }

        public override void Set(GameObject gameObject)
        {
            UnityEngine.UI.Selectable selectable = gameObject.GetComponent<UnityEngine.UI.Selectable>();
            if (selectable)
            {
                selectable.colors = Colors;
            }
        }
    }
}