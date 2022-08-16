using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.UI.Tools;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Experience.Protocol
{
    /// <summary>
    /// window to modify a treatment.
    /// </summary>
    public class TreatmentModifier : ObjectModifier<Treatment>
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

        Core.Tools.TimeWindow m_Window;
        /// <summary>
        /// SubBloc of the window.
        /// </summary>
        public Core.Tools.TimeWindow Window
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

        Core.Tools.TimeWindow m_Baseline;
        /// <summary>
        /// Baseline of the subBloc.
        /// </summary>
        public Core.Tools.TimeWindow Baseline
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
        List<Treatment> m_TreatmentsTemp;

        Type[] m_Types;
        /// <summary>
        /// True if interactable, False otherwise.
        /// </summary>
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
        /// <summary>
        /// Save the modifications.
        /// </summary>
        public override void OK()
        {
            m_Object = ObjectTemp;
            base.OK();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Initialize the window.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            m_OrderInputField.onEndEdit.AddListener(ChangeOrder);

            m_WindowToggle.onValueChanged.AddListener(ChangeUseOnWindow);
            m_WindowSlider.onValueChanged.AddListener(ChangeWindow);

            m_BaselineToggle.onValueChanged.AddListener(ChangeUseOnBaseline);
            m_BaselineSlider.onValueChanged.AddListener(ChangeBaseline);

            m_TypeDropdown.onValueChanged.AddListener(ChangeType);
            m_Types = m_TypeDropdown.Set(typeof(Treatment));

            m_ClampTreatmentSubModifier.Initialize();
            m_AbsTreatmentSubModifier.Initialize();
            m_MaxTreatmentSubModifier.Initialize();
            m_MinTreatmentSubModifier.Initialize();
            m_MeanTreatmentSubModifier.Initialize();
            m_MedianTreatmentSubModifier.Initialize();
            m_OffsetTreatmentSubModifier.Initialize();
            m_RescaleTreatmentSubModifier.Initialize();
            m_ThresholdTreatmentSubModifier.Initialize();
            m_FactorTreatmentSubModifier.Initialize();

            m_SubModifiers = new List<BaseSubModifier>
            {
                m_ClampTreatmentSubModifier,
                m_AbsTreatmentSubModifier,
                m_MaxTreatmentSubModifier,
                m_MinTreatmentSubModifier,
                m_MeanTreatmentSubModifier,
                m_MedianTreatmentSubModifier,
                m_OffsetTreatmentSubModifier,
                m_RescaleTreatmentSubModifier,
                m_ThresholdTreatmentSubModifier,
                m_FactorTreatmentSubModifier
            };
            m_TreatmentsTemp = new List<Treatment>
            {
                new ClampTreatment(),
                new AbsTreatment(),
                new MaxTreatment(),
                new MinTreatment(),
                new MeanTreatment(),
                new MedianTreatment(),
                new OffsetTreatment(),
                new RescaleTreatment(),
                new ThresholdTreatment(),
                new FactorTreatment()
            };
        }
        /// <summary>
        /// Set the fields.
        /// </summary>
        /// <param name="objectToDisplay">Treatment to display</param>
        protected override void SetFields(Treatment objectToDisplay)
        {
            int index = m_TreatmentsTemp.FindIndex(t => t.GetType() == ObjectTemp.GetType());
            m_TreatmentsTemp[index] = ObjectTemp;

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
        /// <summary>
        /// Change the order of the treatment.
        /// </summary>
        /// <param name="order">Order</param>
        protected void ChangeOrder(string value)
        {
            if(int.TryParse(value, out int order))
            {
                ObjectTemp.Order = order;
            }
            else
            {
                m_OrderInputField.text = ObjectTemp.Order.ToString();
            }
        }
        /// <summary>
        /// Change the type of the treatment.
        /// </summary>
        /// <param name="index">Index of the treatment type</param>
        protected void ChangeType(int index)
        {
            Type type = m_Types[index];        
            
            // Close old subModifier
            m_SubModifiers.Find(subModifier => subModifier.GetType().IsSubclassOf(typeof(SubModifier<>).MakeGenericType(m_ObjectTemp.GetType()))).IsActive = false;

            Treatment treatment = m_TreatmentsTemp.Find(t => t.GetType() == type);
            treatment.Copy(m_ObjectTemp);
            m_ObjectTemp = treatment;

            // Open new subModifier;
            BaseSubModifier newSubModifier = m_SubModifiers.Find(subModifier => subModifier.GetType().IsSubclassOf(typeof(SubModifier<>).MakeGenericType(type)));
            newSubModifier.IsActive = true;
            newSubModifier.Object = m_ObjectTemp;
        }
        /// <summary>
        /// Change the window.
        /// </summary>
        /// <param name="min">Start window</param>
        /// <param name="max">End window</param>
        protected void ChangeWindow(float min, float max)
        {
            ObjectTemp.Window = new Core.Tools.TimeWindow(Mathf.RoundToInt(min), Mathf.RoundToInt(max));
        }
        /// <summary>
        /// Change the baseline.
        /// </summary>
        /// <param name="min">Start window</param>
        /// <param name="max">End window</param>
        protected void ChangeBaseline(float min, float max)
        {
            ObjectTemp.Baseline = new Core.Tools.TimeWindow(Mathf.RoundToInt(min), Mathf.RoundToInt(max));
        }
        /// <summary>
        /// Change use on window.
        /// </summary>
        /// <param name="value">True if useWindow, False otherwise</param>
        protected void ChangeUseOnWindow(bool value)
        {
            ObjectTemp.UseOnWindow = value;
            m_WindowSlider.interactable = Interactable && value;
        }
        /// <summary>
        /// Change use on baseline.
        /// </summary>
        /// <param name="value">True if useBaseline, False otherwise</param>
        protected void ChangeUseOnBaseline(bool value)
        {
            ObjectTemp.UseOnBaseline = value;
            m_BaselineSlider.interactable = Interactable && value;
        }
        #endregion
    }
}