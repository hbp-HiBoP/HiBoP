using Tools.Unity.Components;
using HBP.Data.Experience.Protocol;
using System.Linq;

namespace HBP.UI.Experience.Protocol
{
    public class TreatmentCreator : ObjectCreator<Treatment>
    {
        Tools.CSharp.Window m_Window;
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

        public override void CreateFromScratch()
        {
            OpenModifier(new AbsTreatment(true,Window,false,Baseline,0));
        }

        protected override ObjectModifier<Treatment> OpenModifier(Treatment item)
        {
            TreatmentModifier modifier =  base.OpenModifier(item) as TreatmentModifier;
            modifier.Window = Window;
            modifier.Baseline = Baseline;
            return modifier;
        }
    }
}