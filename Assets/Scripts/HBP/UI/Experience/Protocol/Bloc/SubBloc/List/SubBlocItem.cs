using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Experience.Protocol;


namespace HBP.UI.Experience.Protocol
{
    public class SubBlocItem : Tools.Unity.Lists.ActionnableItem<SubBloc>
    {
        #region Properties
        [SerializeField] Text m_NameText;

        [SerializeField] Text m_StartWindowText;
        [SerializeField] Text m_EndWindowText;

        [SerializeField] Text m_EventsText;
        [SerializeField] Text m_IconsText;
        [SerializeField] Text m_TreatmentsText;

        [SerializeField] Text m_OrderText;

        public override SubBloc Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_NameText.text = value.Name;

                m_StartWindowText.text = value.Window.Start.ToString();
                m_EndWindowText.text = value.Window.End.ToString();

                m_OrderText.text = value.Order.ToString();
            }
        }
        #endregion
    }
}