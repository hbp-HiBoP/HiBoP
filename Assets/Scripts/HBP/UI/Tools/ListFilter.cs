using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using HBP.UI.Main;
using HBP.Core.Data;
using HBP.Data.Preferences;
using System.Linq;

namespace HBP.UI.Tools
{
    public class ListFilter : DialogWindow
    {
        #region Properties
        [SerializeField] protected FilterConditionListGestion m_ListGestion;

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
        public override void OK()
        {
            base.OK();
            ApplyFilters();
            PersistentDataManager.FilterConditionsPresets.CurrentPreset = new(m_ListGestion.List.Objects);
        }
        public override void Close()
        {
            base.Close();
            OnApplyFilters.Invoke(Enumerable.Repeat(true, FilteringObjects.Count).ToArray());
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
        public void OpenPresetsWindow()
        {
            var modifier = WindowsManager.OpenModifier(PersistentDataManager.FilterConditionsPresets, this).GetComponent<FilterConditionsPresetCollectionModifier>();
            modifier.FilteringObjects = m_FilteringObjects;
            modifier.OnApplyPreset.AddListener(conditions =>
            {
                m_ListGestion.List.Set(conditions);
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
        }
        protected virtual bool CheckConditions(BaseData obj)
        {
            bool result = true;
            foreach (var condition in m_ListGestion.List.Objects)
            {
                result &= condition.Check(obj);
            }
            return result;
        }
        #endregion
    }
}