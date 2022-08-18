using HBP.Theme.Components;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using HBP.UI.Tools.Lists;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    /// <summary>
    /// Component to display dataset in list.
    /// </summary>
    public class DatasetItem : ActionnableItem<Core.Data.Dataset> 
	{
        #region Properties
        [SerializeField] Text m_NameText;
        [SerializeField] Text m_ProtocolText;
        [SerializeField] Text m_DataText;

        [SerializeField] Theme.State m_ErrorState;

        /// <summary>
        /// Object to display.
        /// </summary>
        public override Core.Data.Dataset Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;

                m_NameText.text = value.Name;
                m_ProtocolText.text = value.Protocol.Name;
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("Data : ");
                Core.Data.DataInfo[] data = value.Data.Where(s => s.IsOk).ToArray();
                string[] names = data.Select(d => d.Name).Distinct().ToArray();
                for (int i = 0; i < names.Length; i++)
                {
                    int nbData = data.Count(d => d.Name == names[i]);
                    string text = string.Format("  \u2022 {0} {1}", nbData, names[i]);
                    if (i < names.Length - 1) stringBuilder.AppendLine(text);
                    else stringBuilder.Append(text);
                }
                if (data.Length == 0)
                {
                    m_DataText.GetComponent<ThemeElement>().Set(m_ErrorState);
                    stringBuilder.Append("  \u2022 None");
                }
                else
                {
                    m_DataText.GetComponent<ThemeElement>().Set();
                }
                m_DataText.GetComponent<Tooltip>().Text = stringBuilder.ToString();
                m_DataText.text = data.Length.ToString();
            }
        }
        #endregion
    }
}