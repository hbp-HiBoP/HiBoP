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

        [SerializeField] GameObject m_ValuePrefab;
        #endregion

        #region Private Methods
        void SetValues(float[] values)
        {
            Clear();
            for (int i = 0; i < values.Length; i++)
            {
                AddValue(values[i], i, values.Length);
            }
        }
        void Clear()
        {
            foreach(Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
        void AddValue(float value, int position, int max)
        {
            // Instantiate and add components needed
            GameObject gameObject = Instantiate(m_ValuePrefab, transform);
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            Text text = gameObject.GetComponent<Text>();
            int maxPosition = max - 1;
            if( position == 0)
            {
                rectTransform.pivot = new Vector2(0, 0);
            }
            else if( position == maxPosition)
            {
                rectTransform.pivot = new Vector2(0, 1);
            }
            else
            {
                rectTransform.pivot = new Vector2(0, 0.5f);
            }

            // SetText
            text.text = value.ToString("n2");

            // SetPosition
            float l_position = (float)position / maxPosition;
            rectTransform.anchorMin = new Vector2(0, l_position);
            rectTransform.anchorMax = new Vector2(1, l_position);
            rectTransform.anchoredPosition = new Vector2(0, 0);
            rectTransform.sizeDelta = new Vector2(0, 25);
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