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
        public List<T> Objects { get; set; }
        /// <summary>
        /// Conditions to be used when filtering the corresponding list
        /// </summary>
        [SerializeField] private InputField m_Conditions;
        /// <summary>
        /// Boolean expression parsed from the string
        /// </summary>
        private BooleanExpression m_BooleanExpression;
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
        /// <summary>
        /// Apply the filters given the input conditions
        /// </summary>
        protected void ApplyFilters()
        {
            try
            {
                ParseConditions();
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
        protected void ParseConditions()
        {
            m_BooleanExpression = BooleanExpressionParser.Parse(m_Conditions.text);
        }
        protected bool CheckConditions(T obj)
        {
            foreach (var booleanValue in m_BooleanExpression.GetAllBooleanValuesUnderThisOne())
            {
                booleanValue.SetBooleanValue((s) => ParseConditionAndCheckValue(obj, s));
            }
            return m_BooleanExpression.Evaluate();
        }
        protected abstract bool ParseConditionAndCheckValue(T obj, string s);
        #endregion
    }
}