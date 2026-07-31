using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.Core.Preferences;
using HBP.UI.Tools;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class FilterConditionsPresetModifier : ObjectModifier<FilterConditionsPreset>
    {
        #region Properties

        [SerializeField] InputField m_NameInputField;
        [SerializeField] FilterConditionListGestion m_FilterConditionsListGestion;

        protected List<object> m_FilteringObjects;

        public List<object> FilteringObjects
        {
            get => m_FilteringObjects;
            set
            {
                m_FilteringObjects = value;
                m_FilterConditionsListGestion.FilteringObjects = value;
            }
        }

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                m_NameInputField.interactable = value;
                m_FilterConditionsListGestion.Interactable = value;
            }
        }

        #endregion

        #region Private Methods

        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onValueChanged.AddListener(name => ObjectTemp.Name = name);

            m_FilterConditionsListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_FilterConditionsListGestion.List.OnAddObject.AddListener(AddCondition);
            m_FilterConditionsListGestion.List.OnRemoveObject.AddListener(RemoveCondition);
            m_FilterConditionsListGestion.List.OnUpdateObject.AddListener(UpdateCondition);
        }

        protected override void SetFields(FilterConditionsPreset preset)
        {
            m_NameInputField.text = preset.Name;
            m_FilterConditionsListGestion.List.Set(preset.Conditions);
        }

        private void AddCondition(BaseFilterCondition condition)
        {
            if (!ObjectTemp.Conditions.Contains(condition))
            {
                ObjectTemp.Conditions.Add(condition);
            }
        }

        private void RemoveCondition(BaseFilterCondition condition)
        {
            if (ObjectTemp.Conditions.Contains(condition))
            {
                ObjectTemp.Conditions.Remove(condition);
            }
        }

        private void UpdateCondition(BaseFilterCondition condition)
        {
            int index = ObjectTemp.Conditions.IndexOf(condition);
            if (index != -1)
            {
                ObjectTemp.Conditions[index] = condition;
            }
        }

        #endregion

        #region Public Methods

        public async void CopyFromCurrent()
        {
            int result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Warning, "Overwrite conditions", "This will overwrite the current preset with all the conditions of the currently used filter. Are you sure you want to do this?", "Overwrite", "Cancel");
            if (result == 0)
            {
                m_ObjectTemp.Conditions = new List<BaseFilterCondition>(PersistentDataManager.FilterConditionsPresets.GetCurrentPreset(m_FilteringObjects[0].GetType()).Conditions.DeepClone());
                SetFields(m_ObjectTemp);
            }
        }

        #endregion
    }
}
