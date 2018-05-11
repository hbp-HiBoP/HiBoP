using UnityEngine;

namespace NewTheme
{
    [System.Serializable]
    public class ColorBlock
    {
        public Color NormalColor;
        public Color HighlightedColor;
        public Color PressedColor;
        public Color DisabledColor;
        [Range(1, 5)] public float ColorMultiplier;
        public float FadeDuration;

        public UnityEngine.UI.ColorBlock ToUnityColorBlock()
        {
            UnityEngine.UI.ColorBlock result = new UnityEngine.UI.ColorBlock();
            result.normalColor = NormalColor.Value;
            result.highlightedColor = HighlightedColor.Value;
            result.pressedColor = PressedColor.Value;
            result.disabledColor = DisabledColor.Value;
            result.colorMultiplier = ColorMultiplier;
            result.fadeDuration = FadeDuration;
            return result;
        }
    }
}