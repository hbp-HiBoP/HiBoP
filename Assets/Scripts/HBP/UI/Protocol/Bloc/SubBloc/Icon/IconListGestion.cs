using UnityEngine;
using System.Linq;
using Tools.Unity.Lists;

namespace HBP.UI.Experience.Protocol
{
    public class IconListGestion : ListGestion<Core.Data.Icon>
    {
        #region Properties
        [SerializeField] protected IconList m_List;
        public override ActionableList<Core.Data.Icon> List => m_List;

        [SerializeField] protected IconCreator m_ObjectCreator;
        public override ObjectCreator<Core.Data.Icon> ObjectCreator => m_ObjectCreator;

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
                m_ObjectCreator.Window = value;
                foreach (var modifier in WindowsReferencer.Windows.OfType<IconModifier>())
                {
                    modifier.Window = value;
                }
            }
        }
        #endregion

        #region Protected Methods
        protected override ObjectModifier<Core.Data.Icon> OpenModifier(Core.Data.Icon item)
        {
            IconModifier modifier = base.OpenModifier(item) as IconModifier;
            modifier.Window = Window;
            return modifier;
        }
        #endregion
    }
}