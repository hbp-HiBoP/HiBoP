using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Experience.Protocol
{
	public class EventItem : Tools.Unity.Lists.SavableItem<Data.Experience.Protocol.Event> 
	{
		#region Attributs
		[SerializeField]
		InputField m_labelInputField;
		[SerializeField]
		InputField m_codeInputField;
        #endregion

        #region Public Methods
        protected override void SetObject(Data.Experience.Protocol.Event eventToSet)
        {
            m_Object = eventToSet;
            m_labelInputField.text = m_Object.Name;
            m_codeInputField.text = m_Object.CodesString;
        }
        public override void Save()
        {
            Object.Name = m_labelInputField.text;
            Object.CodesString = m_codeInputField.text;
        }
		#endregion
    }
}