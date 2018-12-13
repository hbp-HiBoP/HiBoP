using HBP.Data.Experience.Dataset;
using HBP.Data.Informations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Loading_DEBUG : MonoBehaviour
{
    public Texture2D colorMap;
    public HBP.UI.TrialMatrix.Grid.TrialMatrixGrid trialMatrixGrid;

    public void Load()
    {
        Dataset[] datasets = ApplicationState.ProjectLoaded.Datasets.ToArray();
        List<ChannelStruct> channels = new List<ChannelStruct>();
        List<DataStruct> datas = new List<DataStruct>();
        foreach (var dataset in datasets)
        {
            foreach (var dataInfo in dataset.Data)
            {
                DataStruct data = new DataStruct(dataset, dataInfo.Name);
                if (!datas.Contains(data)) datas.Add(data);
                Elan.ElanFile eeg = new Elan.ElanFile(dataInfo.EEG, false);
                channels.AddRange(eeg.Channels.Select(c => new ChannelStruct(c.Label, dataInfo.Patient)).Where(i => !channels.Contains(i)));
            }
        }
        channels = channels.Take(2).ToList();
        datas = datas.Where(d => d.Dataset.Protocol.Name == "VISU").ToList();
        UnityEngine.Profiling.Profiler.BeginSample("Data");
        HBP.Data.TrialMatrix.Grid.TrialMatrixGrid trialMatrixGridData = new HBP.Data.TrialMatrix.Grid.TrialMatrixGrid(channels.ToArray(), datas.ToArray());
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("UI");
        trialMatrixGrid.Display(trialMatrixGridData, colorMap);
        UnityEngine.Profiling.Profiler.EndSample();

    }
}