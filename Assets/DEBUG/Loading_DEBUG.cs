using HBP.Data.Experience.Dataset;
using System.Linq;
using UnityEngine;

public class Loading_DEBUG : MonoBehaviour
{
    public HBP.UI.TrialMatrix.TrialMatrix trialMatrix;
    public Texture2D ColorMap;

    public void Load()
    {
        DataInfo dataInfo = ApplicationState.ProjectLoaded.Datasets.First(d => d.Name == "VISU").Data[0];
        Data data = DataManager.GetData(dataInfo);
        string channel = data.UnitByChannel.Keys.ToArray()[0];
        HBP.Data.TrialMatrix.TrialMatrix trialMatrix = new HBP.Data.TrialMatrix.TrialMatrix(dataInfo, channel);
        this.trialMatrix.Set(trialMatrix, ColorMap);
    }
}
