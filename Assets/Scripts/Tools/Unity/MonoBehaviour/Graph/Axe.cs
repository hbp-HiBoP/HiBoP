using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Graph
{
    public class Axe : MonoBehaviour
    {
        #region Attributs
        [SerializeField]
        GameObject m_scalePoint;

        #region Parameters
        public enum SideEnum { absciss, ordinate };
        public SideEnum Side;

        Color m_color;
        public Color Color { get { return m_color; } set { SetColor(value); } }

        int m_fontSize = 0;
        #endregion

        #endregion

        #region Public Methods

        public void Set(Color color, float min, float max)
        {
            // Clear the axe
            Clear();

            // Set the color
            Color = color;

            // Calculate the value of the axe.
            float l_ratio=0, l_step=0, l_startIndex=0, l_value=0, l_position=0; int l_nbScalePoint=0;
            CalculateAxeValue(max, min, out l_ratio, out l_step, out l_nbScalePoint, out l_startIndex);

            //// Add the scalesPoints
            for (int i = 0; i < l_nbScalePoint; i++)
            {
                l_value = (l_startIndex + i) * l_step;
                l_position = (l_value - min) * l_ratio;
                AddScalePoint(l_value.ToString(), m_fontSize, color, l_position);
            }
        }

        #endregion

        #region Private Methods

        void AddScalePoint(string label, int fontSize, Color color, float position)
        {
            GameObject l_scalePoint = Instantiate(m_scalePoint);
            l_scalePoint.transform.SetParent(transform.GetChild(0));
            l_scalePoint.GetComponent<Graduation>().Set(label, fontSize, color, position, Side);
        }

        void Clear()
        {
            foreach (Transform child in transform.GetChild(0))
            {
                Destroy(child.gameObject);
            }
        }

        void CalculateAxeValue(float max, float min, out float l_ratio, out float l_step, out int l_nbScalePoint, out float l_startIndex)
        {
            // Calculate the range of the axe.
            float l_size = max - min;

            // Calculate the normalized range(1-10) of the axe.
            float l_normalizedSize = l_size;
            float l_coefficient = 1f;
            if(l_normalizedSize > 0)
            {
                while (l_normalizedSize >= 10.0f)
                {
                    l_coefficient *= 10.0f;
                    l_normalizedSize /= 10.0f;
                    break;
                }
                while (l_normalizedSize < 1.0f)
                {
                    l_coefficient /= 10.0f;
                    l_normalizedSize *= 10.0f;
                    break;
                }
                // Calculate the normalizedStep then the Step.
                float l_normalizedStep = l_normalizedSize / 10.0f;
                l_normalizedStep = (Mathf.Ceil(l_normalizedStep * 2.0f)) / 2.0f;
                l_step = l_normalizedStep * l_coefficient;

                // Calculate the firstScalePoint of the axe
                if (min < 0.0f)
                {
                    l_startIndex = Mathf.CeilToInt(min / l_step);
                }
                else
                {
                    l_startIndex = Mathf.CeilToInt(min / l_step);
                }

                // Calculate the number of ScalePoint in the axe
                l_nbScalePoint = 0;
                while ((l_nbScalePoint + l_startIndex) * l_step <= max)
                {
                    l_nbScalePoint += 1;
                }

                float l_axeSize = 0;
                switch (Side)
                {
                    case SideEnum.absciss: l_axeSize = transform.GetChild(0).GetComponent<RectTransform>().rect.width; break;
                    case SideEnum.ordinate: l_axeSize = transform.GetChild(0).GetComponent<RectTransform>().rect.height; break;
                }
                // Find the value of the scalesPoints
                l_ratio = l_axeSize / l_size;
            }
            else
            {
                l_ratio = 0;
                l_step = 0;
                l_nbScalePoint = 0;
                l_startIndex = 0;
            }
        }

        #region Getter/Setter

        void SetColor(Color color)
        {
            m_color = color;
            foreach (Transform scalePoint in transform.GetChild(0))
            {
                scalePoint.GetComponent<Graduation>().Color = color;
            }
            transform.GetChild(1).GetComponent<Image>().color = color;
            transform.GetChild(1).GetChild(0).GetComponent<Image>().color = color;
        }

        #endregion

        #endregion
    }
}

