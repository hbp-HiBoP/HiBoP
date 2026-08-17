using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.UI.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class MultipleSiteTagsFilterConditionSubModifier : SubModifier<MultipleSiteTagsFilterCondition>
    {
        #region Properties

        [SerializeField] SingleTagFilterListGestion m_TagFilterListGestion;

        protected List<object> m_FilteringObjects;

        public List<object> FilteringObjects
        {
            get => m_FilteringObjects;
            set
            {
                m_FilteringObjects = value;
                if (m_TagFilterListGestion != null)
                {
                    m_TagFilterListGestion.FilteringObjects = value;
                }
            }
        }

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                if (m_TagFilterListGestion != null)
                {
                    m_TagFilterListGestion.Interactable = value;
                    m_TagFilterListGestion.Modifiable = value;
                }
            }
        }

        #endregion

        #region Public Methods

        public override void Initialize()
        {
            base.Initialize();

            m_TagFilterListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_TagFilterListGestion.List.OnAddObject.AddListener(AddTagFilter);
            m_TagFilterListGestion.List.OnRemoveObject.AddListener(RemoveTagFilter);
            m_TagFilterListGestion.List.OnUpdateObject.AddListener(UpdateTagFilter);
        }

        #endregion

        #region Private Methods

        protected override void SetFields(MultipleSiteTagsFilterCondition objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_TagFilterListGestion.List.Set(objectToDisplay.TagFilters);
        }

        void AddTagFilter(SingleTagFilter tagFilter)
        {
            if (!Object.TagFilters.Contains(tagFilter))
            {
                Object.TagFilters.Add(tagFilter);
            }
        }

        void RemoveTagFilter(SingleTagFilter tagFilter)
        {
            if (Object.TagFilters.Contains(tagFilter))
            {
                Object.TagFilters.Remove(tagFilter);
            }
        }

        void UpdateTagFilter(SingleTagFilter tagFilter)
        {
            int index = Object.TagFilters.FindIndex(tf => tf.ID == tagFilter.ID);
            if (index != -1)
            {
                Object.TagFilters[index] = tagFilter;
            }
        }

        #endregion
    }
}
