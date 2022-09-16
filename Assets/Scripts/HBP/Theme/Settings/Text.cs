using UnityEngine;

namespace HBP.Theme
{
    [CreateAssetMenu(menuName = "Theme/Settings/Text")]
    public class Text : Settings
    {
        #region Properties
        // Character.
        public Font Font;
        public FontStyle FontStyle;
        public int FontSize;
        public int LineSpacing;
        public bool RichText;

        // Paragraph.
        //public TextAnchor Alignment;
        //public bool AlignByGeometry;
        //public HorizontalWrapMode HorizontalOverflow;
        //public VerticalWrapMode VerticalOverflow;
        //public bool BestFit;
        //public Color Color;
        public Material Material;
        #endregion

        #region Public Methods
        public override void Set(GameObject gameObject)
        {
            
            UnityEngine.UI.Text text = gameObject.GetComponent<UnityEngine.UI.Text>();
            if(text)
            {
                text.font = Font;
                text.fontStyle = FontStyle;
                text.fontSize = FontSize;
                text.lineSpacing = LineSpacing;
                text.supportRichText = RichText;
                //text.alignment = Alignment;
                //text.alignByGeometry = AlignByGeometry;
                //text.horizontalOverflow = HorizontalOverflow;
                //text.verticalOverflow = VerticalOverflow;
                //text.resizeTextForBestFit = BestFit;
                //text.color = Color.Value;
                text.material = Material;
            }
        }
        #endregion
    }
}