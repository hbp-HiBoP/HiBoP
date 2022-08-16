using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Graphs
{
    public class PositionMouseValueDisplayer : MonoBehaviour
    {
        #region Properties
        public Graph Graph;
        public RectTransform GraphRectTransform;

        public Vector2 TopLeft;
        public Vector2 TopRight;
        public Vector2 BotLeft;
        public Vector2 BotRight;

        RectTransform m_RectTransform;
        Text m_Text;
        #endregion

        #region Public Methods
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
        #endregion

        #region Private Methods
        private void Start()
        {
            m_Text = GetComponent<Text>();
            m_RectTransform = GetComponent<RectTransform>();
        }
        private void Update()
        {
            UpdatePosition();
            UpdateValues();
        }

        private void UpdatePosition()
        {
            Vector3[] parentCorners = new Vector3[4];
            GraphRectTransform.GetWorldCorners(parentCorners);


            Vector3 mousePosition = Input.mousePosition;
            float RightLimit = mousePosition.x + Mathf.Max(TopRight.x,BotRight.x) + m_RectTransform.rect.width;
            float TopLimit = mousePosition.y + Mathf.Max(TopRight.y, TopLeft.y) + m_RectTransform.rect.height;

            bool isTop = TopLimit < parentCorners[2].y;
            bool isRight = RightLimit < parentCorners[2].x;
            if(isTop)
            {
                if(isRight)
                {
                    // Top Right.
                    m_RectTransform.pivot = new Vector2(0, 0);
                    m_RectTransform.position = mousePosition + (Vector3) TopRight;
                }
                else
                {
                    // Top Left.
                    m_RectTransform.pivot = new Vector2(1, 0);
                    m_RectTransform.position = mousePosition + (Vector3) TopLeft;
                }
            }
            else
            {
                if (isRight)
                {
                    // Bot Right.
                    m_RectTransform.pivot = new Vector2(0, 1);
                    m_RectTransform.position = mousePosition + (Vector3) BotRight;
                }
                else
                {
                    // Bot Left.
                    m_RectTransform.pivot = new Vector2(1, 1);
                    m_RectTransform.position = mousePosition + (Vector3) BotLeft;
                }
            }
        }

        private void UpdateValues()
        {
            Vector2 mousePosition = Input.mousePosition;
            Vector2 localPosition = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(GraphRectTransform, mousePosition, null, out localPosition);
            Vector2 ratioPosition = new Vector2(localPosition.x / GraphRectTransform.rect.width, localPosition.y / GraphRectTransform.rect.height) + GraphRectTransform.pivot;
            Vector2 ordinateDisplayRange = Graph.OrdinateDisplayRange;
            Vector2 abscissaDisplayRange = Graph.AbscissaDisplayRange;

            Vector2 values = new Vector2(abscissaDisplayRange.x + ratioPosition.x * (abscissaDisplayRange.y - abscissaDisplayRange.x), ordinateDisplayRange.x + ratioPosition.y * (ordinateDisplayRange.y - ordinateDisplayRange.x));
            m_Text.text = "(" + values.x.ToString("F2") + ", " + values.y.ToString("F2") + ")";
        }
        #endregion
    }
}