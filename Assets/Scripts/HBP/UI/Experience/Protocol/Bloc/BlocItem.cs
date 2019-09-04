using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Experience.Protocol;
using Tools.Unity;
using System.Text;
using System.Linq;
using NewTheme.Components;

namespace HBP.UI.Experience.Protocol
{
	public class BlocItem : Tools.Unity.Lists.ActionnableItem<Bloc>
	{
        #region Properties
        [SerializeField] Text m_NameText;

        [SerializeField] Image m_Image;
        [SerializeField] Tooltip m_ImageTooltip;

        [SerializeField] Text m_SubBlocsText;
        [SerializeField] Tooltip m_SubBlocsTooltip;

        [SerializeField] Text m_OrderText;

        [SerializeField] State m_ErrorState;

        public override Bloc Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_NameText.text = value.Name;
                m_OrderText.text = value.Order.ToString();

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("SubBlocs: ");
                string[] subBlocs = value.OrderedSubBlocs.Select(s => s.Name).ToArray();
                for (int i = 0; i < subBlocs.Length; i++)
                {
                    if (i < subBlocs.Length - 1) stringBuilder.AppendLine("  \u2022 " + subBlocs[i]);
                    else stringBuilder.Append("  \u2022 " + subBlocs[i]);
                }
                if (subBlocs.Length == 0)
                {
                    m_SubBlocsText.GetComponent<ThemeElement>().Set(m_ErrorState);
                    stringBuilder.Append("  \u2022 None");
                }
                else
                {
                    m_SubBlocsText.GetComponent<ThemeElement>().Set();
                }
                m_SubBlocsTooltip.Text = stringBuilder.ToString();
                m_SubBlocsText.text = subBlocs.Length.ToString();

                m_Image.overrideSprite = value.Image;
                m_ImageTooltip.Text = "";
                m_ImageTooltip.Image = value.Image;
            }
        }
        #endregion
    }
}