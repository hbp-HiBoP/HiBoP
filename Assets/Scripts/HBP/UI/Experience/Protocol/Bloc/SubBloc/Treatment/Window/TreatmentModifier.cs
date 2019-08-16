using System;
using System.Collections.Generic;
using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Protocol;

namespace HBP.UI.Experience.Protocol
{
    public class TreatmentModifier : ItemModifier<d.Treatment>
    {
        #region Properties
        [SerializeField] Toggle m_WindowToggle;
        [SerializeField] RangeSlider m_WindowSlider;
        [SerializeField] Toggle m_BaselineToggle;
        [SerializeField] RangeSlider m_BaselineSlider;
        [SerializeField] InputField m_OrderInputField;
        [SerializeField] Dropdown m_TypeDropdown;

        [SerializeField] ClampTreatmentSubModifier m_ClampTreatmentSubModifier;
        [SerializeField] AbsTreatmentSubModifier m_AbsTreatmentSubModifier;
        [SerializeField] MaxTreatmentSubModifier m_MaxTreatmentSubModifier;
        [SerializeField] MinTreatmentSubModifier m_MinTreatmentSubModifier;
        [SerializeField] MeanTreatmentSubModifier m_MeanTreatmentSubModifier;
        [SerializeField] MedianTreatmentSubModifier m_MedianTreatmentSubModifier;
        [SerializeField] OffsetTreatmentSubModifier m_OffsetTreatmentSubModifier;
        [SerializeField] RescaleTreatmentSubModifier m_RescaleTreatmentSubModifier;
        [SerializeField] ThresholdTreatmentSubModifier m_ThresholdTreatmentSubModifier;
        [SerializeField] FactorTreatmentSubModifier m_FactorTreatmentSubModifier;

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
                m_WindowSlider.minLimit = m_Window.Start;
                m_WindowSlider.maxLimit = m_Window.End;
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
                m_BaselineSlider.minLimit = m_Baseline.Start;
                m_BaselineSlider.maxLimit = m_Baseline.End;
            }
        }

        List<BaseSubModifier> m_SubModifiers;
        List<d.Treatment> m_TreatmentsTemp;

        Type[] m_Types;
        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;

                m_WindowToggle.interactable = value;
                m_WindowSlider.interactable = value && m_WindowToggle.isOn;

                m_BaselineToggle.interactable = value;
                m_BaselineSlider.interactable = value && m_BaselineToggle.isOn;

                m_OrderInputField.interactable = value;
                m_TypeDropdown.interactable = value;

                m_ClampTreatmentSubModifier.Interactable = value;
                m_AbsTreatmentSubModifier.Interactable = value;
                m_MaxTreatmentSubModifier.Interactable = value;
                m_MinTreatmentSubModifier.Interactable = value;
                m_MeanTreatmentSubModifier.Interactable = value;
                m_MedianTreatmentSubModifier.Interactable = value;
                m_OffsetTreatmentSubModifier.Interactable = value;
                m_RescaleTreatmentSubModifier.Interactable = value;
                m_ThresholdTreatmentSubModifier.Interactable = value;
                m_FactorTreatmentSubModifier.Interactable = value;
            }
        }

        #endregion

        #region Public Methods
        public override void Save()
        {
            ItemTemp.ID = Item.ID;
            Item = ItemTemp;
            OnSave.Invoke();
            base.Close();
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();

            m_OrderInputField.onEndEdit.AddListener(OnChangeOrder);

            m_WindowToggle.onValueChanged.AddListener(OnChangeUseOnWindow);
            m_WindowSlider.onValueChanged.AddListener(OnChangeWindow);

            m_BaselineToggle.onValueChanged.AddListener(OnChangeUseOnBaseline);
            m_BaselineSlider.onValueChanged.AddListener(OnChangeBaseline);

            m_TypeDropdown.onValueChanged.AddListener(OnChangeType);
            m_Types = m_TypeDropdown.Set(typeof(d.Treatment));

            m_SubModifiers = new List<BaseSubModifier>();

            m_ClampTreatmentSubModifier.Initialize();
            m_SubModifiers.Add(m_ClampTreatmentSubModifier);

            m_AbsTreatmentSubModifier.Initialize();
            m_SubModifiers.Add(m_AbsTreatmentSubModifier);

            m_MaxTreatmentSubModifier.Initialize();
            m_SubModifiers.Add(m_MaxTreatmentSubModifier);

            m_MinTreatmentSubModifier.Initialize();
            m_SubModifiers.Add(m_MinTreatmentSubModifier);

            m_MeanTreatmentSubModifier.Initialize();
            m_SubModifiers.Add(m_MeanTreatmentSubModifier);

            m_MedianTreatmentSubModifier.Initialize();
            m_SubModifiers.Add(m_MedianTreatmentSubModifier);

            m_OffsetTreatmentSubModifier.Initialize();
            m_SubModifiers.Add(m_OffsetTreatmentSubModifier);

            m_RescaleTreatmentSubModifier.Initialize();
            m_SubModifiers.Add(m_RescaleTreatmentSubModifier);

            m_ThresholdTreatmentSubModifier.Initialize();
            m_SubModifiers.Add(m_ThresholdTreatmentSubModifier);

            m_FactorTreatmentSubModifier.Initialize();
            m_SubModifiers.Add(m_FactorTreatmentSubModifier);

            m_TreatmentsTemp = new List<d.Treatment>();
            m_TreatmentsTemp.Add(new d.ClampTreatment());
            m_TreatmentsTemp.Add(new d.AbsTreatment());
            m_TreatmentsTemp.Add(new d.MaxTreatment());
            m_TreatmentsTemp.Add(new d.MinTreatment());
            m_TreatmentsTemp.Add(new d.MeanTreatment());
            m_TreatmentsTemp.Add(new d.MedianTreatment());
            m_TreatmentsTemp.Add(new d.OffsetTreatment());
            m_TreatmentsTemp.Add(new d.RescaleTreatment());
            m_TreatmentsTemp.Add(new d.ThresholdTreatment());
            m_TreatmentsTemp.Add(new d.FactorTreatment());
        }
        protected override void SetFields(d.Treatment objectToDisplay)
        {
            int index = m_TreatmentsTemp.FindIndex(t => t.GetType() == ItemTemp.GetType());
            m_TreatmentsTemp[index] = ItemTemp;

            m_OrderInputField.text = objectToDisplay.Order.ToString();
            m_TypeDropdown.SetValue(Array.IndexOf(m_Types, objectToDisplay.GetType()));

            m_WindowToggle.isOn = objectToDisplay.UseOnWindow;
            m_WindowSlider.minLimit = ApplicationState.UserPreferences.Data.Protocol.MinLimit;
            m_WindowSlider.maxLimit = ApplicationState.UserPreferences.Data.Protocol.MaxLimit;
            m_WindowSlider.step = ApplicationState.UserPreferences.Data.Protocol.Step;
            m_WindowSlider.Values = objectToDisplay.Window.ToVector2();

            m_BaselineToggle.isOn = objectToDisplay.UseOnBaseline;
            m_BaselineSlider.minLimit = ApplicationState.UserPreferences.Data.Protocol.MinLimit;
            m_BaselineSlider.maxLimit = ApplicationState.UserPreferences.Data.Protocol.MaxLimit;
            m_BaselineSlider.step = ApplicationState.UserPreferences.Data.Protocol.Step;
            m_BaselineSlider.Values = objectToDisplay.Baseline.ToVector2();
        }
        void OnChangeType(int value)
        {
            Type type = m_Types[value];        
            
            // Close old subModifier
            m_SubModifiers.Find(subModifier => subModifier.GetType().IsSubclassOf(typeof(SubModifier<>).MakeGenericType(itemTemp.GetType()))).IsActive = false;

            d.Treatment treatment = m_TreatmentsTemp.Find(t => t.GetType() == type);
            treatment.Copy(ItemTemp);
            itemTemp = treatment;

            // Open new subModifier;
            BaseSubModifier newSubModifier = m_SubModifiers.Find(subModifier => subModifier.GetType().IsSubclassOf(typeof(SubModifier<>).MakeGenericType(type)));
            newSubModifier.IsActive = true;
            newSubModifier.Object = itemTemp;
        }
        void OnChangeWindow(float min, float max)
        {
            ItemTemp.Window = new Tools.CSharp.Window(Mathf.RoundToInt(min), Mathf.RoundToInt(max));
        }
        void OnChangeBaseline(float min, float max)
        {
            ItemTemp.Baseline = new Tools.CSharp.Window(Mathf.RoundToInt(min), Mathf.RoundToInt(max));
        }
        void OnChangeUseOnWindow(bool value)
        {
            ItemTemp.UseOnWindow = value;
            m_WindowSlider.interactable = Interactable && value;
        }
        void OnChangeUseOnBaseline(bool value)
        {
            ItemTemp.UseOnBaseline = value;
            m_BaselineSlider.interactable = Interactable && value;
        }
        void OnChangeOrder(string order)
        {
            if (int.TryParse(order, out int result)) itemTemp.Order = result;
        }
        #endregion
    }
}