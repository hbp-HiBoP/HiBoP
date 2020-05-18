﻿using HBP.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Tools.CSharp.BooleanExpressionParser;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI
{
    public class PatientListFilter : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Parent PatientListGestion of this list filter
        /// </summary>
        private PatientList m_PatientList;
        /// <summary>
        /// Conditions to be used when filtering the corresponding list
        /// </summary>
        [SerializeField] private InputField m_Conditions;
        /// <summary>
        /// When pressed, filter the corresponding list
        /// </summary>
        [SerializeField] private Button m_Button;
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
        /// <summary>
        /// Initialize this MonoBehaviour
        /// </summary>
        public void Initialize(PatientList patientList)
        {
            m_PatientList = patientList;
            m_Button.onClick.AddListener(ApplyFilters);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Apply the filters given the input conditions
        /// </summary>
        private void ApplyFilters()
        {
            try
            {
                ParseConditions();
                ReadOnlyCollection<Patient> patients = m_PatientList.Objects;
                bool[] result = new bool[patients.Count];
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = CheckConditions(patients[i]);
                }
                OnApplyFilters.Invoke(result);
            }
            catch (Exception e)
            {
                ApplicationState.DialogBoxManager.Open(Tools.Unity.DialogBoxManager.AlertType.Error, e.ToString(), e.Message);
            }
        }
        /// <summary>
        /// Parse the whole string and store it to a BooleanExpression object
        /// </summary>
        private void ParseConditions()
        {
            m_BooleanExpression = Parser.Parse(m_Conditions.text);
        }
        /// <summary>
        /// Check all the set conditions for a specific patient
        /// </summary>
        /// <param name="patient">Patient to check</param>
        /// <returns>True if the conditions are met</returns>
        private bool CheckConditions(Patient patient)
        {
            foreach (var booleanValue in m_BooleanExpression.GetAllBooleanValuesUnderThisOne())
            {
                booleanValue.SetBooleanValue((s) => ParseConditionAndCheckValue(patient, s));
            }
            return m_BooleanExpression.Evaluate();
        }
        /// <summary>
        /// Parse the string containing the conditions and get the value 
        /// </summary>
        /// <param name="patient">Patient to check</param>
        /// <param name="s">String to be parsed</param>
        /// <returns>True if the patient matches the set of conditions</returns>
        private bool ParseConditionAndCheckValue(Patient patient, string s)
        {
            s = s.ToUpper();
            if (s.Contains("=") || s.Contains(">") || s.Contains("<"))
            {
                string[] elements = s.Split('=', '<', '>');
                if (elements.Length == 2)
                {
                    string label = elements[0].Replace("\"", "");
                    string deblankedLabel = System.Text.RegularExpressions.Regex.Replace(label, "^\\s+", "");
                    deblankedLabel = System.Text.RegularExpressions.Regex.Replace(deblankedLabel, "\\s+$", "");
                    string value = elements[1].Replace("\"", "");
                    string deblankedValue = System.Text.RegularExpressions.Regex.Replace(value, "^\\s+", "");
                    deblankedValue = System.Text.RegularExpressions.Regex.Replace(deblankedValue, "\\s+$", "");
                    if (deblankedLabel == "NAME")
                    {
                        return patient.Name.ToUpper().Contains(deblankedValue);
                    }
                    else if (deblankedLabel == "PLACE")
                    {
                        return patient.Place.ToUpper().Contains(deblankedValue);
                    }
                    else if (deblankedLabel == "DATE")
                    {
                        if (int.TryParse(deblankedValue, out int dateValue))
                        {
                            return patient.Date == dateValue;
                        }
                    }
                    else
                    {
                        BaseTag tag = ApplicationState.ProjectLoaded.Preferences.PatientsTags.FirstOrDefault(t => t.Name.ToUpper() == deblankedLabel);
                        if (tag == null) tag = ApplicationState.ProjectLoaded.Preferences.GeneralTags.FirstOrDefault(t => t.Name.ToUpper() == deblankedLabel);
                        if (tag != null)
                        {
                            BaseTagValue tagValue = patient.Tags.FirstOrDefault(t => t.Tag == tag);
                            if (tagValue != null)
                            {
                                return tagValue.DisplayableValue.ToUpper().Contains(deblankedValue);
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (s == "TRUE")
                {
                    return true;
                }
                else if (s == "FALSE")
                {
                    return false;
                }
            }
            throw new InvalidConditionException(s);
        }
        #endregion
    }

}