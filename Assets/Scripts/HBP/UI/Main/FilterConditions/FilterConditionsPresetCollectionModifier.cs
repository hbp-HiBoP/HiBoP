using HBP.Core.Data;
using HBP.Data.Preferences;
using HBP.UI.Tools;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class FilterConditionsPresetCollectionModifier : ObjectModifier<FilterConditionsPresetCollection>
    {
        #region Properties
        [SerializeField] FilterConditionsPresetListGestion m_FilterConditionsPresetListGestion;
        [SerializeField] Button m_ApplyButton;

        private List<BaseData> m_FilteringObjects;
        public List<BaseData> FilteringObjects
        {
            get => m_FilteringObjects;
            set
            {
                m_FilteringObjects = value;
                m_FilterConditionsPresetListGestion.FilteringObjects = value;
            }
        }

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;

                m_FilterConditionsPresetListGestion.Interactable = value;
                m_FilterConditionsPresetListGestion.Modifiable = value;
            }
        }
        #endregion

        #region Events
        public GenericEvent<List<BaseFilterCondition>> OnApplyPreset = new();
        #endregion

        #region Public Methods
        public override void OK()
        {
            base.OK();
            Object.SetPresets(m_FilterConditionsPresetListGestion.List.Objects.ToList());
            PersistentDataManager.FilterConditionsPresets.Save();
        }
        public async void ApplySelectedPresets()
        {
            var result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Warning, "Overwrite conditions", "This will overwrite the currently used conditions with the selected presets. Do you want to continue?", "Overwrite", "Cancel");
            if (result == 0)
            {
                var selectedPresets = m_FilterConditionsPresetListGestion.List.ObjectsSelected;
                if (selectedPresets.Length > 0)
                {
                    OnApplyPreset.Invoke(selectedPresets.SelectMany(p => p.Conditions).ToList());
                    OK();
                }
            }
        }
        #endregion

        #region Protected Methods
        protected override void Initialize()
        {
            base.Initialize();

            m_FilterConditionsPresetListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_FilterConditionsPresetListGestion.List.OnSelect.AddListener((preset) => SetApplyButtonState());
            m_FilterConditionsPresetListGestion.List.OnDeselect.AddListener((preset) => SetApplyButtonState());
            m_FilterConditionsPresetListGestion.List.OnRemoveObject.AddListener((preset) => SetApplyButtonState());
            m_FilterConditionsPresetListGestion.List.OnAddObject.AddListener((preset) => SetApplyButtonState());
        }
        protected override void SetFields(FilterConditionsPresetCollection objectToDisplay)
        {
            m_FilterConditionsPresetListGestion.List.Set(objectToDisplay.Presets);
        }
        protected void SetApplyButtonState()
        {
            FilterConditionsPreset[] presets = m_FilterConditionsPresetListGestion.List.ObjectsSelected;
            m_ApplyButton.interactable = presets.Length > 0 && Interactable;
        }
        #endregion
    }
}