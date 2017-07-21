using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Experience.Protocol
{
	public class EventItem : Tools.Unity.Lists.SavableItem<Data.Experience.Protocol.Event> 
	{
		#region Attributs
		[SerializeField]
		InputField m_LabelInputField;
		[SerializeField]
		InputField m_CodeInputField;
        public override Data.Experience.Protocol.Event Object
        {
            get
            {
                return base.Object;
            }

            set
            {
                base.Object = value;
                m_LabelInputField.text = m_Object.Name;
                m_CodeInputField.text = m_Object.CodesString;
            }
        }
        #endregion

        #region Public Methods
        public override void Save()
        {
            Object.Name = m_LabelInputField.text;
            Object.CodesString = m_CodeInputField.text;
        }
		#endregion
    }
}