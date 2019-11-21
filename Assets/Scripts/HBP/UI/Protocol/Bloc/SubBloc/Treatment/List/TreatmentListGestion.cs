using HBP.Data.Experience.Protocol;
using System.Linq;
using Tools.Unity.Components;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI.Experience.Protocol
{
    public class TreatmentListGestion : ListGestion<Treatment>
    {
        #region Properties
        [SerializeField] protected TreatmentList m_List;
        public override SelectableListWithItemAction<Treatment> List => m_List;

        [SerializeField] protected TreatmentCreator m_ObjectCreator;
        public override ObjectCreator<Treatment> ObjectCreator => m_ObjectCreator;

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
                m_ObjectCreator.Window = value;
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
                m_ObjectCreator.Baseline = value;

            }
        }
        #endregion

        #region Public Methods
        protected override ObjectModifier<Treatment> OpenModifier(Treatment item, bool interactable)
        {
            TreatmentModifier modifier = base.OpenModifier(item, interactable) as TreatmentModifier;
            modifier.Window = Window;
            modifier.Baseline = Baseline;
            return modifier;
        }
        #endregion
    }
}