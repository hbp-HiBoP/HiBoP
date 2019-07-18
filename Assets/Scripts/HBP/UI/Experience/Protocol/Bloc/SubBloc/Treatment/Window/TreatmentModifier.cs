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
        [SerializeField] RangeSlider m_WindowSlider;
        [SerializeField] InputField m_OrderInputField;
        [SerializeField] Dropdown m_TypeDropdown;

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
                m_WindowSlider.interactable = value;
                m_OrderInputField.interactable = value;
                m_TypeDropdown.interactable = value;
            }
        }

        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            m_OrderInputField.onEndEdit.AddListener((value) => ItemTemp.Order = int.Parse(value));     
            m_TypeDropdown.onValueChanged.AddListener(OnChangeType);
            m_WindowSlider.onValueChanged.AddListener((min, max) => ItemTemp.Window = new Tools.CSharp.Window(Mathf.RoundToInt(min), Mathf.RoundToInt(max)));
            m_Types = m_TypeDropdown.Set(typeof(d.Treatment));
            base.Initialize();
        }
        protected override void SetFields(d.Treatment objectToDisplay)
        {
            m_OrderInputField.text = objectToDisplay.Order.ToString();
            m_TypeDropdown.SetValue(Array.IndexOf(m_Types, objectToDisplay.GetType()));

            //ProtocolPreferences preferences = ApplicationState.UserPreferences.Data.Protocol;
            //m_WindowSlider.minLimit = preferences.MinLimit;
            //m_WindowSlider.maxLimit = preferences.MaxLimit;
            //m_WindowSlider.step = preferences.Step;

            m_WindowSlider.Values = objectToDisplay.Window.ToVector2();
        }

        void OnChangeType(int value)
        {
            //Type type = m_Types[value];
            //if (type == typeof(iEEGDataInfo))
            //{
            //    if (itemTemp is d.CCEPDataInfo ccepDataInfo)
            //    {
            //        m_IEEGDataInfoTemp.Name = ccepDataInfo.Name;
            //        m_IEEGDataInfoTemp.Patient = ccepDataInfo.Patient;
            //        m_IEEGDataInfoTemp.DataContainer = ccepDataInfo.DataContainer;
            //    }

            //    m_PatientDataInfoGestion.Object = m_IEEGDataInfoTemp;
            //    m_PatientDataInfoGestion.IsActive = true;

            //    m_iEEGDataInfoSubModifier.Object = m_IEEGDataInfoTemp;
            //    m_iEEGDataInfoSubModifier.IsActive = true;
            //    m_CCEPDataInfoSubModifier.IsActive = false;

            //    m_DataContainerModifier.DataAttribute = new iEEG();
            //    m_DataContainerModifier.Object = m_IEEGDataInfoTemp.DataContainer;

            //    itemTemp = m_IEEGDataInfoTemp;
            //}
            //else if (type == typeof(CCEPDataInfo))
            //{
            //    if (itemTemp is d.iEEGDataInfo ieegDataInfo)
            //    {
            //        m_CCEPDataInfoTemp.Name = ieegDataInfo.Name;
            //        m_CCEPDataInfoTemp.Patient = ieegDataInfo.Patient;
            //        m_CCEPDataInfoTemp.DataContainer = ieegDataInfo.DataContainer;
            //    }
            //    m_PatientDataInfoGestion.Object = m_CCEPDataInfoTemp;
            //    m_PatientDataInfoGestion.IsActive = true;

            //    m_CCEPDataInfoSubModifier.Object = m_CCEPDataInfoTemp;
            //    m_CCEPDataInfoSubModifier.IsActive = true;
            //    m_iEEGDataInfoSubModifier.IsActive = false;

            //    m_DataContainerModifier.DataAttribute = new CCEP();
            //    m_DataContainerModifier.Object = m_CCEPDataInfoTemp.DataContainer;

            //    itemTemp = m_CCEPDataInfoTemp;
            //}
            //else
            //{
            //    m_PatientDataInfoGestion.IsActive = false;
            //    m_CCEPDataInfoSubModifier.IsActive = false;
            //    m_iEEGDataInfoSubModifier.IsActive = false;
            //}
        }
        #endregion
    }
}