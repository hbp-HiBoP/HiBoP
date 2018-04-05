using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.TrialMatrix
{
    public class ValuesLegend : MonoBehaviour
    {
        #region Properties
        public int NumberOfValues = 5;
        Vector2 m_Limits;
        public Vector2 Limits
        {
            get
            {
                return m_Limits;
            }
            set
            {
                m_Limits = value;
                SetValues(GenerateValues(value.x, value.y, NumberOfValues));
            }
        }
        #endregion

        #region Private Methods
        void SetValues(float[] values)
        {
            ClearValues();
            for (int i = 0; i < values.Length; i++)
            {
                AddValue(values[i], i, values.Length);
            }
        }
        void ClearValues()
        {
            foreach(Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
        void AddValue(float value, int position, int max)
        {
            // Instantiate and add components needed
            GameObject l_gameObject = new GameObject();
            l_gameObject.transform.SetParent(transform);
            RectTransform l_rect = l_gameObject.AddComponent<RectTransform>();
            Text l_text = l_gameObject.AddComponent<Text>();
            int l_max = max - 1;

            // SetText
            l_text.text = value.ToString("n2");
            l_text.color = Color.white;
            l_text.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
            l_text.fontStyle = FontStyle.Normal;
            l_text.fontSize = 14;
            l_text.horizontalOverflow = HorizontalWrapMode.Wrap;

            // Set pivot, text alignment and name
            if (position == 0)
            {
                l_rect.pivot = new Vector2(0.5f, 0f);
                l_text.alignment = TextAnchor.LowerLeft;
                l_gameObject.name = "Min";
            }
            else if (position == l_max)
            {
                l_rect.pivot = new Vector2(0.5f, 1f);
                l_text.alignment = TextAnchor.UpperLeft;
                l_gameObject.name = "Max";
            }
            else
            {
                l_text.alignment = TextAnchor.MiddleLeft;
                l_gameObject.name = position.ToString() + "/" + l_max.ToString();
            }

            // SetPosition
            float l_position = (float)position / l_max;
            l_rect.anchorMin = new Vector2(0, l_position);
            l_rect.anchorMax = new Vector2(1, l_position);
            l_rect.anchoredPosition = new Vector2(0, 0);
            l_rect.sizeDelta = new Vector2(0, 30);
        }
        float[] GenerateValues(float min, float max, int nbValue)
        {
            float l_size = max - min;
            float[] l_result = new float[nbValue];
            for (int i = 0; i < nbValue; i++)
            {
                l_result[i] = min + l_size *(i / (float)(nbValue-1));
            }
            return l_result;
        }
        #endregion
    }
}