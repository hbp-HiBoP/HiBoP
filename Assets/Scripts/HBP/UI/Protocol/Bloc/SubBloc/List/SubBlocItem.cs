using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.ComponentModel;
using HBP.Core.Enums;

namespace HBP.UI.Experience.Protocol
{
    /// <summary>
    /// Component to display subBloc in list.
    /// </summary>
    public class SubBlocItem : Tools.Unity.Lists.ActionnableItem<Core.Data.SubBloc>
    {
        #region Properties
        [SerializeField] Text m_NameText;

        [SerializeField] Text m_StartWindowText;
        [SerializeField] Text m_EndWindowText;

        [SerializeField] Text m_EventsText;
        [SerializeField] Text m_IconsText;
        [SerializeField] Text m_TreatmentsText;

        [SerializeField] Text m_OrderText;
        [SerializeField] Text m_TypeText;

        [SerializeField] HBP.Theme.State m_ErrorState;

        /// <summary>
        /// Object to display.
        /// </summary>
        public override Core.Data.SubBloc Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_NameText.text = value.Name;

                m_StartWindowText.text = value.Window.Start.ToString() + "ms";
                m_EndWindowText.text = value.Window.End.ToString() + "ms";

                m_EventsText.SetIEnumerableFieldInItem("Events", from e in m_Object.Events where e.IsVisualizable select e.Name, m_ErrorState);
                m_IconsText.SetIEnumerableFieldInItem("Icons", from i in m_Object.Icons select i.Name, m_ErrorState);
                m_TreatmentsText.SetIEnumerableFieldInItem("Treatments", m_Object.Treatments.Select(t => string.Format("{0} {1}ms to {2}ms",(t.GetType().GetCustomAttributes(typeof(DisplayNameAttribute), false)[0] as DisplayNameAttribute).DisplayName,t.Window.Start,t.Window.End)), m_ErrorState);

                m_OrderText.text = value.Order.ToString();

                switch (m_Object.Type)
                {
                    case MainSecondaryEnum.Main: m_TypeText.text = "M"; break;
                    case MainSecondaryEnum.Secondary: m_TypeText.text = "S"; break;
                }
            }
        }
        #endregion
    }
}