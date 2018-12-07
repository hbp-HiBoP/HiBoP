using d = HBP.Data.Experience.Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.TrialMatrix.Grid
{
    public class BlocHeader : MonoBehaviour
    {
        #region Properties
        d.Bloc m_Bloc;
        public d.Bloc Bloc
        {
            get
            {
                return m_Bloc;
            }
            set
            {
                m_Bloc = value;
                m_Text.text = value.Name;
            }
        }
        public float FlexibleHeight
        {
            get
            {
                return m_LayoutElement.flexibleHeight;
            }
            set
            {
                m_LayoutElement.flexibleHeight = value;
            }
        }

        [SerializeField] Text m_Text;
        [SerializeField] LayoutElement m_LayoutElement;
        #endregion
    }
}