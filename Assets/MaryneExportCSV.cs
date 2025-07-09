using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.UI.Tools;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ThirdParty.CielaSpike;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class MaryneExportCSV : MonoBehaviour
{
    readonly string[] m_Protocols = new string[] { "VISU", "LEC1" };
    readonly string[] m_Areas = new string[] { "CTX_OCCIPITAL", "HIPPOCAMP", "HNP" };

    Dictionary<string, List<string>> m_LabelsBySite = new();

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3))
        {
            GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
            LoadingManager.Load(c_ExportCSV(onChangeProgress), onChangeProgress);
        }
    }

    private IEnumerator c_ExportCSV(GenericEvent<float, float, LoadingText> onChangeProgress)
    {
        onChangeProgress.Invoke(0, 0, new LoadingText("Initialize"));

        yield return Ninja.JumpToUnity;

        var inputCSVPath = FileBrowser.GetExistingFileName(new string[] { "csv" }, "Select input CSV file");
        var exportCSVFolder = FileBrowser.GetExistingDirectoryName("Select export folder");

        if (string.IsNullOrEmpty(inputCSVPath) || string.IsNullOrEmpty(exportCSVFolder))
        {
            yield break;
        }

        yield return Ninja.JumpBack;

        m_LabelsBySite.Clear();

        onChangeProgress.Invoke(0, 0, new LoadingText("Reading CSV"));
        Regex csvParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
        if (new FileInfo(inputCSVPath).Exists)
        {
            using (StreamReader sr = new StreamReader(inputCSVPath))
            {
                string line = sr.ReadLine();
                while (!string.IsNullOrEmpty(line = sr.ReadLine()))
                {
                    string[] splits = csvParser.Split(line);
                    string site = splits[0].TrimStart(' ', '"').TrimEnd('"');
                    string labelsString = splits[4].TrimStart(' ', '"').TrimEnd('"');
                    var labels = labelsString.Split(';', System.StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimStart(' ', '"').TrimEnd('"')).ToList();
                    if (labels.Count == 0) continue;
                    m_LabelsBySite.Add(site, labels);
                }
            }
        }

        onChangeProgress.Invoke(0, 0, new LoadingText("Get data infos to read"));

        var datainfoToRead = ApplicationState.ProjectLoaded.Datasets.Where(ds => m_Protocols.Any(d => ds.Protocol.Name == d)).SelectMany(ds => ds.GetIEEGDataInfos()).Where(di => di.Name == "f50f150sm0").ToList();

        int length = datainfoToRead.Count;
        int count = 0;
        foreach (var dataInfo in datainfoToRead)
        {
            onChangeProgress.Invoke((float)count / length, 0, new LoadingText("Loading ", string.Format("{0} ({1})", dataInfo.Name, dataInfo.Dataset.Name) + (dataInfo is PatientDataInfo patientDataInfo ? " for " + patientDataInfo.Patient.Name : ""), " [" + (count + 1) + "/" + length + "]"));
            Data data = DataManager.GetData(dataInfo);
            count++;
        }

        foreach (var protocolName in m_Protocols)
        {
            foreach (var areaName in m_Areas)
            {

            }
        }
    }
}