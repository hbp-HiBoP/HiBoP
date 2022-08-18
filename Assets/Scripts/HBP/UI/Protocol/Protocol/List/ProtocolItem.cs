using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using HBP.UI.Tools.Lists;

namespace HBP.UI.Main
{
    /// <summary>
    /// Component to display protocol in list.
    /// </summary>
	public class ProtocolItem : ActionnableItem<Core.Data.Protocol> 
	{
		#region Properties
		[SerializeField] Text m_NameText;
        [SerializeField] Text m_BlocsText;
        [SerializeField] Theme.State m_ErrorState;

        /// <summary>
        /// Object to display.
        /// </summary>
        public override Core.Data.Protocol Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_NameText.text = value.Name;
                m_BlocsText.SetIEnumerableFieldInItem("Blocs", from bloc in m_Object.Blocs select bloc.Name, m_ErrorState);
            }
        }
        #endregion
    }
}
