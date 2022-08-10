﻿using System.Linq;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI.Experience.Protocol
{
    public class TreatmentListGestion : ListGestion<Core.Data.Treatment>
    {
        #region Properties
        [SerializeField] protected TreatmentList m_List;
        public override ActionableList<Core.Data.Treatment> List => m_List;

        [SerializeField] protected TreatmentCreator m_ObjectCreator;
        public override ObjectCreator<Core.Data.Treatment> ObjectCreator => m_ObjectCreator;

        Core.Tools.TimeWindow m_Window;
        public Core.Tools.TimeWindow Window
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

        Core.Tools.TimeWindow m_Baseline;
        public Core.Tools.TimeWindow Baseline
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
        protected override ObjectModifier<Core.Data.Treatment> OpenModifier(Core.Data.Treatment item)
        {
            TreatmentModifier modifier = base.OpenModifier(item) as TreatmentModifier;
            modifier.Window = Window;
            modifier.Baseline = Baseline;
            return modifier;
        }
        #endregion
    }
}