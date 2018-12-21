using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Protocol;
using Tools.Unity.Lists;
using NewTheme.Components;
using System.Linq;
using System.Collections.Generic;

namespace HBP.UI.Experience.Protocol
{
	public class ProtocolItem : ActionnableItem<d.Protocol> 
	{
		#region Properties
		[SerializeField] Text m_NameText;
        [SerializeField] Text m_BlocsText;
        [SerializeField] LabelList m_BlocsList;

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

                int nbBlocs = value.Blocs.Count;
                m_BlocsText.text = nbBlocs.ToString();
                if (nbBlocs == 0) m_BlocsText.GetComponent<ThemeElement>().Set(m_ErrorState);
                else m_BlocsText.GetComponent<ThemeElement>().Set();
            }
        }
        #endregion

        #region Public Methods
        public void SetBlocs()
        {
            m_BlocsList.Initialize();
            IEnumerable<string> labels = from bloc in m_Object.Blocs select bloc.Name;
            if (labels.Count() == 0) labels = new string[] { "No Bloc" };
            m_BlocsList.Objects = labels.ToArray();
        }
        #endregion
    }
}
