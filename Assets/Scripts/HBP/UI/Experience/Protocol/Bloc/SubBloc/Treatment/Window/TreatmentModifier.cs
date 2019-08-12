using System;
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

        d.ClampTreatment m_ClampTreatmentTemp = new d.ClampTreatment();
        d.AbsTreatment m_AbsTreatmentTemp = new d.AbsTreatment();
        d.MaxTreatment m_MaxTreatmentTemp = new d.MaxTreatment();
        d.MinTreatment m_MinTreatmentTemp = new d.MinTreatment();
        d.MeanTreatment m_MeanTreatmentTemp = new d.MeanTreatment();
        d.MedianTreatment m_MedianTreatmentTemp = new d.MedianTreatment();
        d.OffsetTreatment m_OffsetTreatmentTemp = new d.OffsetTreatment();
        d.RescaleTreatment m_RescaleTreatmentTemp = new d.RescaleTreatment();
        d.TresholdTreatment m_TresholdTreatmentTemp = new d.TresholdTreatment();

        [SerializeField] ClampTreatmentSubModifier m_ClampTreatmentSubModifier;
        [SerializeField] AbsTreatmentSubModifier m_AbsTreatmentSubModifier;
        [SerializeField] MaxTreatmentSubModifier m_MaxTreatmentSubModifier;
        [SerializeField] MinTreatmentSubModifier m_MinTreatmentSubModifier;
        [SerializeField] MeanTreatmentSubModifier m_MeanTreatmentSubModifier;
        [SerializeField] MedianTreatmentSubModifier m_MedianTreatmentSubModifier;
        [SerializeField] OffsetTreatmentSubModifier m_OffsetTreatmentSubModifier;
        [SerializeField] RescaleTreatmentSubModifier m_RescaleTreatmentSubModifier;
        [SerializeField] TresholdTreatmentSubModifier m_TresholdTreatmentSubModifier;

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
                m_TresholdTreatmentSubModifier.Interactable = value;
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

            m_ClampTreatmentSubModifier.Initialize();
            m_AbsTreatmentSubModifier.Initialize();
            m_MaxTreatmentSubModifier.Initialize();
            m_MinTreatmentSubModifier.Initialize();
            m_MeanTreatmentSubModifier.Initialize();
            m_MedianTreatmentSubModifier.Initialize();
            m_OffsetTreatmentSubModifier.Initialize();
            m_RescaleTreatmentSubModifier.Initialize();
            m_TresholdTreatmentSubModifier.Initialize();

        }
        protected override void SetFields(d.Treatment objectToDisplay)
        {
            if (itemTemp is d.ClampTreatment clampTreatment) m_ClampTreatmentTemp = clampTreatment;
            else if (itemTemp is d.AbsTreatment absTreatment) m_AbsTreatmentTemp = absTreatment;
            else if (itemTemp is d.MaxTreatment maxTreatment) m_MaxTreatmentTemp = maxTreatment;
            else if (itemTemp is d.MinTreatment minTreatment) m_MinTreatmentTemp = minTreatment;
            else if (itemTemp is d.MeanTreatment meanTreatment) m_MeanTreatmentTemp = meanTreatment;
            else if (itemTemp is d.MedianTreatment medianTreatment) m_MedianTreatmentTemp = medianTreatment;
            else if (itemTemp is d.OffsetTreatment offsetTreatment) m_OffsetTreatmentTemp = offsetTreatment;
            else if (itemTemp is d.RescaleTreatment rescaleTreatment) m_RescaleTreatmentTemp = rescaleTreatment;
            else if (itemTemp is d.TresholdTreatment tresholdTreatment) m_TresholdTreatmentTemp = tresholdTreatment;

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
            if (type == typeof(d.ClampTreatment))
            {
                if (itemTemp is d.AbsTreatment absTreatment)
                {
                    m_AbsTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MaxTreatment maxTreatment)
                {
                    m_MaxTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MinTreatment minTreatment)
                {
                    m_MinTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MeanTreatment meanTreatment)
                {
                    m_MeanTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MedianTreatment medianTreatment)
                {
                    m_MedianTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.OffsetTreatment offsetTreatment)
                {
                    m_OffsetTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.RescaleTreatment rescaleTreatment)
                {
                    m_ClampTreatmentTemp.Min = rescaleTreatment.AfterMin;
                    m_ClampTreatmentTemp.Max = rescaleTreatment.AfterMax;
                    m_RescaleTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.TresholdTreatment tresholdTreatment)
                {
                    m_ClampTreatmentTemp.Min = tresholdTreatment.Min;
                    m_ClampTreatmentTemp.Max = tresholdTreatment.Max;
                    m_ClampTreatmentTemp.UseMinClamp = tresholdTreatment.UseMinTreshold;
                    m_ClampTreatmentTemp.UseMaxClamp = tresholdTreatment.UseMaxTreshold;
                    m_TresholdTreatmentSubModifier.IsActive = false;
                }

                m_ClampTreatmentTemp.Window = itemTemp.Window;
                m_ClampTreatmentTemp.Order = itemTemp.Order;
                m_ClampTreatmentSubModifier.Object = m_ClampTreatmentTemp;
                m_ClampTreatmentSubModifier.IsActive = true;
                itemTemp = m_ClampTreatmentTemp;
            }
            else if (type == typeof(d.AbsTreatment))
            {
                if (itemTemp is d.ClampTreatment clampTreatment)
                {
                    m_ClampTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MaxTreatment maxTreatment)
                {
                    m_MaxTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MinTreatment minTreatment)
                {
                    m_MinTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MeanTreatment meanTreatment)
                {
                    m_MeanTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MedianTreatment medianTreatment)
                {
                    m_MedianTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.OffsetTreatment offsetTreatment)
                {
                    m_OffsetTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.RescaleTreatment rescaleTreatment)
                {
                    m_RescaleTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.TresholdTreatment tresholdTreatment)
                {
                    m_TresholdTreatmentSubModifier.IsActive = false;
                }

                m_AbsTreatmentTemp.Window = itemTemp.Window;
                m_AbsTreatmentTemp.Order = itemTemp.Order;
                m_AbsTreatmentSubModifier.Object = m_AbsTreatmentTemp;
                m_AbsTreatmentSubModifier.IsActive = true;
                itemTemp = m_AbsTreatmentTemp;
            }
            else if (type == typeof(d.MaxTreatment))
            {
                if (itemTemp is d.ClampTreatment clampTreatment)
                {
                    m_ClampTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.AbsTreatment absTreatment)
                {
                    m_AbsTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MinTreatment minTreatment)
                {
                    m_MinTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MeanTreatment meanTreatment)
                {
                    m_MeanTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MedianTreatment medianTreatment)
                {
                    m_MedianTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.OffsetTreatment offsetTreatment)
                {
                    m_OffsetTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.RescaleTreatment rescaleTreatment)
                {
                    m_RescaleTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.TresholdTreatment tresholdTreatment)
                {
                    m_TresholdTreatmentSubModifier.IsActive = false;
                }

                m_MaxTreatmentTemp.Window = itemTemp.Window;
                m_MaxTreatmentTemp.Order = itemTemp.Order;
                m_MaxTreatmentSubModifier.Object = m_MaxTreatmentTemp;
                m_MaxTreatmentSubModifier.IsActive = true;
                itemTemp = m_MaxTreatmentTemp;
            }
            else if (type == typeof(d.MinTreatment))
            {
                if (itemTemp is d.ClampTreatment clampTreatment)
                {
                    m_ClampTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.AbsTreatment absTreatment)
                {
                    m_AbsTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MaxTreatment maxTreatment)
                {
                    m_MaxTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MeanTreatment meanTreatment)
                {
                    m_MeanTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MedianTreatment medianTreatment)
                {
                    m_MedianTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.OffsetTreatment offsetTreatment)
                {
                    m_OffsetTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.RescaleTreatment rescaleTreatment)
                {
                    m_RescaleTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.TresholdTreatment tresholdTreatment)
                {
                    m_TresholdTreatmentSubModifier.IsActive = false;
                }

                m_MinTreatmentTemp.Window = itemTemp.Window;
                m_MinTreatmentTemp.Order = itemTemp.Order;
                m_MinTreatmentSubModifier.Object = m_MinTreatmentTemp;
                m_MinTreatmentSubModifier.IsActive = true;
                itemTemp = m_MinTreatmentTemp;
            }
            else if (type == typeof(d.MeanTreatment))
            {
                if (itemTemp is d.ClampTreatment clampTreatment)
                {
                    m_ClampTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.AbsTreatment absTreatment)
                {
                    m_AbsTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MaxTreatment maxTreatment)
                {
                    m_MaxTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MinTreatment minTreatment)
                {
                    m_MinTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MedianTreatment medianTreatment)
                {
                    m_MedianTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.OffsetTreatment offsetTreatment)
                {
                    m_OffsetTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.RescaleTreatment rescaleTreatment)
                {
                    m_RescaleTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.TresholdTreatment tresholdTreatment)
                {
                    m_TresholdTreatmentSubModifier.IsActive = false;
                }

                m_MeanTreatmentTemp.Window = itemTemp.Window;
                m_MeanTreatmentTemp.Order = itemTemp.Order;
                m_MeanTreatmentSubModifier.Object = m_MeanTreatmentTemp;
                m_MeanTreatmentSubModifier.IsActive = true;
                itemTemp = m_MeanTreatmentTemp;
            }
            else if (type == typeof(d.MedianTreatment))
            {
                if (itemTemp is d.ClampTreatment clampTreatment)
                {
                    m_ClampTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.AbsTreatment absTreatment)
                {
                    m_AbsTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MaxTreatment maxTreatment)
                {
                    m_MaxTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MinTreatment minTreatment)
                {
                    m_MinTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MeanTreatment meanTreatment)
                {
                    m_MeanTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.OffsetTreatment offsetTreatment)
                {
                    m_OffsetTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.RescaleTreatment rescaleTreatment)
                {
                    m_RescaleTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.TresholdTreatment tresholdTreatment)
                {
                    m_TresholdTreatmentSubModifier.IsActive = false;
                }

                m_MedianTreatmentTemp.Window = itemTemp.Window;
                m_MedianTreatmentTemp.Order = itemTemp.Order;
                m_MedianTreatmentSubModifier.Object = m_MedianTreatmentTemp;
                m_MedianTreatmentSubModifier.IsActive = true;
                itemTemp = m_MedianTreatmentTemp;
            }
            else if (type == typeof(d.OffsetTreatment))
            {
                if (itemTemp is d.ClampTreatment clampTreatment)
                {
                    m_ClampTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.AbsTreatment absTreatment)
                {
                    m_AbsTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MaxTreatment maxTreatment)
                {
                    m_MaxTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MinTreatment minTreatment)
                {
                    m_MinTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MeanTreatment meanTreatment)
                {
                    m_MeanTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MedianTreatment medianTreatment)
                {
                    m_MedianTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.RescaleTreatment rescaleTreatment)
                {
                    m_RescaleTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.TresholdTreatment tresholdTreatment)
                {
                    m_TresholdTreatmentSubModifier.IsActive = false;
                }

                m_OffsetTreatmentTemp.Window = itemTemp.Window;
                m_OffsetTreatmentTemp.Order = itemTemp.Order;
                m_OffsetTreatmentSubModifier.Object = m_OffsetTreatmentTemp;
                m_OffsetTreatmentSubModifier.IsActive = true;
                itemTemp = m_OffsetTreatmentTemp;
            }
            else if (type == typeof(d.RescaleTreatment))
            {
                if (itemTemp is d.ClampTreatment clampTreatment)
                {
                    m_ClampTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.AbsTreatment absTreatment)
                {
                    m_AbsTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MaxTreatment maxTreatment)
                {
                    m_MaxTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MinTreatment minTreatment)
                {
                    m_MinTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MeanTreatment meanTreatment)
                {
                    m_MeanTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MedianTreatment medianTreatment)
                {
                    m_MedianTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.OffsetTreatment offsetTreatment)
                {
                    m_OffsetTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.TresholdTreatment tresholdTreatment)
                {
                    m_TresholdTreatmentSubModifier.IsActive = false;
                }

                m_RescaleTreatmentTemp.Window = itemTemp.Window;
                m_RescaleTreatmentTemp.Order = itemTemp.Order;
                m_RescaleTreatmentSubModifier.Object = m_RescaleTreatmentTemp;
                m_RescaleTreatmentSubModifier.IsActive = true;
                itemTemp = m_RescaleTreatmentTemp;
            }
            else if (type == typeof(d.TresholdTreatment))
            {
                if (itemTemp is d.ClampTreatment clampTreatment)
                {
                    m_TresholdTreatmentTemp.Min = clampTreatment.Min;
                    m_TresholdTreatmentTemp.Max = clampTreatment.Max;
                    m_TresholdTreatmentTemp.UseMinTreshold = clampTreatment.UseMinClamp;
                    m_TresholdTreatmentTemp.UseMaxTreshold = clampTreatment.UseMaxClamp;
                    m_ClampTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.AbsTreatment absTreatment)
                {
                    m_AbsTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MaxTreatment maxTreatment)
                {
                    m_MaxTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MinTreatment minTreatment)
                {
                    m_MinTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MeanTreatment meanTreatment)
                {
                    m_MeanTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.MedianTreatment medianTreatment)
                {
                    m_MedianTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.OffsetTreatment offsetTreatment)
                {
                    m_OffsetTreatmentSubModifier.IsActive = false;
                }
                else if (itemTemp is d.RescaleTreatment rescaleTreatment)
                {
                    m_RescaleTreatmentSubModifier.IsActive = false;
                }

                m_TresholdTreatmentTemp.Window = itemTemp.Window;
                m_TresholdTreatmentTemp.Order = itemTemp.Order;
                m_TresholdTreatmentSubModifier.Object = m_TresholdTreatmentTemp;
                m_TresholdTreatmentSubModifier.IsActive = true;
                itemTemp = m_TresholdTreatmentTemp;
            }
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
            ItemTemp.Order = int.Parse(order);
        }
        #endregion
    }
}