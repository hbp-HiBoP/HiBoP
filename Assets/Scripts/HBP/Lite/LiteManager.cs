using HBP.Data;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using HBP.Data.Visualization;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.Lite
{
    public class LiteManager : MonoBehaviour
    {
        #region Properties
        private Project m_Project;
        private List<Patient> m_Patients;
        private Protocol m_Protocol;
        private Dataset m_Dataset;
        private Visualization m_Visualization;
        #endregion

        #region Private Methods
        private void Awake()
        {
            ApplicationState.ProjectLoaded = new Project();
            m_Patients = new List<Patient>();
            m_Protocol = new Protocol("Protocol", new Bloc[0]);
            m_Dataset = new Dataset("Dataset", m_Protocol, new DataInfo[0]);
            m_Visualization = new Visualization("Visualization", m_Patients, new Column[] { new IEEGColumn("Column", new BaseConfiguration(), m_Dataset, "Data", new Bloc(), new DynamicConfiguration()) });
            m_Project = new Project(new ProjectPreferences("Project"), m_Patients, new Group[0], new Protocol[] { m_Protocol }, new Dataset[] { m_Dataset }, new Visualization[] { m_Visualization });
            ApplicationState.ProjectLoaded = m_Project;
            ApplicationState.Module3D.LoadScenes(new Visualization[] { m_Visualization });
        }
        #endregion
    }
}