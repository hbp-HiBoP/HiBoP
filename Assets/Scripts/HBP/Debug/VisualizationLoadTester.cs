using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HBP.Data.Visualization;
using HBP.Data;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using HBP.Data.General;
using System.Linq;

public class VisualizationLoadTester : MonoBehaviour
{
	void Update ()
    {
		if (Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log("LoadScene");
            Project project = ApplicationState.ProjectLoaded;
            List<Column> columns = new List<Column>();
            for (int i = 0; i < 2; i++)
            {
                Dataset dataset = project.Datasets[0];
                Protocol protocol = project.Protocols[0];
                Bloc bloc = protocol.Blocs[i];
                ColumnConfiguration configuration = new ColumnConfiguration();
                Column col = new Column(dataset, "TestLabel", protocol, bloc, configuration);
                columns.Add(col);
            }
            Patient patient = project.Patients[0];
            SinglePatientVisualization visualization = new SinglePatientVisualization("VisuTest", columns, patient);
            ApplicationState.Module3D.AddVisualization(visualization);
        }
        if (Input.GetKeyDown(KeyCode.Z)) // to be tested when loading a sp visualization works entirely
        {
            Debug.Log("RemoveScene");
            Debug.Log(ApplicationState.Module3D.Visualizations.Count);
            ApplicationState.Module3D.RemoveVisualization(ApplicationState.Module3D.Visualizations.Last());
        }
	}
}
