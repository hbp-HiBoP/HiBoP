using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.TrialMatrix
{
    public class TimeBloc : MonoBehaviour
    {
        #region Properties
        [SerializeField] Text m_StartText;
        [SerializeField] Text m_EndText;
        #endregion

        #region Public Methods
        public void Set(float start, float end)
        {
            m_StartText.text = start.ToString() + " (ms)";
            m_EndText.text = end.ToString() + " (ms)";
        }
        #endregion
    }
}
