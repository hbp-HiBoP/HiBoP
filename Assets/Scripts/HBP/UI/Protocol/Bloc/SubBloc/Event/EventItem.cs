using UnityEngine;
using UnityEngine.UI;
using Tools.Unity.Lists;
using NewTheme.Components;
using Tools.Unity;
using System.Text;

namespace HBP.UI.Experience.Protocol
{
	public class EventItem : ActionnableItem<Data.Experience.Protocol.Event> 
	{
		#region Properties
		[SerializeField] Text m_NameText;

        [SerializeField] Text m_CodeText;
        [SerializeField] Tooltip m_CodeTooltip;

        [SerializeField] Text m_TypeText;
        [SerializeField] State m_ErrorState;

        public override Data.Experience.Protocol.Event Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;

                m_NameText.text = m_Object.Name;

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("Codes: ");
                int[] codes = value.Codes.ToArray();
                for (int i = 0; i < codes.Length; i++)
                {
                    if (i < codes.Length - 1) stringBuilder.AppendLine("  \u2022 " + codes[i]);
                    else stringBuilder.Append("  \u2022 " + codes[i]);
                }
                if (codes.Length == 0)
                {
                    m_CodeText.GetComponent<ThemeElement>().Set(m_ErrorState);
                    stringBuilder.Append("  \u2022 None");
                }
                else m_CodeText.GetComponent<ThemeElement>().Set();
                m_CodeTooltip.Text = stringBuilder.ToString();
                m_CodeText.text = codes.Length.ToString();

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