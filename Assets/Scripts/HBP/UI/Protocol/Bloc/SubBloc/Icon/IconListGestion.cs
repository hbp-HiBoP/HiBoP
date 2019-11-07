using HBP.Data.Experience.Protocol;
using Tools.Unity.Components;
using UnityEngine;
using System.Linq;
using Tools.Unity.Lists;

namespace HBP.UI.Experience.Protocol
{
    public class IconListGestion : ListGestion<Icon>
    {
        #region Properties
        [SerializeField] protected IconList m_List;
        public override SelectableListWithItemAction<Icon> List => m_List;

        [SerializeField] protected IconCreator m_ObjectCreator;
        public override ObjectCreator<Icon> ObjectCreator => m_ObjectCreator;

        [SerializeField] Tools.CSharp.Window m_Window;
        public Tools.CSharp.Window Window
        {
            get
            {
                return m_Window;
            }
            set
            {
                m_Window = value;
                foreach(var modifier in WindowsReferencer.Windows.OfType<IconModifier>())
                {
                    modifier.Window = value;
                }
            }
        }
        #endregion

        #region Protected Methods
        protected override ObjectModifier<Icon> OpenModifier(Icon item, bool interactable)
        {
            IconModifier modifier = base.OpenModifier(item, interactable) as IconModifier;
            modifier.Window = Window;
            return modifier;
        }
        #endregion
    }
}