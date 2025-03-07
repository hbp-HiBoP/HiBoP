using HBP.Data.Tools;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;

namespace HBP.UI.Tools
{
    public abstract class ListFilter<T> : DialogWindow
    {
        #region Properties
        protected List<T> m_Objects;
        public List<T> Objects
        {
            get => m_Objects;
            set
            {
                m_Objects = value;
                SetObjects();
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
                bool[] result = new bool[Objects.Count];
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = CheckConditions(Objects[i]);
                }
                OnApplyFilters.Invoke(result);
            }
            catch (Exception e)
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, e.ToString(), e.Message).Forget();
            }
        }
        protected abstract bool CheckConditions(T obj);
        protected abstract void SetObjects();
        #endregion
    }
}