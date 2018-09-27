﻿using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Protocol;

namespace HBP.UI.Experience.Protocol
{
    public class SubBlocModifier : ItemModifier<d.SubBloc>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] InputField m_OrderInputField;
        [SerializeField] Dropdown m_TypeDropdown;
        [SerializeField] Slider m_StartWindowSlider;
        [SerializeField] Slider m_EndWindowSlider;
        [SerializeField] Slider m_StartBaselineSlider;
        [SerializeField] Slider m_EndBaselineSlider;

        [SerializeField] EventListGestion m_EventListGestion;
        [SerializeField] IconListGestion m_IconListGestion;
        [SerializeField] TreatmentListGestion m_TreatmentListGestion;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;
                m_NameInputField.interactable = value;
                m_OrderInputField.interactable = value;
                m_TypeDropdown.interactable = value && ItemTemp.Type == Data.Enums.MainSecondaryEnum.Secondary;
                m_StartWindowSlider.interactable = value;
                m_EndWindowSlider.interactable = value;
                m_StartBaselineSlider.interactable = value;
                m_EndBaselineSlider.interactable = value;
                m_EventListGestion.Interactable = value;
                m_IconListGestion.Interactable = value;
                m_TreatmentListGestion.Interactable = value;
            }
        }

        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onEndEdit.AddListener((value) => ItemTemp.Name = value);
            m_OrderInputField.onEndEdit.AddListener((value) => ItemTemp.Order = int.Parse(value));
            m_TypeDropdown.onValueChanged.AddListener((value) => ItemTemp.Type = (Data.Enums.MainSecondaryEnum) value);
            m_StartWindowSlider.onValueChanged.AddListener((value) => ItemTemp.Window = new Tools.CSharp.Window((int)value, ItemTemp.Window.End));
            m_EndWindowSlider.onValueChanged.AddListener((value) => ItemTemp.Window = new Tools.CSharp.Window(ItemTemp.Window.Start, (int)value));
            m_StartBaselineSlider.onValueChanged.AddListener((value) => ItemTemp.Baseline = new Tools.CSharp.Window((int)value, ItemTemp.Baseline.End));
            m_EndBaselineSlider.onValueChanged.AddListener((value) => ItemTemp.Baseline = new Tools.CSharp.Window(ItemTemp.Baseline.Start, (int)value));
            m_EventListGestion.Initialize(m_SubWindows);
            m_IconListGestion.Initialize(m_SubWindows);
            m_TreatmentListGestion.Initialize(m_SubWindows);
        }
        protected override void SetFields(d.SubBloc objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_OrderInputField.text = objectToDisplay.Order.ToString();
            m_TypeDropdown.Set(typeof(Data.Enums.MainSecondaryEnum), (int)objectToDisplay.Type);
            m_StartWindowSlider.value = objectToDisplay.Window.Start;
            m_EndWindowSlider.value = objectToDisplay.Window.End;
            m_StartBaselineSlider.value = objectToDisplay.Baseline.Start;
            m_EndBaselineSlider.value = objectToDisplay.Baseline.End;
            m_EventListGestion.Items = objectToDisplay.Events;
            m_IconListGestion.Items = objectToDisplay.Icons;
            m_TreatmentListGestion.Items = objectToDisplay.Treatments;
        }
        #endregion
    }
}