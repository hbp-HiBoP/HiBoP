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
        [SerializeField] Button m_BlocsButton;
        [SerializeField] LabelList m_BlocsList;

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
                if(nbBlocs == 0)
                {
                    m_BlocsText.color = ApplicationState.GeneralSettings.Theme.General.Error;
                    m_BlocsButton.interactable = false;
                }
                else
                {
                    m_BlocsText.color = ApplicationState.GeneralSettings.Theme.Window.Content.Text.Color;
                    m_BlocsButton.interactable = true;
                }
            }
        }
        #endregion

        #region Public Methods
        public void SetBlocs()
        {
            m_BlocsList.Objects = (from bloc in m_Object.Blocs select bloc.DisplayInformations.Name).ToArray();
        }
        #endregion
    }
}
