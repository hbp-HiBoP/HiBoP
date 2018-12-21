using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Experience.Protocol;
using NewTheme.Components;

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
        [SerializeField] Text m_TypeText;

        [SerializeField] State m_ErrorState;

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

                int nbEvents = value.Events.Count;
                m_EventsText.text = nbEvents.ToString();
                if (nbEvents == 0) m_EventsText.GetComponent<ThemeElement>().Set(m_ErrorState);
                else m_EventsText.GetComponent<ThemeElement>().Set();

                int nbIcons = value.Icons.Count;
                m_IconsText.text = nbIcons.ToString();
                if (nbIcons == 0) m_IconsText.GetComponent<ThemeElement>().Set(m_ErrorState);
                else m_IconsText.GetComponent<ThemeElement>().Set();

                int nbTreatments = value.Treatments.Count;
                m_TreatmentsText.text = nbTreatments.ToString();
                if (nbTreatments == 0) m_TreatmentsText.GetComponent<ThemeElement>().Set(m_ErrorState);
                else m_TreatmentsText.GetComponent<ThemeElement>().Set();

                m_OrderText.text = value.Order.ToString();

                switch (m_Object.Type)
                {
                    case Data.Enums.MainSecondaryEnum.Main: m_TypeText.text = "M"; break;
                    case Data.Enums.MainSecondaryEnum.Secondary: m_TypeText.text = "S"; break;
                }
            }
        }
        #endregion
    }
}