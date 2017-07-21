using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Protocol;

namespace HBP.UI.Experience.Protocol
{
	public class ProtocolItem : Tools.Unity.Lists.ActionnableItem<d.Protocol> 
	{
		#region Properties
		[SerializeField] Text m_Name;
        [SerializeField] Text m_Blocs;
        public override d.Protocol Object
        {
            get
            {
                return base.Object;
            }

            set
            {
                base.Object = value;
                m_Name.text = value.Name;
                m_Blocs.text = value.Blocs.Count.ToString();
            }
        }
        #endregion
	}
}
