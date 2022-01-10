using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine.Events;
using Tools.Unity;
using System.IO;
using Tools.CSharp;
using System.Collections.ObjectModel;
using System.Collections;
using CielaSpike;
using System.Text.RegularExpressions;

namespace HBP.Data.Experience.Dataset
{
    /// <summary>
    /// Class which contains data of the experiment.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>ID</b></term>
    /// <description>Unique identifier.</description>
    /// </item>
    /// <item>
    /// <term><b>Name</b></term>
    /// <description>Name of the dataset.</description>
    /// </item>
    /// <item>
    /// <term><b>Protocol</b></term>
    /// <description>Protocol used during the experiment.</description>
    /// </item>
    /// <item>
    /// <term><b>Data</b></term>
    /// <description>DataInfo of the dataset.</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract]
    public class Dataset : BaseData, ILoadable<Dataset>, ILoadableFromDatabase<Dataset>, INameable
    {
        #region Properties
        public const string EXTENSION = ".dataset";
        /// <summary>
        /// Name of the dataset.
        /// </summary>
        [DataMember] public string Name { get; set; }

        [DataMember(Name = "Protocol", Order = 3)] string m_ProtocolID;
        Protocol.Protocol m_Protocol;
        /// <summary>
        /// Protocol used during the experiment.
        /// </summary>
        public Protocol.Protocol Protocol
        {
            get
            {
                return m_Protocol;
            }
            set
            {
                m_Protocol = value;
                UpdateDataStates();
            }
        }

        Dictionary<DataInfo, UnityAction> m_ActionByDataInfo;
        [DataMember(Order = 4, Name = "Data")] List<DataInfo> m_Data;
        /// <summary>
        /// DataInfo of the dataset.
        /// </summary>
        public ReadOnlyCollection<DataInfo> Data
        {
            get
            {
                return new ReadOnlyCollection<DataInfo>(m_Data);
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new Dataset instance.
        /// </summary>
        /// <param name="name">Name of the dataset</param>
        /// <param name="protocol">Protocol used during the experiment</param>
        /// <param name="data">DataInfo of the dataset</param>
        /// <param name="ID">Unique identifier</param>
        public Dataset(string name, Protocol.Protocol protocol, IEnumerable<DataInfo> data, string ID) : base(ID)
        {
            Name = name;
            Protocol = protocol;
            SetData(data);
        }
        /// <summary>
        /// Create a new Dataset instance.
        /// </summary>
        /// <param name="name">Name of the dataset</param>
        /// <param name="protocol">Protocol used during the experiment</param>
        /// <param name="data">DataInfo of the dataset</param>
        public Dataset(string name, Protocol.Protocol protocol, IEnumerable<DataInfo> data) : base()
        {
            Name = name;
            Protocol = protocol;
            SetData(data);
        }
        /// <summary>
        /// Create a new Dataset instance with default values.
        /// </summary>
        public Dataset() : this("New dataset", ApplicationState.ProjectLoaded.Protocols.FirstOrDefault(), new DataInfo[0], Guid.NewGuid().ToString())
        {
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Get all the patient dataInfo.
        /// </summary>
        /// <returns>Patient dataInfo</returns>
        public PatientDataInfo[] GetPatientDataInfos()
        {
            return m_Data.OfType<PatientDataInfo>().ToArray();
        }
        /// <summary>
        /// Get all the patient dataInfo for a specified patient.
        /// </summary>
        /// <param name="patient">Patient</param>
        /// <returns>Patient dataInfo</returns>
        public PatientDataInfo[] GetPatientDataInfos(Patient patient)
        {
            return m_Data.OfType<PatientDataInfo>().Where(d => d.Patient == patient).ToArray();
        }
        /// <summary>
        /// Get all the IEEG dataInfo.
        /// </summary>
        /// <returns>IEEG dataInfo</returns>
        public IEEGDataInfo[] GetIEEGDataInfos()
        {
            return m_Data.OfType<IEEGDataInfo>().ToArray();
        }
        /// <summary>
        /// Get all the CCEP dataInfo.
        /// </summary>
        /// <returns>CCEP dataInfo</returns>
        public CCEPDataInfo[] GetCCEPDataInfos()
        {
            return m_Data.OfType<CCEPDataInfo>().ToArray();
        }
        public FMRIDataInfo[] GetFMRIDataInfos()
        {
            return m_Data.OfType<FMRIDataInfo>().ToArray();
        }
        public PatientDataInfo[] GetMEGDataInfos()
        {
            return m_Data.Where(d => d is MEGcDataInfo || d is MEGvDataInfo).Select(d => d as PatientDataInfo).ToArray();
        }
        /// <summary>
        /// Add a dataInfo to the dataset.
        /// </summary>
        /// <param name="data">DataInfo to add</param>
        /// <returns>True if its worked, False otherwise</returns>
        public bool AddData(DataInfo data)
        {
            if (!m_Data.Contains(data))
            {
                m_Data.Add(data);
                UnityAction action = new UnityAction(() => { data.GetErrors(Protocol); });
                m_ActionByDataInfo.Add(data, action);
                data.OnRequestErrorCheck.AddListener(action);
                return true;
            }
            else return false;
        }
        /// <summary>
        /// Add dataInfo to the dataset.
        /// </summary>
        /// <param name="data">DataInfo to add</param>
        /// <returns>True if its worked, False otherwise</returns>
        public bool AddData(IEnumerable<DataInfo> data)
        {
            return data.All((d) => AddData(d));
        }
        /// <summary>
        /// Remove a dataInfo from the dataset.
        /// </summary>
        /// <param name="data">DataInfo to remove</param>
        /// <returns>True if its worked, False otherwise</returns>
        public bool RemoveData(DataInfo data)
        {
            if (m_Data.Contains(data))
            {
                m_Data.Remove(data);
                data.OnRequestErrorCheck.RemoveListener(m_ActionByDataInfo[data]);
                m_ActionByDataInfo.Remove(data);
                return true;
            }
            else return false;
        }
        /// <summary>
        /// remove dataInfo from the dataset.
        /// </summary>
        /// <param name="data">DataInfo to remove.</param>
        /// <returns>True if its worked, False otherwise</returns>
        public bool RemoveData(IEnumerable<DataInfo> data)
        {
            return data.All((d) => RemoveData(d));
        }
        /// <summary>
        /// Update a specified dataInfo.
        /// </summary>
        /// <param name="data">DataInfo to update</param>
        /// <returns>True if its worked, False otherwise</returns>
        public bool UpdateData(DataInfo data)
        {
            int index = m_Data.FindIndex(d => d.Equals(data));
            if (index != -1)
            {
                m_Data[index] = data;
                return true;
            }
            return false;
        }
        /// <summary>
        /// Set the data of the dataset.
        /// </summary>
        /// <param name="data">DataInfo of the dataset.</param>
        /// <returns>True if its worked, False otherwise</returns>
        public bool SetData(IEnumerable<DataInfo> data)
        {
            m_Data = new List<DataInfo>();
            m_ActionByDataInfo = new Dictionary<DataInfo, UnityAction>();
            return data.All((d) => AddData(d));
        }
        /// <summary>
        /// Update all the dataInfo states.
        /// </summary>
        public void UpdateDataStates()
        {
            if (m_Data != null)
            {
                foreach (DataInfo dataInfo in m_Data) dataInfo.GetErrors(Protocol);
            }
        }
        public override void GenerateID()
        {
            base.GenerateID();
            foreach (var dataInfo in Data) dataInfo.GenerateID();
        }
        public override List<BaseData> GetAllIdentifiable()
        {
            List<BaseData> IDs = base.GetAllIdentifiable();
            foreach (var dataInfo in Data) IDs.AddRange(dataInfo.GetAllIdentifiable());
            return IDs;
        }
        #endregion

        #region Public static Methods
        /// <summary>
        /// Get all the extensions of dataset file.
        /// </summary>
        /// <returns></returns>
        public static string[] GetExtensions()
        {
            return new string[] { EXTENSION[0] == '.' ? EXTENSION.Substring(1) : EXTENSION };
        }
        /// <summary>
        /// Load a dataset from a specified file.
        /// </summary>
        /// <param name="path">Path of the file</param>
        /// <param name="result">Dataset loaded from the file</param>
        /// <returns>True if its worked, False otherwise</returns>
        public static bool LoadFromFile(string path, out Dataset result)
        {
            result = null;
            try
            {
                result = ClassLoaderSaver.LoadFromJson<Dataset>(path);
                return result != null;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                throw new CanNotReadDatasetFileException(Path.GetFileNameWithoutExtension(path));
            }
        }
        /// <summary>
        /// Loads datasets from localizers database.
        /// </summary>
        /// <param name="path">The specified path of the localizers database.</param>
        /// <param name="datasets">Datasets loaded in the database.</param>
        /// <returns></returns>
        public static void LoadFromLocalizersDatabase(string path, out Dataset[] datasets, Action<float, float, LoadingText> OnChangeProgress = null)
        {
            OnChangeProgress?.Invoke(0, 0, new LoadingText("Finding datasets to load"));
            datasets = new Dataset[0];
            if (string.IsNullOrEmpty(path)) return;
            DirectoryInfo directory = new DirectoryInfo(path);
            if (!directory.Exists) return;

            string GetDownsamplingString(DirectoryInfo dir)
            {
                Regex posRegex = new Regex(dir.Name + @"_(ds[0-9]+)?\.pos$");
                FileInfo[] posFiles = dir.GetFiles("*.pos", SearchOption.AllDirectories);
                string ds = "";
                foreach (var file in posFiles)
                {
                    Match match = posRegex.Match(file.FullName);
                    if (match.Success)
                    {
                        ds = match.Groups[1].Value;
                    }
                }
                return ds;
            }

            IEnumerable<DirectoryInfo> directories = directory.GetDirectories().SelectMany(d => d.GetDirectories());
            int length = directories.Count();
            int progress = 0;
            Dictionary<Protocol.Protocol, Dataset> datasetByProtocol = new Dictionary<Protocol.Protocol, Dataset>(ApplicationState.ProjectLoaded.Protocols.Count);
            foreach (var protocol in ApplicationState.ProjectLoaded.Protocols)
            {
                datasetByProtocol.Add(protocol, new Dataset(protocol.Name, protocol, new DataInfo[0]));
            }
            foreach (var dir in directories)
            {
                OnChangeProgress?.Invoke((float)progress++ / length, 0, new LoadingText("Loading localizer ", dir.Name, " [" + (progress + 1) + "/" + length + "]"));
                Patient patient = ApplicationState.ProjectLoaded.Patients.FirstOrDefault(p => p.ID.ToUpper().CompareTo(dir.Name.ToUpper()) == 0);
                if (patient != null)
                {
                    DirectoryInfo[] subDirectories = dir.GetDirectories();
                    foreach (var subdir in subDirectories)
                    {
                        string[] splits = subdir.Name.Split('_');
                        if (splits.Length == 4)
                        {
                            Protocol.Protocol protocol = ApplicationState.ProjectLoaded.Protocols.FirstOrDefault(p => p.Name == splits[3]);
                            if (protocol != null)
                            {
                                datasetByProtocol[protocol].AddData(new IEEGDataInfo("raw", new Container.Elan(Path.Combine(subdir.FullName, subdir.Name + ".eeg"), Path.Combine(subdir.FullName, subdir.Name + ".pos"), ""), patient, IEEGDataInfo.NormalizationType.Auto));
                                string ds = GetDownsamplingString(subdir);
                                if (!string.IsNullOrEmpty(ds))
                                {
                                    FileInfo posDS = new FileInfo(Path.Combine(subdir.FullName, string.Format("{0}_{1}.pos", subdir.Name, ds)));
                                    if (posDS.Exists)
                                    {
                                        // Maybe TODO : parameters (specific UI or user preferences)
                                        string[] frequencies = new string[] { "f8f24", "f50f150" };
                                        string[] temporalSmoothings = new string[] { "sm0", "sm250", "sm500", "sm1000", "sm2500", "sm5000" };
                                        foreach (var freq in frequencies)
                                        {
                                            foreach (var ts in temporalSmoothings)
                                            {
                                                FileInfo eeg = new FileInfo(Path.Combine(subdir.FullName, string.Format("{0}_{1}", subdir.Name, freq), string.Format("{0}_{1}_{2}_{3}.eeg", subdir.Name, freq, ds, ts)));
                                                if (eeg.Exists)
                                                {
                                                    datasetByProtocol[protocol].AddData(new IEEGDataInfo(string.Format("{0}{1}", freq, ts), new Container.Elan(eeg.FullName, posDS.FullName, ""), patient, IEEGDataInfo.NormalizationType.Auto));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            datasets = datasetByProtocol.Values.OrderBy(d => d.Name).ToArray();
            OnChangeProgress?.Invoke(1.0f, 0, new LoadingText("Datasets loaded successfully"));
        }
        /// <summary>
        /// Loads datasets from BIDS database.
        /// </summary>
        /// <param name="path">The specified path of the BIDS database.</param>
        /// <param name="datasets"></param>
        /// <returns></returns>
        public static void LoadFromBIDSDatabase(string path, out Dataset[] datasets, Action<float, float, LoadingText> OnChangeProgress = null)
        {
            datasets = new Dataset[0];
            //if (string.IsNullOrEmpty(path)) return;
            //DirectoryInfo databaseDirectoryInfo = new DirectoryInfo(path);
            //if (!databaseDirectoryInfo.Exists) return;

            //// Read participants.tsv.
            //OnChangeProgress?.Invoke(0, 0, new LoadingText("Reading participants.tsv file"));
            //FileInfo participantsFileInfo = new FileInfo(Path.Combine(databaseDirectoryInfo.FullName, "participants.tsv"));
            //Dictionary<string, Dictionary<string, string>> tagValuesBySubjectID = new Dictionary<string, Dictionary<string, string>>();
            //using (StreamReader streamReader = new StreamReader(participantsFileInfo.FullName))
            //{
            //    string[] lines = streamReader.ReadToEnd().Split(new string[] { Environment.NewLine, "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            //    if (lines.Length == 0) return;
            //    string[] tags = lines[0].Split(new char[] { '\t' });
            //    for (int l = 1; l < lines.Length; l++)
            //    {
            //        string[] values = lines[l].Split(new char[] { '\t' });
            //        if (values.Length == tags.Length)
            //        {
            //            Dictionary<string, string> valueByTag = new Dictionary<string, string>();
            //            for (int t = 1; t < tags.Length; t++)
            //            {
            //                valueByTag.Add(tags[t], values[t]);
            //            }
            //            tagValuesBySubjectID.Add(values[0], valueByTag);
            //        }
            //    }
            //}

            //// Find mesh files.
            //Regex meshRegex = new Regex(@"sub-([a-zA-Z0-9.]+)(_ses-([a-zA-Z0-9.]+))?(_acq-([a-zA-Z0-9.]+))?(_ce-([a-zA-Z0-9.]+))?(_rec-([a-zA-Z0-9.]+))?(_run-([a-zA-Z0-9.]+))?(_[a-zA-Z0-9.-]+)*(_hemi-([a-zA-Z0-9.-]))(_[a-zA-Z0-9.-]+)*_([a-zA-Z0-9.-]+)\.gii$");
            //FileInfo[] meshFiles = databaseDirectoryInfo.GetFiles("*.gii", SearchOption.AllDirectories);
            //Dictionary<string, List<BIDSMeshFile>> meshesFilesBySubjectID = new Dictionary<string, List<BIDSMeshFile>>();
            //foreach (var file in meshFiles)
            //{
            //    Match match = meshRegex.Match(file.FullName);
            //    if (match.Success)
            //    {
            //        BIDSMeshFile meshFile = new BIDSMeshFile();
            //        GroupCollection groups = match.Groups;
            //        meshFile.Subject = groups[1].Value;
            //        meshFile.Session = groups[3].Value;
            //        meshFile.DataAcquisition = groups[5].Value;
            //        meshFile.Contrast = groups[7].Value;
            //        meshFile.Reconstruction = groups[9].Value;
            //        if (int.TryParse(groups[11].Value, out int run)) meshFile.Run = run;
            //        meshFile.Hemisphere = groups[14].Value;
            //        meshFile.Name = groups[16].Value;
            //        meshFile.Path = file.FullName;
            //        if (meshesFilesBySubjectID.TryGetValue(meshFile.Subject, out List<BIDSMeshFile> files))
            //        {
            //            files.Add(meshFile);
            //        }
            //        else
            //        {
            //            meshesFilesBySubjectID[meshFile.Subject] = new List<BIDSMeshFile>() { meshFile };
            //        }
            //    }
            //}

            //// Find MRI files.
            //Regex mriRegex = new Regex(@"sub-(\w+)(_ses-(\w+))?(_acq-(\w+))?(_ce-(\w+))?(_rec-(\w+))?(_run-(\w+))?_(T1w|T2w|T1rho|T1map|T2map|T2star|FLAIR|FLASH|PD|PDmap|PDT2|inplaneT1|inplaneT2|angio)\.nii(\.gz)?$");
            //FileInfo[] mriFiles = databaseDirectoryInfo.GetFiles("*.nii", SearchOption.AllDirectories);
            //Dictionary<string, List<BIDSMRIFile>> mriFilesBySubjectID = new Dictionary<string, List<BIDSMRIFile>>();
            //foreach (var file in mriFiles)
            //{
            //    Match match = mriRegex.Match(file.FullName);
            //    if (match.Success)
            //    {
            //        BIDSMRIFile mriFile = new BIDSMRIFile();
            //        GroupCollection groups = match.Groups;
            //        mriFile.Subject = groups[1].Value;
            //        mriFile.Session = groups[3].Value;
            //        mriFile.DataAcquisition = groups[5].Value;
            //        mriFile.Contrast = groups[7].Value;
            //        mriFile.Reconstruction = groups[9].Value;
            //        if (int.TryParse(groups[11].Value, out int run)) mriFile.Run = run;
            //        mriFile.Name = groups[12].Value;
            //        mriFile.Path = file.FullName;
            //        if (mriFilesBySubjectID.TryGetValue(mriFile.Subject, out List<BIDSMRIFile> files))
            //        {
            //            files.Add(mriFile);
            //        }
            //        else
            //        {
            //            mriFilesBySubjectID[mriFile.Subject] = new List<BIDSMRIFile>() { mriFile };
            //        }
            //    }
            //}

            //// Find Electrodes files.
            //Regex electrodesRegex = new Regex(@"sub-(\w+)(_ses-(\w+))?(_acq-(\w+))?(_ce-(\w+))?(_rec-(\w+))?(_run-(\w+))?(_space-(\w+))?_electrodes\.tsv?$");
            //FileInfo[] electrodesFiles = databaseDirectoryInfo.GetFiles("*_electrodes.tsv", SearchOption.AllDirectories);
            //Dictionary<string, List<BIDSElectrodeFile>> electrodesFilesBySubjectID = new Dictionary<string, List<BIDSElectrodeFile>>();
            //foreach (var file in electrodesFiles)
            //{
            //    Match match = electrodesRegex.Match(file.FullName);
            //    if (match.Success)
            //    {
            //        BIDSElectrodeFile electrodeFile = new BIDSElectrodeFile();
            //        GroupCollection groups = match.Groups;
            //        electrodeFile.Subject = groups[1].Value;
            //        electrodeFile.Session = groups[3].Value;
            //        electrodeFile.DataAcquisition = groups[5].Value;
            //        electrodeFile.Contrast = groups[7].Value;
            //        electrodeFile.Reconstruction = groups[9].Value;
            //        if (int.TryParse(groups[11].Value, out int run)) electrodeFile.Run = run;
            //        electrodeFile.Space = groups[12].Value;
            //        electrodeFile.Name = groups[12].Value;
            //        electrodeFile.Path = file.FullName;
            //        if (electrodesFilesBySubjectID.TryGetValue(electrodeFile.Subject, out List<BIDSElectrodeFile> files))
            //        {
            //            files.Add(electrodeFile);
            //        }
            //        else
            //        {
            //            electrodesFilesBySubjectID[electrodeFile.Subject] = new List<BIDSElectrodeFile>() { electrodeFile };
            //        }
            //    }
            //}

            //// Create patients.
            //int length = tagValuesBySubjectID.Count;
            //int progress = 0;
            //List<Patient> patientsList = new List<Patient>(tagValuesBySubjectID.Count);
            //foreach (var pair in tagValuesBySubjectID)
            //{
            //    OnChangeProgress?.Invoke((float)progress++ / length, 0, new LoadingText("Loading patient ", pair.Key, " [" + (progress + 1) + "/" + length + "]"));

            //    // Meshes.
            //    List<BaseMesh> meshes = new List<BaseMesh>();
            //    if (meshesFilesBySubjectID.TryGetValue(pair.Key, out List<BIDSMeshFile> subjectMeshFiles))
            //    {
            //        List<BIDSMeshFile> usedMeshFiles = new List<BIDSMeshFile>(subjectMeshFiles.Count);
            //        foreach (var meshFile in subjectMeshFiles)
            //        {
            //            if (!usedMeshFiles.Contains(meshFile))
            //            {
            //                if (meshFile.Hemisphere == "L" || meshFile.Hemisphere == "l" || meshFile.Hemisphere == "left" || meshFile.Hemisphere == "Left")
            //                {
            //                    var rightMeshFile = subjectMeshFiles.FirstOrDefault(f => f.Same(meshFile) && (f.Hemisphere == "R" || f.Hemisphere == "r" || f.Hemisphere == "right" || f.Hemisphere == "Right"));
            //                    if (rightMeshFile == null) rightMeshFile = new BIDSMeshFile();
            //                    usedMeshFiles.Add(rightMeshFile);
            //                    meshes.Add(new LeftRightMesh(meshFile.Name, "", meshFile.Path, rightMeshFile.Path, "", ""));
            //                }
            //                else if (meshFile.Hemisphere == "R" || meshFile.Hemisphere == "r" || meshFile.Hemisphere == "right" || meshFile.Hemisphere == "Right")
            //                {
            //                    var leftMeshFile = subjectMeshFiles.FirstOrDefault(f => f.Same(meshFile) && (f.Hemisphere == "L" || f.Hemisphere == "l" || f.Hemisphere == "left" || f.Hemisphere == "Left"));
            //                    if (leftMeshFile == null) leftMeshFile = new BIDSMeshFile();
            //                    usedMeshFiles.Add(leftMeshFile);
            //                    meshes.Add(new LeftRightMesh(meshFile.Name, "", leftMeshFile.Path, meshFile.Path, "", ""));
            //                }
            //                else
            //                {
            //                    meshes.Add(new SingleMesh(meshFile.Name, "", meshFile.Path, ""));
            //                }
            //                usedMeshFiles.Add(meshFile);
            //            }
            //        }
            //    }

            //    // MRIs.
            //    List<MRI> mris = new List<MRI>();
            //    if (mriFilesBySubjectID.TryGetValue(pair.Key, out List<BIDSMRIFile> subjectMRIFiles))
            //    {
            //        mris = subjectMRIFiles.Select(f => new MRI(string.Format("{0}{1}", f.Name, !string.IsNullOrEmpty(f.Session) ? string.Format(" ({0})", f.Session) : ""), f.Path)).ToList();
            //    }

            //    // Sites.
            //    List<Site> sites = new List<Site>();
            //    if (electrodesFilesBySubjectID.TryGetValue(pair.Key, out List<BIDSElectrodeFile> subjectElectrodesFiles))
            //    {
            //        foreach (var electrodeFile in subjectElectrodesFiles)
            //        {
            //            (new Site() as ILoadable<Site>).LoadFromFile(electrodeFile.Path, out Site[] fileSites);
            //            foreach (var site in fileSites)
            //            {
            //                Site existingSite = sites.FirstOrDefault(s => s.Name == site.Name);
            //                if (existingSite != null)
            //                {
            //                    existingSite.Coordinates.AddRange(site.Coordinates);
            //                    existingSite.Tags.AddRange(site.Tags);
            //                }
            //                else
            //                {
            //                    sites.Add(site);
            //                }
            //            }
            //        }
            //    }

            //    // Tags.
            //    List<BaseTagValue> tags = new List<BaseTagValue>();
            //    if (tagValuesBySubjectID.TryGetValue(pair.Key, out Dictionary<string, string> subjectTags))
            //    {
            //        // Add tags to project.
            //        IEnumerable<BaseTag> projectTags = ApplicationState.ProjectLoaded.Preferences.PatientsTags.Concat(ApplicationState.ProjectLoaded.Preferences.GeneralTags);
            //        foreach (var tagName in subjectTags.Keys)
            //        {
            //            if (!projectTags.Any(t => t.Name == tagName))
            //            {
            //                ApplicationState.ProjectLoaded.Preferences.PatientsTags.Add(new StringTag(tagName));
            //            }
            //        }
            //        // Add tags to patient
            //        projectTags = ApplicationState.ProjectLoaded.Preferences.PatientsTags.Concat(ApplicationState.ProjectLoaded.Preferences.GeneralTags);
            //        foreach (var subjectTag in subjectTags)
            //        {
            //            BaseTag tag = projectTags.FirstOrDefault(t => t.Name == subjectTag.Key);
            //            if (tag != null)
            //            {
            //                BaseTagValue tagValue = null;
            //                if (tag is EmptyTag emptyTag)
            //                {
            //                    tagValue = new EmptyTagValue(emptyTag);
            //                }
            //                else if (tag is BoolTag boolTag)
            //                {
            //                    if (bool.TryParse(subjectTag.Value, out bool result))
            //                    {
            //                        tagValue = new BoolTagValue(boolTag, result);
            //                    }
            //                }
            //                else if (tag is EnumTag enumTag)
            //                {
            //                    tagValue = new EnumTagValue(enumTag, subjectTag.Value);
            //                }
            //                else if (tag is FloatTag floatTag)
            //                {
            //                    if (NumberExtension.TryParseFloat(subjectTag.Value, out float result))
            //                    {
            //                        tagValue = new FloatTagValue(floatTag, result);
            //                    }
            //                }
            //                else if (tag is IntTag intTag)
            //                {
            //                    if (int.TryParse(subjectTag.Value, out int result))
            //                    {
            //                        tagValue = new IntTagValue(intTag, result);
            //                    }
            //                }
            //                else if (tag is StringTag stringTag)
            //                {
            //                    if (!string.IsNullOrEmpty(subjectTag.Value))
            //                    {
            //                        tagValue = new StringTagValue(stringTag, subjectTag.Value);
            //                    }
            //                }
            //                if (tagValue != null)
            //                {
            //                    tags.Add(tagValue);
            //                }
            //            }
            //        }
            //    }

            //    // Create patient.
            //    Patient patient = new Patient(pair.Key, "", 0, meshes, mris, sites, tags);
            //    patientsList.Add(patient);
            //}
            //datasets = patientsList.ToArray();
            //OnChangeProgress?.Invoke(1.0f, 0, new LoadingText("Patients loaded successfully"));
        }
        /// <summary>
        /// Coroutine to load datasets from database. Implementation of ILoadableFromDatabase.
        /// </summary>
        /// <param name="path">The specified path of the dataset file.</param>
        /// <param name="OnChangeProgress">Action called on change progress.</param>
        /// <param name="result">The datasets loaded.</param>
        /// <returns></returns>
        public static IEnumerator c_LoadFromDatabase(string path, Action<float, float, LoadingText> OnChangeProgress, Action<IEnumerable<Dataset>> result)
        {
            yield return Ninja.JumpBack;
            Dataset[] datasets;
            if (IsBIDSDirectory(path)) LoadFromBIDSDatabase(path, out datasets, OnChangeProgress);
            else LoadFromLocalizersDatabase(path, out datasets, OnChangeProgress);
            yield return Ninja.JumpToUnity;
            result(datasets);
        }
        #endregion

        #region Private Static Methods
        /// <summary>
        /// Checks if the input directory is a BIDS database
        /// </summary>
        /// <param name="path">Path to the input database</param>
        /// <returns>True if the input database is a BIDS database</returns>
        private static bool IsBIDSDirectory(string path)
        {
            FileInfo participantsFileInfo = new FileInfo(Path.Combine(path, "participants.tsv"));
            return participantsFileInfo.Exists;
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Clone of this instance</returns>
        public override object Clone()
        {
            return new Dataset(Name, Protocol, Data.DeepClone(), ID);
        }
        /// <summary>
        /// Copy this a instance to this instance.
        /// </summary>
        /// <param name="copy">Instance to copy.</param>
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is Dataset dataset)
            {
                Name = dataset.Name;
                Protocol = dataset.Protocol;
                SetData(dataset.Data);
            }
        }
        #endregion

        #region Serialization
        protected override void OnSerializing()
        {
            base.OnSerializing();
            m_ProtocolID = m_Protocol?.ID;
        }
        protected override void OnDeserialized()
        {
            base.OnDeserialized();
            var protocol = ApplicationState.ProjectLoaded.Protocols.FirstOrDefault(p => p.ID == m_ProtocolID);
            if (protocol == null) protocol = ApplicationState.ProjectLoaded.Protocols.First();
            Protocol = protocol;
            foreach (var data in m_Data)
            {
                UnityAction action = new UnityAction(() => data.GetErrors(Protocol));
                m_ActionByDataInfo.Add(data, action);
                data.OnRequestErrorCheck.AddListener(action);
            }
        }
        #endregion

        #region Interfaces
        /// <summary>
        /// Get all the extensions of dataset file.
        /// </summary>
        /// <returns></returns>
        string[] ILoadable<Dataset>.GetExtensions()
        {
            return GetExtensions();
        }
        /// <summary>
        /// Load a dataset from a specified file.
        /// </summary>
        /// <param name="path">Path of the file</param>
        /// <param name="result">Dataset loaded from the file</param>
        /// <returns>True if its worked, False otherwise</returns>
        bool ILoadable<Dataset>.LoadFromFile(string path, out Dataset[] result)
        {
            bool success = LoadFromFile(path, out Dataset dataset);
            result = new Dataset[] { dataset };
            return success;
        }
        IEnumerator ILoadableFromDatabase<Dataset>.LoadFromDatabase(string path, Action<float, float, LoadingText> OnChangeProgress, Action<IEnumerable<Dataset>> result)
        {
            yield return Ninja.JumpToUnity;
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadFromDatabase(path, OnChangeProgress, result));
            yield return Ninja.JumpBack;
        }
        #endregion
    }
}