using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Data;
using HBP.Core.Data.Container;
using HBP.Core.Tools;

namespace HBP.UI.QuickStart
{
    public class FunctionalPanel : QuickStartPanel
    {
        #region Properties
        [SerializeField] private Dropdown m_DataContainer;
        private Type[] m_DataContainerTypes;
        [SerializeField] private FunctionalDataList m_List;
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();
            
            m_DataContainerTypes = m_DataContainer.Set(typeof(DataContainer), new IEEG());
            m_DataContainer.SetValue(Array.IndexOf(m_DataContainerTypes, typeof(BrainVision)));

            m_DataContainer.onValueChanged.AddListener(OnChangeDataContainerType);
        }
        private void OnChangeDataContainerType(int type)
        {
            foreach (var functionalData in m_List.Objects)
            {
                functionalData.ChangeContainer(m_DataContainerTypes[type]);
            }
            m_List.Refresh();
        }
        #endregion

        #region Public Methods
        public override void Open()
        {
            base.Open();

            var functionalDataObjects = m_List.Objects;
            foreach (var functionalData in functionalDataObjects)
            {
                if (!ApplicationState.ProjectLoaded.Patients.Any(p => functionalData.DataInfo.Patient == p))
                {
                    m_List.Remove(functionalData);
                }
            }
            foreach (var patient in ApplicationState.ProjectLoaded.Patients)
            {
                if (!functionalDataObjects.Any(f => f.DataInfo.Patient == patient))
                {
                    m_List.Add(new FunctionalData(patient, m_DataContainerTypes[m_DataContainer.value]));
                }
            }
        }
        public override bool OpenNextPanel()
        {
            if (m_List.Objects.All(o => !o.DataInfo.IsOk))
            {
                DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "No valid data", "At least one data must be valid in order to continue.");
                return false;
            }
            Dataset dataset = new Dataset("QuickStart", ApplicationState.ProjectLoaded.Protocols[0], m_List.Objects.Select(f => f.DataInfo));
            ApplicationState.ProjectLoaded.SetDatasets(new Dataset[] { dataset });
            return base.OpenNextPanel();
        }
        public override bool OpenPreviousPanel()
        {
            ApplicationState.ProjectLoaded.SetDatasets(new Dataset[0]);
            return base.OpenPreviousPanel();
        }
        #endregion
    }
}