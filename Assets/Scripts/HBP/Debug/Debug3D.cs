using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HBP.Data.Visualization;
using HBP.Data;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using HBP.Data.General;
using System.Linq;
using UnityEngine.EventSystems;

namespace HBP.UI.Module3D.DBG
{
    public class Debug3D : MonoBehaviour
    {
        public GameObject SceneWindowPrefab;

        private void Awake()
        {
            ApplicationState.Module3D.OnAddScene.AddListener((scene) =>
            {
                Scene3DWindow sceneWindow = Instantiate(SceneWindowPrefab, transform).GetComponent<Scene3DWindow>();
                sceneWindow.Initialize(scene);
            });
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                Debug.Log("LoadSingle");
                Project project = ApplicationState.ProjectLoaded;
                List<Column> columns = new List<Column>();
                for (int i = 0; i < 3; i++)
                {
                    Dataset dataset = project.Datasets[0];
                    Protocol protocol = project.Protocols[0];
                    Bloc bloc = protocol.Blocs[i % 2];
                    ColumnConfiguration configuration = new ColumnConfiguration();
                    Column col = new Column(dataset, "TestLabel", protocol, bloc, configuration);
                    columns.Add(col);
                }
                Data.Patient patient = project.Patients[0];
                SinglePatientVisualization visualization = new SinglePatientVisualization("VisuTest", columns, patient);
                ApplicationState.Module3D.AddVisualization(visualization);
            }
            if (Input.GetKeyDown(KeyCode.B))
            {
                Debug.Log("LoadMulti");
                Project project = ApplicationState.ProjectLoaded;
                List<Column> columns = new List<Column>();
                for (int i = 0; i < 1; i++)
                {
                    Dataset dataset = project.Datasets[0];
                    Protocol protocol = project.Protocols[0];
                    Bloc bloc = protocol.Blocs[i];
                    ColumnConfiguration configuration = new ColumnConfiguration();
                    Column col = new Column(dataset, "TestLabel", protocol, bloc, configuration);
                    columns.Add(col);
                }
                Data.Patient[] patients = { project.Patients[0], project.Patients[1] };
                MultiPatientsVisualization visualization = new MultiPatientsVisualization("VisuTest", columns, patients);
                ApplicationState.Module3D.AddVisualization(visualization);
            }
            if (Input.GetKeyDown(KeyCode.Z)) // to be tested when loading a sp visualization works entirely
            {
                Debug.Log("RemoveScene");
                ApplicationState.Module3D.RemoveVisualization(ApplicationState.Module3D.Visualizations.Last());
            }
        }

        public void RaycastToCursor()
        {
            PointerEventData data = new PointerEventData(EventSystem.current);
            data.position = Input.mousePosition;
            List<RaycastResult> res = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, res);
            foreach (var item in res)
            {
                Debug.Log(item.gameObject.name);
            }
        }
    }
}