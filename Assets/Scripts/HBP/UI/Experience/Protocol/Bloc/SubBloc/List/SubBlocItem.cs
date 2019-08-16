using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Experience.Protocol;
using NewTheme.Components;
using System.Text;
using System.Linq;
using Tools.Unity;

namespace HBP.UI.Experience.Protocol
{
    public class SubBlocItem : Tools.Unity.Lists.ActionnableItem<SubBloc>
    {
        #region Properties
        [SerializeField] Text m_NameText;

        [SerializeField] Text m_StartWindowText;
        [SerializeField] Text m_EndWindowText;

        [SerializeField] Text m_EventsText;
        [SerializeField] Tooltip m_EventsTooltip;

        [SerializeField] Text m_IconsText;
        [SerializeField] Tooltip m_IconsTooltip;

        [SerializeField] Text m_TreatmentsText;
        [SerializeField] Tooltip m_TreatmentsTooltip;

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

                m_StartWindowText.text = value.Window.Start.ToString() + "ms";
                m_EndWindowText.text = value.Window.End.ToString() + "ms";


                // Events.
                StringBuilder stringBuilder = new StringBuilder();
                string[] events = value.Events.Select(s => s.Name).ToArray();
                stringBuilder.AppendLine("Events :");
                for (int i = 0; i < events.Length; i++)
                {
                    if (i < events.Length - 1) stringBuilder.AppendLine("  \u2022 " + events[i]);
                    else stringBuilder.Append("  \u2022 " + events[i]);
                }
                if (events.Length == 0)
                {
                    m_EventsText.GetComponent<ThemeElement>().Set(m_ErrorState);
                    stringBuilder .Append("  \u2022 None ");
                }
                else
                {
                    m_EventsText.GetComponent<ThemeElement>().Set();
                }
                m_EventsTooltip.Text = stringBuilder.ToString();
                m_EventsText.text = events.Length.ToString();

                // Icons.
                stringBuilder = new StringBuilder();
                string[] icons = value.Icons.Select(s => s.Name).ToArray();
                stringBuilder.AppendLine("Icons :");
                for (int i = 0; i < icons.Length; i++)
                {
                    if (i < icons.Length - 1) stringBuilder.AppendLine("  \u2022 " + icons[i]);
                    else stringBuilder.Append("  \u2022 " + icons[i]);
                }
                if (icons.Length == 0)
                {
                    m_IconsText.GetComponent<ThemeElement>().Set(m_ErrorState);
                    stringBuilder.Append("  \u2022 None ");
                }
                else
                {
                    m_IconsText.GetComponent<ThemeElement>().Set();
                }
                m_IconsTooltip.Text = stringBuilder.ToString();
                m_IconsText.text = icons.Length.ToString();

                // Treatments.
                stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("Treatments :");
                string[] treatments = value.Treatments.Select(s => s.GetType().ToString()).ToArray();
                for (int i = 0; i < treatments.Length; i++)
                {
                    if (i < treatments.Length - 1) stringBuilder.AppendLine("  \u2022 " + treatments[i]);
                    else stringBuilder.Append("  \u2022 " + treatments[i]);
                }
                if (treatments.Length == 0)
                {
                    m_TreatmentsText.GetComponent<ThemeElement>().Set(m_ErrorState);
                    stringBuilder.Append("  \u2022 None");
                }
                else
                {
                    m_TreatmentsText.GetComponent<ThemeElement>().Set();
                }
                m_TreatmentsTooltip.Text = stringBuilder.ToString();
                m_TreatmentsText.text = treatments.Length.ToString();

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