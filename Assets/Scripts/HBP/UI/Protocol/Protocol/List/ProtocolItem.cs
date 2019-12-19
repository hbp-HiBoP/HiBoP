using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Protocol;
using Tools.Unity.Lists;
using System.Linq;

namespace HBP.UI.Experience.Protocol
{
	public class ProtocolItem : ActionnableItem<d.Protocol> 
	{
		#region Properties
		[SerializeField] Text m_NameText;
        [SerializeField] Text m_BlocsText;
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
                m_BlocsText.SetIEnumerableFieldInItem("Blocs", from bloc in m_Object.Blocs select bloc.Name, m_ErrorState);
            }
        }
        #endregion
    }
}
