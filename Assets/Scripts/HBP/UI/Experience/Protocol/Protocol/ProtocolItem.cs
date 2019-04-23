using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Protocol;
using Tools.Unity.Lists;
using NewTheme.Components;
using System.Linq;
using Tools.Unity;
using System.Text;

namespace HBP.UI.Experience.Protocol
{
	public class ProtocolItem : ActionnableItem<d.Protocol> 
	{
		#region Properties
		[SerializeField] Text m_NameText;

        [SerializeField] Text m_BlocsText;
        [SerializeField] Tooltip m_BlocsTooltip;

        [SerializeField] State m_ErrorState;

        public override d.Protocol Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_NameText.text = value.Name;

                StringBuilder stringBuilder = new StringBuilder();
                string[] blocs = value.Blocs.Select(b => b.Name).ToArray();
                for (int i = 0; i < blocs.Length; i++)
                {
                    if (i < blocs.Length - 1) stringBuilder.AppendLine(" \u2022 " + blocs[i]);
                    else stringBuilder.Append(" \u2022 " + blocs[i]);
                }
                if (blocs.Length == 0)
                {
                    m_BlocsText.GetComponent<ThemeElement>().Set(m_ErrorState);
                    m_BlocsTooltip.Text = " \u2022 None";
                }
                else
                {
                    m_BlocsText.GetComponent<ThemeElement>().Set();
                    m_BlocsTooltip.Text = stringBuilder.ToString();
                }
                m_BlocsText.text = blocs.Length.ToString();
            }
        }
        #endregion
    }
}
