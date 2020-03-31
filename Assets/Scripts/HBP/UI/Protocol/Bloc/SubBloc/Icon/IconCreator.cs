using HBP.Data.Experience.Protocol;
using System.Linq;
using Tools.Unity.Components;

namespace HBP.UI.Experience.Protocol
{
    /// <summary>
    /// Component to create new Icons.
    /// </summary>
    public class IconCreator : ObjectCreator<Icon>
    {
        Tools.CSharp.Window m_Window;
        /// <summary>
        /// SubBloc window.
        /// </summary>
        public Tools.CSharp.Window Window
        {
            get
            {
                return m_Window;
            }
            set
            {
                m_Window = value;
                foreach (var modifier in WindowsReferencer.Windows.OfType<TreatmentModifier>())
                {
                    modifier.Window = value;
                }
            }
        }

        /// <summary>
        /// Create new Icon from scrath.
        /// </summary>
        public override void CreateFromScratch()
        {
            OpenModifier(new Icon(Window));
        }

        /// <summary>
        /// Open a new Icon modifier.
        /// </summary>
        /// <param name="item">Icon to modify</param>
        /// <returns>Icon modifier created</returns>
        protected override ObjectModifier<Icon> OpenModifier(Icon item)
        {
            IconModifier modifier = base.OpenModifier(item) as IconModifier;
            modifier.Window = Window;
            return modifier;
        }
    }
}