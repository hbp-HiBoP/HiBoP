using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Graph
{
    public class PositionMouseValueDisplayer : MonoBehaviour
    {
        #region Properties
        public Graph Graph;
        public RectTransform GraphRectTransform;
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
            //transform.position = Input.mousePosition;
            CheckSide();
        }

        private void UpdateValues()
        {
            Vector2 mousePosition = Input.mousePosition;
            Vector2 localPosition = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(GraphRectTransform, mousePosition, null, out localPosition);
            Vector2 ratioPosition = new Vector2(localPosition.x / GraphRectTransform.rect.width, localPosition.y / GraphRectTransform.rect.height) + GraphRectTransform.pivot;
            Limits limits = Graph.Limits;
            Vector2 values = new Vector2(limits.AbscissaMin + ratioPosition.x * (limits.AbscissaMax - limits.AbscissaMin), limits.OrdinateMin + ratioPosition.y * (limits.OrdinateMax - limits.OrdinateMin));
            m_Text.text = "(" + values.x.ToString("F2") + ", " + values.y.ToString("F2") + ")";
        }

        private void CheckSide()
        {
            Vector3[] corners = new Vector3[4];
            Vector3[] parentCorners = new Vector3[4];
            m_RectTransform.GetWorldCorners(corners);
            GraphRectTransform.GetWorldCorners(parentCorners);

            float botPosition = m_RectTransform.position.y - m_RectTransform.rect.height;
            float rightPosition = m_RectTransform.position.x + m_RectTransform.rect.width;

            if (botPosition > parentCorners[0].y && rightPosition < parentCorners[2].x)
            {
                m_RectTransform.pivot = new Vector2(0, 1);
                transform.position = Input.mousePosition + new Vector3(15,0,0);
            }
            else
            {
                if (corners[2].x > parentCorners[2].x)
                {
                    m_RectTransform.pivot = new Vector2(1, m_RectTransform.pivot.y);
                    transform.position = Input.mousePosition + new Vector3(-10, -10, 0);
                }
                else
                {
                    transform.position = Input.mousePosition + new Vector3(15, 0, 0);
                }
                if (corners[0].y < parentCorners[0].y)
                {
                    m_RectTransform.pivot = new Vector2(m_RectTransform.pivot.x, 0);
                    transform.position = Input.mousePosition + new Vector3(-10, -10, 0);
                }
                else
                {
                    transform.position = Input.mousePosition + new Vector3(15, 0, 0);
                }
            }
        }
        #endregion
    }
}