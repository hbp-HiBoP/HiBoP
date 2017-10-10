using UnityEngine;
using UnityEngine.UI;
using Tools.Unity.Lists;
using System.Linq;

namespace HBP.UI.Experience.Protocol
{
	public class EventItem : Tools.Unity.Lists.ActionnableItem<Data.Experience.Protocol.Event> 
	{
		#region Attributs
		[SerializeField] Text m_NameText;
        [SerializeField] Text m_TypeText;

        [SerializeField] Text m_CodeText;
        [SerializeField] Button m_CodeButton;
        [SerializeField] LabelList m_CodeList;
        
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
                int nbCode = m_Object.Codes.Count;
                m_CodeText.text = m_Object.Codes.Count.ToString();
                m_CodeList.Objects = (from code in m_Object.Codes select code.ToString()).ToArray();
                if (nbCode == 0)
                {
                    m_CodeText.color = ApplicationState.GeneralSettings.Theme.General.Error;
                    m_CodeButton.interactable = false;
                }
                else
                {
                    m_CodeText.color = ApplicationState.GeneralSettings.Theme.Window.Content.Item.Text.Color;
                    m_CodeButton.interactable = true;
                }
                switch (m_Object.Type)
                {
                    case Data.Experience.Protocol.Event.TypeEnum.Main: m_TypeText.text = "M"; break;
                    case Data.Experience.Protocol.Event.TypeEnum.Secondary: m_TypeText.text = "S"; break;
                }
            }
        }
        #endregion
    }
}