using UnityEngine;
using UnityEngine.Profiling;
using System.IO;
using HBP.Data.Localizer;

public class Test : MonoBehaviour
{
    int nb = 10;
    // Use this for initialization
    void Start()
    {
        Load();
	}

    void Load()
    {
        string path = "D:\\UnityProjects\\ElanTest\\Assets\\Data\\LYONNEURO_2016_SQUt_VISU_f8f24_ds8_sm250.eeg";
        EEG[] myEEGFiles = new EEG[nb];
        for (int i = 0; i < nb; i++)
        {
            Profiler.BeginSample("Create file");
            myEEGFiles[i] = new EEG(path);
            myEEGFiles[i].ReadData();
            Profiler.EndSample();
        }
    }

}
