using HBP.Data.Database;
using HBP.UI.Tools;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Database
{
    public class LocalizerDatabaseParametersSubModifier : SubModifier<LocalizerDatabaseParameters>
    {
        #region Properties
        [SerializeField] InputField m_FrequenciesInputField;
        [SerializeField] InputField m_TemporalSmoothingsInputField;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_FrequenciesInputField.onValueChanged.AddListener(OnChangeFrequencies);
            m_TemporalSmoothingsInputField.onValueChanged.AddListener(OnChangeTemporalSmoothings);
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(LocalizerDatabaseParameters objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            m_FrequenciesInputField.text = string.Join(",", objectToDisplay.Frequencies);
            m_TemporalSmoothingsInputField.text = string.Join(",", objectToDisplay.TemporalSmoothings);
        }
        #endregion

        #region Private Methods
        private void OnChangeFrequencies(string value)
        {
            Object.Frequencies = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }
        private void OnChangeTemporalSmoothings(string value)
        {
            Object.TemporalSmoothings = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }
        #endregion
    }
}