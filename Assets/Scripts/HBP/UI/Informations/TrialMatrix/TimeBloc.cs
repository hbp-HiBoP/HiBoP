using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Informations.TrialMatrix
{
    [RequireComponent(typeof(LayoutElement))]
    public class TimeBloc : MonoBehaviour
    {
        #region Properties
        [SerializeField] Text m_StartText;
        [SerializeField] Text m_EndText;
        [SerializeField] LayoutElement m_LayoutElement;
        #endregion

        #region Public Methods
        public void Set(int start, int end)
        {
            m_StartText.text = start.ToString();
            m_EndText.text = end.ToString();
            m_LayoutElement.flexibleWidth = end - start;
        }
        #endregion
    }
}
