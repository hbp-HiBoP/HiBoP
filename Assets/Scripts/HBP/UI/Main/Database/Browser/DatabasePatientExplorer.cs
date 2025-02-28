using HBP.Core.Data;
using HBP.UI.Tools;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Database
{
    public class DatabasePatientExplorer : MonoBehaviour
    {
        #region Properties
        [SerializeField] private InputField m_NameInputField;
        [SerializeField] private InputField m_PlaceInputField;
        [SerializeField] private InputField m_DateInputField;

        [SerializeField] private AnatomicalDataExplorer m_AnatomicalDataExplorer;
        [SerializeField] private FunctionalDataExplorer m_FunctionalDataExplorer;
        #endregion

        #region Public Methods
        public void Initialize(WindowsReferencer windowsReferencer)
        {
            m_AnatomicalDataExplorer.Initialize(windowsReferencer);
            m_FunctionalDataExplorer.Initialize(windowsReferencer);
        }
        public void SetFields()
        {
            m_AnatomicalDataExplorer.SetFields();
            m_FunctionalDataExplorer.SetFields();
        }
        public void Set(Patient patient)
        {
            m_NameInputField.text = patient.Name;
            m_DateInputField.text = patient.Date.ToString();
            m_PlaceInputField.text = patient.Place;

            m_AnatomicalDataExplorer.Set(patient);
            m_FunctionalDataExplorer.Set(patient);
        }
        #endregion
    }
}