using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using HBP.UI.Main;
using HBP.Core.Data;
using HBP.Data.Preferences;
using System.Linq;
using UnityEngine.UI;

namespace HBP.UI.Tools
{
    public class ListFilter : DialogWindow
    {
        #region Properties
        [SerializeField] protected FilterConditionListGestion m_ListGestion;
        [SerializeField] protected Button m_ApplyButton;

        protected List<BaseData> m_FilteringObjects;
        public List<BaseData> FilteringObjects
        {
            get => m_FilteringObjects;
            set
            {
                if (value.Count == 0)
                {
                    DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "No objects to filter", "The list you are trying to filter contains no object. This is not supported.").Forget();
                    Close();
                    return;
                }
                m_FilteringObjects = value;
                m_ListGestion.FilteringObjects = value;
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Event called when applying a filter to the corresponding list
        /// </summary>
        public GenericEvent<bool[]> OnApplyFilters = new GenericEvent<bool[]>();
        #endregion

        #region Public Methods
        public override void Close()
        {
            base.Close();
            PersistentDataManager.FilterConditionsPresets.CurrentPreset = new(m_ListGestion.List.Objects);
        }
        public void ApplyFilters()
        {
            try
            {
                bool[] result = new bool[FilteringObjects.Count];
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = CheckConditions(FilteringObjects[i]);
                }
                OnApplyFilters.Invoke(result);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                DialogBoxManager.OpenScrollable(Core.Enums.DialogBoxType.Error, "Unknown error", e.ToString()).Forget();
            }
        }
        public void ResetFilters()
        {
            OnApplyFilters.Invoke(Enumerable.Repeat(true, FilteringObjects.Count).ToArray());
        }
        public void OpenPresetsWindow()
        {
            var modifier = WindowsManager.OpenModifier(PersistentDataManager.FilterConditionsPresets, this).GetComponent<FilterConditionsPresetCollectionModifier>();
            modifier.FilteringObjects = m_FilteringObjects;
        }
        public void CreatePresetFromSelected()
        {
            var preset = new FilterConditionsPreset("New preset", m_ListGestion.List.ObjectsSelected);
            var modifier = WindowsManager.OpenModifier(preset, this) as FilterConditionsPresetModifier;
            modifier.FilteringObjects = m_FilteringObjects;
            modifier.OnOk.AddListener(() =>
            {
                PersistentDataManager.FilterConditionsPresets.AddPreset(modifier.Object);
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Informational, "Preset created", "The preset has been created and added to the list of presets.").Forget();
            });
        }
        public void LoadConditionsFromPreset()
        {
            var selector = WindowsManager.OpenSelector(PersistentDataManager.FilterConditionsPresets.Presets, this) as FilterConditionsPresetSelector;
            selector.OnOk.AddListener(() =>
            {
                m_ListGestion.List.Add(selector.ObjectsSelected.SelectMany(p => p.Conditions).Where(c => !m_ListGestion.List.Objects.Contains(c)));
            });
        }
        public void SetPreset(FilterConditionsPreset preset)
        {
            m_ListGestion.List.Set(preset.Conditions);
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();

            m_ListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_ListGestion.List.OnAddObject.AddListener(condition => PersistentDataManager.FilterConditionsPresets.CurrentPreset = new(m_ListGestion.List.Objects));
            m_ListGestion.List.OnRemoveObject.AddListener(condition => PersistentDataManager.FilterConditionsPresets.CurrentPreset = new(m_ListGestion.List.Objects));
            m_ListGestion.List.OnUpdateObject.AddListener(condition => PersistentDataManager.FilterConditionsPresets.CurrentPreset = new(m_ListGestion.List.Objects));
            m_ListGestion.List.OnSelect.AddListener((condition) => SetApplyButtonState());
            m_ListGestion.List.OnDeselect.AddListener((condition) => SetApplyButtonState());
            m_ListGestion.List.OnRemoveObject.AddListener((condition) => SetApplyButtonState());
            m_ListGestion.List.OnAddObject.AddListener((condition) => SetApplyButtonState());
        }
        protected virtual bool CheckConditions(BaseData obj)
        {
            bool result = true;
            foreach (var condition in m_ListGestion.List.ObjectsSelected)
            {
                result &= condition.Check(obj);
            }
            return result;
        }
        protected void SetApplyButtonState()
        {
            m_ApplyButton.interactable = m_ListGestion.List.ObjectsSelected.Length > 0;
        }
        #endregion
    }
}