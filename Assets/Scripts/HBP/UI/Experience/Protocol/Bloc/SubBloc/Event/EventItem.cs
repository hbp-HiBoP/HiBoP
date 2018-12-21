using UnityEngine;
using UnityEngine.UI;
using Tools.Unity.Lists;
using NewTheme.Components;

namespace HBP.UI.Experience.Protocol
{
	public class EventItem : ActionnableItem<Data.Experience.Protocol.Event> 
	{
		#region Properties
		[SerializeField] Text m_NameText;
        [SerializeField] Text m_CodeText;
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

                int nbCode = m_Object.Codes.Count;
                m_CodeText.text = m_Object.Codes.Count.ToString();
                if (nbCode == 0) m_CodeText.GetComponent<ThemeElement>().Set(m_ErrorState);
                else m_CodeText.GetComponent<ThemeElement>().Set();

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