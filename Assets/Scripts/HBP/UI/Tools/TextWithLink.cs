using HBP.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HBP.UI.Tools
{
    [RequireComponent(typeof(TMP_Text)), RequireComponent(typeof(ThemeElement))]
    public class TextWithLink : MonoBehaviour, IPointerClickHandler, IPointerMoveHandler
    {
        #region Properties
        private TMP_Text m_TextWithLink;
        private string m_OriginalText;
        private string m_BaseHexColor;
        private string m_HighlightHexColor;

        private ThemeElement m_ThemeElement;
        [SerializeField] State m_DefaultState;
        [SerializeField] State m_HoverState;
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_TextWithLink = GetComponent<TMP_Text>();
            m_ThemeElement = GetComponent<ThemeElement>();
        }
        private void Start()
        {
            m_OriginalText = m_TextWithLink.text;
            string colorTag = "<color=#([0-9A-F]{6})>";
            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(m_TextWithLink.text, colorTag);
            if (match.Success)
            {
                m_BaseHexColor = match.Groups[1].Value;
                // Set highlight color to a lighter version of the base color
                UnityEngine.Color baseColor = ColorUtility.TryParseHtmlString("#" + m_BaseHexColor, out UnityEngine.Color color) ? color : UnityEngine.Color.white;
                UnityEngine.Color highlightColor = baseColor * 1.2f;
                m_HighlightHexColor = ColorUtility.ToHtmlStringRGB(highlightColor);
            }
        }
        #endregion

        #region Public Methods
        public void OnPointerClick(PointerEventData eventData)
        {
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(m_TextWithLink, eventData.position, null);
            if (linkIndex != -1)
            {
                TMP_LinkInfo linkInfo = m_TextWithLink.textInfo.linkInfo[linkIndex];
                Application.OpenURL(linkInfo.GetLinkID());
            }
        }
        public void OnPointerMove(PointerEventData eventData)
        {
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(m_TextWithLink, eventData.position, null);
            if (linkIndex != -1)
            {
                m_TextWithLink.text = m_OriginalText.Replace(m_BaseHexColor, m_HighlightHexColor);
                m_ThemeElement.Set(m_HoverState);
            }
            else
            {
                m_TextWithLink.text = m_OriginalText;
                m_ThemeElement.Set(m_DefaultState);
            }
            m_TextWithLink.ForceMeshUpdate();
        }
        #endregion
    }
}