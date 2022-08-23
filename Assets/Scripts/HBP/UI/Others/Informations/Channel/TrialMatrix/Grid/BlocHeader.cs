using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Informations.TrialMatrix.Grid
{
    public class BlocHeader : MonoBehaviour
    {
        #region Properties
        Core.Data.Bloc m_Bloc;
        public Core.Data.Bloc Bloc
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