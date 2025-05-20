using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using HBP.UI.Main;
using HBP.Core.Data;
using HBP.Data.Preferences;
using System.Linq;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using HBP.Core.Tools;
using System.Threading;

namespace HBP.UI.Tools
{
    public class ListFilter : DialogWindow
    {
        #region Properties
        [SerializeField] protected FilterConditionListGestion m_ListGestion;
        [SerializeField] protected Button m_ApplyButton;

        protected List<object> m_FilteringObjects;
        public List<object> FilteringObjects
        {
            get => m_FilteringObjects;
            set
            {
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
            if (m_FilteringObjects.Count > 0)
            {
                PersistentDataManager.FilterConditionsPresets.SetCurrentPreset(new FilterConditionsPreset(m_ListGestion.List.Objects), m_FilteringObjects[0].GetType());
            }
        }
        public void ApplyFilters()
        {
            LoadingManager.Load((update, token) => ApplyFiltersAsync(update, token), false);
        }
        public virtual void ResetFilters()
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
                if (m_FilteringObjects.Count == 0)
                {
                    DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Preset not created", "The preset could not be created because the list you are trying to filter contains no object. This is not supported.").Forget();
                    return;
                }

                PersistentDataManager.FilterConditionsPresets.AddPreset(modifier.Object, m_FilteringObjects[0].GetType());
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Informational, "Preset created", "The preset has been created and added to the list of presets.").Forget();
            });
        }
        public void LoadConditionsFromPreset()
        {
            if (m_FilteringObjects.Count == 0)
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Preset not loaded", "The presets can not be loaded because the list you are trying to filter contains no object. This is not supported.").Forget();
                return;
            }

            var selector = WindowsManager.OpenSelector(PersistentDataManager.FilterConditionsPresets.GetPresets(m_FilteringObjects[0].GetType()), this) as FilterConditionsPresetSelector;
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
            m_ListGestion.List.OnAddObject.AddListener(condition => PersistentDataManager.FilterConditionsPresets.SetCurrentPreset(new(m_ListGestion.List.Objects), m_FilteringObjects[0].GetType()));
            m_ListGestion.List.OnRemoveObject.AddListener(condition => PersistentDataManager.FilterConditionsPresets.SetCurrentPreset(new(m_ListGestion.List.Objects), m_FilteringObjects[0].GetType()));
            m_ListGestion.List.OnUpdateObject.AddListener(condition => PersistentDataManager.FilterConditionsPresets.SetCurrentPreset(new(m_ListGestion.List.Objects), m_FilteringObjects[0].GetType()));
            m_ListGestion.List.OnSelect.AddListener((condition) => SetApplyButtonState());
            m_ListGestion.List.OnDeselect.AddListener((condition) => SetApplyButtonState());
            m_ListGestion.List.OnRemoveObject.AddListener((condition) => SetApplyButtonState());
            m_ListGestion.List.OnAddObject.AddListener((condition) => SetApplyButtonState());
        }
        protected virtual async UniTask ApplyFiltersAsync(Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            await UniTask.SwitchToThreadPool();

            bool[] result = new bool[FilteringObjects.Count];
            for (int i = 0; i < result.Length; i++)
            {
                updateProgress.Invoke((float)i / result.Length, 0, new LoadingText("Filtering objects"));
                result[i] = CheckConditions(FilteringObjects[i]);
                token.ThrowIfCancellationRequested();
            }
            updateProgress.Invoke(1, 0, new LoadingText("Filtered"));

            await UniTask.SwitchToMainThread();

            OnApplyFilters.Invoke(result);
        }
        protected virtual bool CheckConditions(object obj)
        {
            bool result = true;
            foreach (var condition in m_ListGestion.List.ObjectsSelected)
            {
                result &= condition.Check(obj);
            }
            return result;
        }
        protected virtual void SetApplyButtonState()
        {
            m_ApplyButton.interactable = m_ListGestion.List.ObjectsSelected.Length > 0;
        }
        #endregion
    }
}