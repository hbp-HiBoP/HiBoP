using UnityEngine;
using UnityEngine.UI;
using Tools.Unity.Lists;
using HBP.Theme.Components;
using Tools.Unity;
using System.Text;
using HBP.Core.Enums;

namespace HBP.UI.Experience.Protocol
{
    /// <summary>
    /// Component to display event in list.
    /// </summary>
	public class EventItem : ActionnableItem<Core.Data.Event> 
	{
		#region Properties
		[SerializeField] Text m_NameText;

        [SerializeField] Text m_CodeText;
        [SerializeField] Tooltip m_CodeTooltip;

        [SerializeField] Text m_TypeText;
        [SerializeField] HBP.Theme.State m_ErrorState;

        /// <summary>
        /// Object to display.
        /// </summary>
        public override Core.Data.Event Object
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
                    case MainSecondaryEnum.Main: m_TypeText.text = "M"; break;
                    case MainSecondaryEnum.Secondary: m_TypeText.text = "S"; break;
                }
            }
        }
        #endregion
    }
}