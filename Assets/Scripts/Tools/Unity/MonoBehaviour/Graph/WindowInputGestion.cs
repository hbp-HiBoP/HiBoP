using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Graph
{
    public class WindowInputGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        InputField m_xmin;
        [SerializeField]
        InputField m_xmax;
        [SerializeField]
        InputField m_ymin;
        [SerializeField]
        InputField m_ymax;
        [SerializeField]
        DisplayGestion m_display;
        #endregion

        #region Public Methods
        public void OnChange()
        {
            m_display.ChangeWindowSize(new Vector2(float.Parse(m_xmin.text), float.Parse(m_xmax.text)), new Vector2(float.Parse(m_ymin.text), float.Parse(m_ymax.text)));
        }

        public void SetFields(Vector2 x,Vector2 y)
        {
            m_xmin.text = Mathf.RoundToInt(x.x).ToString();
            m_xmax.text = Mathf.RoundToInt(x.y).ToString();
            m_ymin.text = Mathf.RoundToInt(y.x).ToString();
            m_ymax.text = Mathf.RoundToInt(y.y).ToString();
        }
        #endregion
    }
}