using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Experience.Protocol
{
    /// <summary>
    /// Component to display treatment in list.
    /// </summary>
    public class TreatmentItem : Tools.Unity.Lists.ActionnableItem<Core.Data.Treatment>
    {
        #region Properties
        [SerializeField] Text m_TypeText;

        [SerializeField] Text m_StartWindowText;
        [SerializeField] Text m_EndWindowText;

        [SerializeField] Text m_OrderText;

        /// <summary>
        /// Object to display.
        /// </summary>
        public override Core.Data.Treatment Object
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
                m_TypeText.text = (value.GetType().GetCustomAttributes(typeof(DisplayNameAttribute), false)[0] as DisplayNameAttribute).DisplayName;
            }
        }
        #endregion
    }
}