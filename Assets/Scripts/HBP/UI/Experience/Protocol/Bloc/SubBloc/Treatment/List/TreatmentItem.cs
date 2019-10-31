using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Protocol;

namespace HBP.UI.Experience.Protocol
{
    public class TreatmentItem : Tools.Unity.Lists.ActionnableItem<d.Treatment>
    {
        #region Properties
        [SerializeField] Text m_TypeText;

        [SerializeField] Text m_StartWindowText;
        [SerializeField] Text m_EndWindowText;

        [SerializeField] Text m_OrderText;

        public override d.Treatment Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;

                m_StartWindowText.text = value.Window.Start.ToString() + "ms";
                m_EndWindowText.text = value.Window.End.ToString() + "ms";

                m_OrderText.text = value.Order.ToString();
                m_TypeText.text = value.GetType().Name;
            }
        }
        #endregion
    }
}