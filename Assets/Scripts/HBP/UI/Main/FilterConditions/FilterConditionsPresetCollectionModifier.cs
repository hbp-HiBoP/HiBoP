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

        #region Public Methods
        public override void OK()
        {
            base.OK();
            Object.SetPresets(m_FilterConditionsPresetListGestion.List.Objects.ToList());
            PersistentDataManager.FilterConditionsPresets.Save();
        }
        #endregion

        #region Protected Methods
        protected override void Initialize()
        {
            base.Initialize();

            m_FilterConditionsPresetListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
        }
        protected override void SetFields(FilterConditionsPresetCollection objectToDisplay)
        {
            m_FilterConditionsPresetListGestion.List.Set(objectToDisplay.Presets);
        }
        #endregion
    }
}