using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using HBP.UI.Main;
using HBP.Core.Data;

namespace HBP.UI.Tools
{
    public abstract class ListFilter : DialogWindow
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
        }
        #endregion

        #region Private Methods
        protected void ApplyFilters()
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
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, e.ToString(), e.Message).Forget();
            }
        }
        protected abstract bool CheckConditions(BaseData obj);
        #endregion
    }
}