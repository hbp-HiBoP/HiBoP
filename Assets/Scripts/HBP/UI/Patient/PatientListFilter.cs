using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using HBP.Data.Tools;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using HBP.Core.Exceptions;
using HBP.Core.Data;
using HBP.UI.Tools;
using HBP.Core.Tools;

namespace HBP.UI.Main
{
    public class PatientListFilter : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Parent PatientListGestion of this list filter
        /// </summary>
        [SerializeField] private PatientList m_PatientList;
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

        #region Private Methods
        private void Awake()
        {
            m_Button.onClick.AddListener(ApplyFilters);
            OnApplyFilters.AddListener(mask =>
            {
                m_PatientList.MaskList(mask);
                m_PatientList.SortByNone();
            });
        }
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
                DialogBoxManager.Open(DialogBoxManager.AlertType.Error, e.ToString(), e.Message);
            }
        }
        /// <summary>
        /// Parse the whole string and store it to a BooleanExpression object
        /// </summary>
        private void ParseConditions()
        {
            m_BooleanExpression = BooleanExpressionParser.Parse(m_Conditions.text);
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
            Regex conditionRegex = new Regex(@"(.+)([=><]{1})(.+)");
            Match match = conditionRegex.Match(s);
            if (match.Success)
            {
                GroupCollection groups = match.Groups;
                string label = groups[1].Value.Replace("\"", "");
                string deblankedLabel = Regex.Replace(label, "^\\s+", "");
                deblankedLabel = Regex.Replace(deblankedLabel, "\\s+$", "");
                string value = groups[3].Value.Replace("\"", "");
                string deblankedValue = Regex.Replace(value, "^\\s+", "");
                deblankedValue = Regex.Replace(deblankedValue, "\\s+$", "");
                if (deblankedLabel == "NAME")
                {
                    if (groups[2].Value == "=")
                        return patient.Name.ToUpper().Contains(deblankedValue);
                    else if (groups[2].Value == ">")
                        return patient.Name.ToUpper().CompareTo(deblankedValue) > 0;
                    else if (groups[2].Value == "<")
                        return patient.Name.ToUpper().CompareTo(deblankedValue) < 0;
                }
                else if (deblankedLabel == "PLACE")
                {
                    if (groups[2].Value == "=")
                        return patient.Place.ToUpper().Contains(deblankedValue);
                    else if (groups[2].Value == ">")
                        return patient.Place.ToUpper().CompareTo(deblankedValue) > 0;
                    else if (groups[2].Value == "<")
                        return patient.Place.ToUpper().CompareTo(deblankedValue) < 0;
                }
                else if (deblankedLabel == "DATE")
                {
                    if (int.TryParse(deblankedValue, out int dateValue))
                    {
                        if (groups[2].Value == "=")
                            return patient.Date == dateValue;
                        else if (groups[2].Value == ">")
                            return patient.Date > dateValue;
                        else if (groups[2].Value == "<")
                            return patient.Date < dateValue;
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
                            if (groups[2].Value == "=")
                                return tagValue.DisplayableValue.ToUpper().Contains(deblankedValue);
                            else if (groups[2].Value == ">")
                                return tagValue.DisplayableValue.ToUpper().CompareTo(deblankedValue) > 0;
                            else if (groups[2].Value == "<")
                                return tagValue.DisplayableValue.ToUpper().CompareTo(deblankedValue) < 0;
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