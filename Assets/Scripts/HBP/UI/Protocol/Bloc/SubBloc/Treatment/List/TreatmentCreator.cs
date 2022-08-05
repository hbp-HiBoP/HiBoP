using System.Linq;

namespace HBP.UI.Experience.Protocol
{
    public class TreatmentCreator : ObjectCreator<Core.Data.Treatment>
    {
        #region Properties
        Tools.CSharp.Window m_Window;
        /// <summary>
        /// SubBloc Window.
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

        Tools.CSharp.Window m_Baseline;
        /// <summary>
        /// SubBloc Baseline.
        /// </summary>
        public Tools.CSharp.Window Baseline
        {
            get
            {
                return m_Baseline;
            }
            set
            {
                m_Baseline = value;
                foreach (var modifier in WindowsReferencer.Windows.OfType<TreatmentModifier>())
                {
                    modifier.Baseline = value;
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Create Treatment from Scrath.
        /// </summary>
        public override void CreateFromScratch()
        {
            OpenModifier(new Core.Data.AbsTreatment(true,Window,false,Baseline,0));
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Open a Treatment modifier.
        /// </summary>
        /// <param name="item">Treatment to modify</param>
        /// <returns>Treatment modifier</returns>
        protected override ObjectModifier<Core.Data.Treatment> OpenModifier(Core.Data.Treatment item)
        {
            TreatmentModifier modifier =  base.OpenModifier(item) as TreatmentModifier;
            modifier.Window = Window;
            modifier.Baseline = Baseline;
            return modifier;
        }
        #endregion
    }
}