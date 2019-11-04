using HBP.Data.Visualization;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DebugBenjamin : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            ApplicationState.ProjectLoaded.Settings.SitesTags.Add(new HBP.Data.Tags.StringTag("MarsAtlas"));
            ApplicationState.ProjectLoaded.Settings.SitesTags.Add(new HBP.Data.Tags.StringTag("Freesurfer"));
            ApplicationState.ProjectLoaded.Settings.SitesTags.Add(new HBP.Data.Tags.BoolTag("Salut"));
            ApplicationState.ProjectLoaded.Patients.First().Sites.First().Tags.Add(new HBP.Data.Tags.StringTagValue(ApplicationState.ProjectLoaded.Settings.SitesTags[0] as HBP.Data.Tags.StringTag, "Gyrus"));
            ApplicationState.ProjectLoaded.Patients.First().Sites.First().Tags.Add(new HBP.Data.Tags.StringTagValue(ApplicationState.ProjectLoaded.Settings.SitesTags[1] as HBP.Data.Tags.StringTag, "Toast"));
            ApplicationState.ProjectLoaded.Patients.First().Sites.First().Tags.Add(new HBP.Data.Tags.BoolTagValue(ApplicationState.ProjectLoaded.Settings.SitesTags[2] as HBP.Data.Tags.BoolTag, true));
            ApplicationState.ProjectLoaded.AddVisualization(new Visualization("debug", ApplicationState.ProjectLoaded.Patients, new Column[] { new AnatomicColumn("column", new BaseConfiguration()), new AnatomicColumn("column", new BaseConfiguration()) }));
            ApplicationState.Module3D.LoadScenes(ApplicationState.ProjectLoaded.Visualizations);
        }
    }
}
