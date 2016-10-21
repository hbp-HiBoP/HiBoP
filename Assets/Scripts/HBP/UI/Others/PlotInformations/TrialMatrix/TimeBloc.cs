using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.TrialMatrix
{
    public class TimeBloc : MonoBehaviour
    {
        #region Properties
        Text m_min;
        Text m_max;
        #endregion

        #region Public Methods
        public void Set(float min, float max)
        {
            SetValues(min, max);
        }
        #endregion

        #region Private Methods
        void SetValues(float min, float max)
        {
            m_min.text = min.ToString() + " (ms)";
            m_max.text = max.ToString() + " (ms)";
        }

        void Awake()
        {
            m_min = transform.GetChild(0).FindChild("Min").GetComponent<Text>();
            m_max = transform.GetChild(0).FindChild("Max").GetComponent<Text>();
        }
        #endregion
    }
}
