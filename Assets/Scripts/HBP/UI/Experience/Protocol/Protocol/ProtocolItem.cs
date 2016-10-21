using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Protocol;

namespace HBP.UI.Experience.Protocol
{
	public class ProtocolItem : Tools.Unity.Lists.ListItemWithActions<d.Protocol> 
	{
		#region Attributs
		[SerializeField]
		Text m_name;
        [SerializeField]
        Text m_blocs;
		#endregion

		#region Private Methods
        protected override void SetObject(d.Protocol protocol)
        {
            m_name.text = Object.Name;
            m_blocs.text = Object.Blocs.Count.ToString();
        }
		#endregion
	}
}
