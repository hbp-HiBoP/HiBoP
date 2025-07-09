using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.UI.Tools;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ThirdParty.CielaSpike;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class MaryneExportCSV : MonoBehaviour
{
    readonly string[] m_ProtocolNames = new string[] { "VISU", "LEC1" };
    readonly string[] m_Areas = new string[] { "CTX_OCCIPITAL", "HIPPOCAMP", "HNP" };
    readonly string[] m_DataTypes = new string[] { "f50f150sm0" };

    public class SiteStruct
    {
        public string Site { get; set; }
        public Patient Patient { get; set; }
        public List<string> Labels { get; set; }

        public SiteStruct(string site, Patient patient, List<string> labels)
        {
            Site = site;
            Patient = patient;
            Labels = labels;
        }
    }

    private List<SiteStruct> m_SiteStructs;

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

        if (string.IsNullOrEmpty(inputCSVPath) || string.IsNullOrEmpty(exportCSVFolder)) yield break;

        yield return Ninja.JumpBack;

        onChangeProgress.Invoke(0, 0, new LoadingText("Reading CSV"));
        m_SiteStructs = GenerateSiteStructs(inputCSVPath);

        onChangeProgress.Invoke(0, 0, new LoadingText("Get data infos to read"));
        ReadDataInfo(onChangeProgress);

        onChangeProgress.Invoke(1, 0, new LoadingText("Exporting CSV"));
        ExportCSV(exportCSVFolder);

        yield return Ninja.JumpToUnity;

        DialogBoxManager.Open(DialogBoxManager.AlertType.Informational, "Export completed", "The CSV files have been successfully exported to the selected folder.");
    }

    public List<SiteStruct> GenerateSiteStructs(string inputCSVPath)
    {
        List<SiteStruct> result = new List<SiteStruct>();
        Regex csvParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
        if (new FileInfo(inputCSVPath).Exists)
        {
            using (StreamReader sr = new StreamReader(inputCSVPath))
            {
                string line = sr.ReadLine();
                while (!string.IsNullOrEmpty(line = sr.ReadLine()))
                {
                    string[] splits = csvParser.Split(line);
                    string siteID = splits[0].TrimStart(' ', '"').TrimEnd('"');
                    var splitSiteID = siteID.Split('_');
                    string site = splitSiteID[3];
                    string patientID = splitSiteID[0] + "_" + splitSiteID[1] + "_" + splitSiteID[2];
                    Patient patient = ApplicationState.ProjectLoaded.Patients.FirstOrDefault(p => p.ID == patientID);
                    string labelsString = splits[4].TrimStart(' ', '"').TrimEnd('"');
                    var labels = labelsString.Split(';', System.StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimStart(' ', '"').TrimEnd('"')).ToList();
                    if (labels.Count == 0) continue;
                    result.Add(new(site, patient, labels));
                }
            }
        }
        return result;
    }
    public void ReadDataInfo(GenericEvent<float, float, LoadingText> onChangeProgress)
    {
        var datainfoToRead = ApplicationState.ProjectLoaded.Datasets.Where(ds => m_ProtocolNames.Any(d => ds.Protocol.Name == d)).SelectMany(ds => ds.Data).Where(di => m_DataTypes.Any(dt => dt == di.Name)).ToList();
        int length = datainfoToRead.Count;
        int count = 0;
        foreach (var dataInfo in datainfoToRead)
        {
            onChangeProgress.Invoke((float)count / length, 0, new LoadingText("Loading ", string.Format("{0} ({1})", dataInfo.Name, dataInfo.Dataset.Name) + (dataInfo is PatientDataInfo patientDataInfo ? " for " + patientDataInfo.Patient.Name : ""), " [" + (count + 1) + "/" + length + "]"));
            Data data = DataManager.GetData(dataInfo);
            count++;
        }
    }
    public void ExportCSV(string exportDirectory)
    {
        foreach (var protocolName in m_ProtocolNames)
        {
            Protocol protocol = ApplicationState.ProjectLoaded.Protocols.FirstOrDefault(p => p.Name == protocolName);
            List<float> times = new List<float>();
            int start = protocol.Blocs[0].MainSubBloc.Window.Start;
            int timeLength = protocol.Blocs[0].MainSubBloc.Window.Length;
            int numberOfSamples = Mathf.RoundToInt(new Frequency(64).ConvertToNumberOfSamples(timeLength));
            for (int i = 0; i < numberOfSamples; i++)
            {
                times.Add((float)i / (numberOfSamples - 1) * timeLength + start);
            }
            string header = $"Patient,Site,Area,Protocol,Bloc,{string.Join(',', times.Select(t => t.ToString("F2", CultureInfo.InvariantCulture)))}";
            foreach (var areaName in m_Areas)
            {
                StringBuilder csvBuilder = new StringBuilder();
                csvBuilder.AppendLine(header);
                var areaSites = m_SiteStructs.Where(s => s.Labels.Any(l => l.Equals(areaName))).ToList();
                foreach (var bloc in protocol.Blocs)
                {
                    var blocSites = areaSites.Where(s => s.Labels.Any(l => l.Equals($"{protocol.Name}_{bloc.Name}")));
                    foreach (var site in blocSites)
                    {
                        var statistics = DataManager.GetStatistics(ApplicationState.ProjectLoaded.Datasets.FirstOrDefault(ds => ds.Protocol == protocol).Data.FirstOrDefault(di => di is PatientDataInfo patientDataInfo && patientDataInfo.Patient == site.Patient && m_DataTypes.Any(dt => dt == di.Name)), bloc, site.Site);
                        var values = statistics.Trial.ChannelSubTrialBySubBloc[bloc.MainSubBloc].Values;
                        csvBuilder.AppendLine($"{site.Patient.ID},{site.Site},{areaName},{protocol.Name},{bloc.Name},{string.Join(',', values.Select(v => v.ToString("F2", CultureInfo.InvariantCulture)))}");
                        //csvBuilder.AppendLine($"{site.Patient.ID},{site.Site},{areaName},{protocol.Name},{bloc.Name},{string.Join(',', Enumerable.Repeat(0, numberOfSamples).Select(v => v.ToString("F2", CultureInfo.InvariantCulture)))}");
                    }
                }
                // Write to file
                string fileName = $"{protocolName}_{areaName}.csv";
                string filePath = Path.Combine(exportDirectory, fileName);
                using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    sw.Write(csvBuilder.ToString());
                }
            }
        }
    }
}