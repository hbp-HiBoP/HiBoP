using HBP.Data.Visualization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugBenjamin : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            ApplicationState.ProjectLoaded.AddVisualization(new Visualization("debug", ApplicationState.ProjectLoaded.Patients, new Column[] { new AnatomicColumn("column", new BaseConfiguration()), new AnatomicColumn("column", new BaseConfiguration()) }));
            ApplicationState.Module3D.LoadScenes(ApplicationState.ProjectLoaded.Visualizations);
        }
    }
}
