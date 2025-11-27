using Cysharp.Threading.Tasks;
using HBP.Core.Exceptions;
using HBP.Core.Interfaces;
using HBP.Core.Tools;
using HBP.Data.Database;
using HBP.Data.Preferences;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    /// <summary>
    /// Contains all the data about a patient.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>Name</b></term>
    /// <description>Name of the patient.</description>
    /// </item>
    /// <item>
    /// <term><b>Date</b></term>
    /// <description>Year in which the patient was implanted.</description>
    /// </item>
    /// <item>
    /// <term><b>Place</b></term>
    /// <description>Place where the patient had the operation.</description>
    /// </item>
    /// <item>
    /// <term><b>Meshes</b></term>
    /// <description>Meshes of the patient.</description>
    /// </item>
    /// <item>
    /// <term><b>MRIs</b></term>
    /// <description>MRI scans of the patient.</description>
    /// </item>
    /// <item>
    /// <term><b>Sites</b></term>
    /// <description>Sites of the patient.</description>
    /// </item>
    /// <item>
    /// <term><b>Tags</b></term>
    /// <description>Tags of the patient.</description>
    /// </item>
    /// </list>
    /// </remarks>
    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class Patient : BaseData, ILoadable<Patient>, ILoadableFromDatabase<Patient>, ILoadableFromDirectory<Patient>, INameable
    {
        #region Properties
        /// <summary>
        /// Extension of patient files.
        /// </summary>
        public const string EXTENSION = ".patient";
        /// <summary>
        /// Name of the patient.
        /// </summary>
        [JsonProperty] public string Name { get; set; }
        /// <summary>
        /// Meshes of the patient.
        /// </summary>
        [JsonProperty] public List<BaseMesh> Meshes { get; set; }
        /// <summary>
        /// MRI scans of the patient.
        /// </summary>
        [JsonProperty] public List<MRI> MRIs { get; set; }
        /// <summary>
        /// Sites of the patient.
        /// </summary>
        [JsonProperty] public List<Site> Sites { get; set; }
        /// <summary>
        /// Tags of the patient.
        /// </summary>
        [JsonProperty] public List<BaseTagValue> Tags { get; set; }
        [JsonProperty] public string CorrespondingDatabaseID { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of Patient.
        /// </summary>
        /// <param name="name">Name of the patient.</param>
        /// <param name="meshes">Meshes of the patient.</param>
        /// <param name="MRIs">MRI scans of the patient.</param>
        /// <param name="sites">Sites of the patient.</param>
        /// <param name="tags">Tags of the patient.</param>
        /// <param name="ID">Unique identifier to identify the patient.</param>
        public Patient(string name, IEnumerable<BaseMesh> meshes, IEnumerable<MRI> MRIs, IEnumerable<Site> sites, IEnumerable<BaseTagValue> tags, string correspondingDatabaseID, string ID) : base(ID)
        {
            Name = name;
            Meshes = meshes.ToList();
            this.MRIs = MRIs.ToList();
            Sites = sites.ToList();
            Tags = tags.ToList();
            CorrespondingDatabaseID = correspondingDatabaseID;
        }
        /// <summary>
        /// Create a new instance of Patient.
        /// </summary>
        /// <param name="name">Name of the patient.</param>
        /// <param name="meshes">Meshes of the patient.</param>
        /// <param name="MRIs">MRI scans of the patient.</param>
        /// <param name="sites">Sites of the patient.</param>
        /// <param name="tags">Tags of the patient.</param>
        public Patient(string name, IEnumerable<BaseMesh> meshes, IEnumerable<MRI> MRIs, IEnumerable<Site> sites, IEnumerable<BaseTagValue> tags, string correspondingDatabaseID) : base()
        {
            Name = name;
            Meshes = meshes.ToList();
            this.MRIs = MRIs.ToList();
            Sites = sites.ToList();
            Tags = tags.ToList();
            CorrespondingDatabaseID = correspondingDatabaseID;
        }
        /// <summary>
        /// Create a new instance of Patient.
        /// </summary>
        public Patient() : this("Unknown", new BaseMesh[0], new MRI[0], new Site[0], new BaseTagValue[0], "")
        {
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Generates  ID recursively.
        /// </summary>
        public override void GenerateID()
        {
            base.GenerateID();
            foreach (var mesh in Meshes) mesh.GenerateID();
            foreach (var mri in MRIs) mri.GenerateID();
            foreach (var site in Sites) site.GenerateID();
            foreach (var tag in Tags) tag.GenerateID();
        }
        public override List<BaseData> GetAllIdentifiable()
        {
            List<BaseData> IDs = base.GetAllIdentifiable();
            foreach (var mesh in Meshes) IDs.AddRange(mesh.GetAllIdentifiable());
            foreach (var mri in MRIs) IDs.AddRange(mri.GetAllIdentifiable());
            foreach (var site in Sites) IDs.AddRange(site.GetAllIdentifiable());
            foreach (var tag in Tags) IDs.AddRange(tag.GetAllIdentifiable());
            return IDs;
        }
        /// <summary>
        /// Clean this patient by removing any invalid data
        /// </summary>
        public void CleanInvalidData()
        {
            // Patient tags
            List<BaseTagValue> patientTagsToRemove = new List<BaseTagValue>();
            foreach (var tag in Tags)
                if (tag.Tag == null)
                    patientTagsToRemove.Add(tag);
            foreach (var tag in patientTagsToRemove)
                Tags.Remove(tag);

            // Site tags
            foreach (var site in Sites)
            {
                List<BaseTagValue> siteTagsToRemove = new List<BaseTagValue>();
                foreach (var tag in site.Tags)
                    if (tag.Tag == null)
                        siteTagsToRemove.Add(tag);
                foreach (var tag in siteTagsToRemove)
                    site.Tags.Remove(tag);
            }
        }
        public async UniTask CheckTagsAsync(IEnumerable<BaseTag> tags)
        {
            await UniTask.SwitchToThreadPool();
            Tags.RemoveAll(t => t.Tag == null || !PersistentDataManager.Tags.AllTags.Contains(t.Tag));
            foreach (var site in Sites) site.Tags.RemoveAll(t => t.Tag == null || !PersistentDataManager.Tags.AllTags.Contains(t.Tag));
            List<BaseTagValue> tagsToUpdate = Tags.Where(t => tags.Contains(t.Tag)).ToList();
            tagsToUpdate.AddRange(Sites.SelectMany(s => s.Tags).Where(t => tags.Contains(t.Tag)));
            foreach (var tagValue in tagsToUpdate)
            {
                if (tagValue.Tag is IntTag && tagValue is not IntTagValue)
                {
                    Tags.Remove(tagValue);
                    var newTagValue = new IntTagValue();
                    newTagValue.Copy(tagValue);
                    Tags.Add(newTagValue);
                    newTagValue.UpdateValue();
                }
                else if (tagValue.Tag is FloatTag && tagValue is not FloatTagValue)
                {
                    Tags.Remove(tagValue);
                    var newTagValue = new FloatTagValue();
                    newTagValue.Copy(tagValue);
                    Tags.Add(newTagValue);
                    newTagValue.UpdateValue();
                }
                else if (tagValue.Tag is BoolTag && tagValue is not BoolTagValue)
                {
                    Tags.Remove(tagValue);
                    var newTagValue = new BoolTagValue();
                    newTagValue.Copy(tagValue);
                    Tags.Add(newTagValue);
                    newTagValue.UpdateValue();
                }
                else if (tagValue.Tag is EmptyTag && tagValue is not EmptyTagValue)
                {
                    Tags.Remove(tagValue);
                    var newTagValue = new EmptyTagValue();
                    newTagValue.Copy(tagValue);
                    Tags.Add(newTagValue);
                    newTagValue.UpdateValue();
                }
                else if (tagValue.Tag is EnumTag && tagValue is not EnumTagValue)
                {
                    Tags.Remove(tagValue);
                    var newTagValue = new EnumTagValue();
                    newTagValue.Copy(tagValue);
                    Tags.Add(newTagValue);
                    newTagValue.UpdateValue();
                }
                else if (tagValue.Tag is StringTag && tagValue is not StringTagValue)
                {
                    Tags.Remove(tagValue);
                    var newTagValue = new StringTagValue();
                    newTagValue.Copy(tagValue);
                    Tags.Add(newTagValue);
                    newTagValue.UpdateValue();
                }
                else
                {
                    tagValue.UpdateValue();
                }
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Checks if a tag value is invalid (n/a, nan, empty, etc.)
        /// This method provides consistent handling between Intranat and BIDS databases.
        /// </summary>
        /// <param name="value">The string value to check</param>
        /// <returns>True if the value is invalid and should be ignored, false otherwise</returns>
        private static bool IsInvalidTagValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true;

            string lowerValue = value.Trim().ToLower();
            return lowerValue == "n/a" || 
                   lowerValue == "na" || 
                   lowerValue == "nan" || 
                   lowerValue == "null" || 
                   lowerValue == "none" || 
                   lowerValue == "" || 
                   lowerValue == "-";
        }
        #endregion

        #region Public Static Methods
        /// <summary>
        /// Gets the extension of the patient files.
        /// </summary>
        /// <returns></returns>
        public static string[] GetExtensions()
        {
            return new string[] { EXTENSION[0] == '.' ? EXTENSION[1..] : EXTENSION };
        }
        /// <summary>
        /// Determines if the specified directory is a patient directory.
        /// </summary>
        /// <param name="path">The specified directory.</param>
        /// <returns><see langword="true"/> if the directory is a patient directory; otherwise, <see langword="false"/></returns>
        public static bool IsPatientDirectory(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            DirectoryInfo directory = new DirectoryInfo(path);
            if (!directory.Exists) return false;
            DirectoryInfo[] directories = directory.GetDirectories();
            if (directories.Any((dir) => dir.Name == "implantation") || directories.Any((dir) => dir.Name == "t1mri")) return true;
            return false;
        }
        /// <summary>
        /// Checks if the directory name follows the Place_Date_Name format.
        /// </summary>
        /// <param name="directoryName">Name of the directory.</param>
        /// <param name="place">Output place if format matches.</param>
        /// <param name="date">Output date if format matches.</param>
        /// <param name="name">Output name if format matches.</param>
        /// <returns><see langword="true"/> if the format matches; otherwise, <see langword="false"/></returns>
        private static bool TryParsePlaceDateNameFormat(string directoryName, out string place, out int date, out string name)
        {
            place = null;
            date = 0;
            name = null;
            
            string[] nameElements = directoryName.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (nameElements.Length != 3) return false;
            
            // Check if the second element is a valid year (integer)
            if (!int.TryParse(nameElements[1], out date)) return false;
            
            place = nameElements[0];
            name = nameElements[2];
            return true;
        }
        /// <summary>
        /// Loads patients from a directory
        /// </summary>
        /// <param name="path">The specified path of the patient directory.</param>
        /// <param name="result">The patient in the patient directory.</param>
        /// <returns><see langword="true"/> if the method worked successfully; otherwise, <see langword="false"/></returns>
        public static bool LoadFromDirectory(string path, out Patient result)
        {
            result = null;
            if (IsPatientDirectory(path))
            {
                DirectoryInfo directory = new DirectoryInfo(path);
                string patientName;
                List<BaseTagValue> patientTags = new List<BaseTagValue>();
                
                // Check if directory follows Place_Date_Name format
                if (TryParsePlaceDateNameFormat(directory.Name, out string place, out int date, out string name))
                {
                    // Use the full directory name as patient name (Place_Date_Name)
                    patientName = directory.Name;
                    
                    // Create Date and Place tags
                    IEnumerable<BaseTag> tags = PersistentDataManager.Tags.PatientsTags.Concat(PersistentDataManager.Tags.GeneralTags);
                    IntTag dateTag = tags.OfType<IntTag>().FirstOrDefault(t => t.Name == "Date");
                    if (dateTag == null)
                    {
                        dateTag = new IntTag("Date");
                        PersistentDataManager.Tags.AddPatientTag(dateTag);
                    }
                    StringTag placeTag = tags.OfType<StringTag>().FirstOrDefault(t => t.Name == "Place");
                    if (placeTag == null)
                    {
                        placeTag = new StringTag("Place");
                        PersistentDataManager.Tags.AddPatientTag(placeTag);
                    }
                    patientTags.Add(new IntTagValue(dateTag, date));
                    patientTags.Add(new StringTagValue(placeTag, place));
                }
                else
                {
                    // Use directory name as is
                    patientName = directory.Name;
                }
                
                result = new Patient(patientName, BaseMesh.LoadFromDirectory(path), MRI.LoadFromDirectory(path), Site.LoadFromIntranatDirectory(path), patientTags, "", directory.Name);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Loads patient from patient file.
        /// </summary>
        /// <param name="path">The specified path of the patient file.</param>
        /// <param name="result">The patient in the patient file.</param>
        /// <returns><see langword="true"/> if the method worked successfully; otherwise, <see langword="false"/></returns>
        public static bool LoadFromFile(string path, out Patient result)
        {
            result = null;
            try
            {
                result = ClassLoaderSaver.LoadFromJson<Patient>(path);
                result.CleanInvalidData();
                return result != null;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                throw new CanNotReadPatientFileException(Path.GetFileNameWithoutExtension(path));
            }
        }
        /// <summary>
        /// Loads patients from intranat database.
        /// </summary>
        /// <param name="path">The specified path of the intranat database.</param>
        /// <param name="patients">Patients loaded in the database.</param>
        /// <returns></returns>
        public static void LoadFromIntranatDatabase(DatabaseReference databaseReference, out Patient[] patients, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            updateProgress?.Invoke(0, 0, new LoadingText("Finding patients to load"));
            patients = new Patient[0];
            if (string.IsNullOrEmpty(databaseReference.Path)) return;
            DirectoryInfo directory = new DirectoryInfo(databaseReference.Path);
            if (!directory.Exists) return;

            IEnumerable<DirectoryInfo> patientDirectories = directory.GetDirectories().Where(d => IsPatientDirectory(d.FullName));
            int length = patientDirectories.Count();
            int progress = 0;
            List<Patient> patientsList = new List<Patient>(length);
            foreach (var dir in patientDirectories)
            {
                token.ThrowIfCancellationRequested();
                updateProgress?.Invoke((float)progress++ / length, 0, new LoadingText("Loading patient ", dir.Name, " [" + (progress + 1) + "/" + length + "]"));
                if (LoadFromDirectory(dir.FullName, out Patient patient))
                {
                    patient.CorrespondingDatabaseID = databaseReference.ID;
                    patientsList.Add(patient);
                }
            }
            patients = patientsList.ToArray();
            updateProgress?.Invoke(1.0f, 0, new LoadingText("Patients loaded successfully"));
        }
        /// <summary>
        /// Loads patients from BIDS database.
        /// </summary>
        /// <param name="path">The specified path of the BIDS database.</param>
        /// <param name="patients"></param>
        /// <returns></returns>
        public static void LoadFromBIDSDatabase(DatabaseReference databaseReference, out Patient[] patients, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            patients = new Patient[0];
            if (string.IsNullOrEmpty(databaseReference.Path)) return;
            DirectoryInfo databaseDirectoryInfo = new DirectoryInfo(databaseReference.Path);
            if (!databaseDirectoryInfo.Exists) return;

            // Read participants.tsv.
            updateProgress?.Invoke(0, 0, new LoadingText("Reading participants.tsv file"));
            FileInfo participantsFileInfo = new FileInfo(Path.Combine(databaseDirectoryInfo.FullName, "participants.tsv"));
            if (!participantsFileInfo.Exists) throw new HBPException("Missing file", "The mandatory file 'participants.tsv' is missing in the BIDS database directory.");
            Dictionary<string, Dictionary<string, string>> tagValuesBySubjectID = new Dictionary<string, Dictionary<string, string>>();
            using (StreamReader streamReader = new StreamReader(participantsFileInfo.FullName))
            {
                string[] lines = streamReader.ReadToEnd().Split(new string[] { Environment.NewLine, "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length == 0) return;
                string[] tags = lines[0].Split(new char[] { '\t' });
                for (int l = 1; l < lines.Length; l++)
                {
                    string[] values = lines[l].Split(new char[] { '\t' });
                    if (values.Length == tags.Length)
                    {
                        Dictionary<string, string> valueByTag = new Dictionary<string, string>();
                        for (int t = 1; t < tags.Length; t++)
                        {
                            valueByTag.Add(tags[t], values[t]);
                        }
                        tagValuesBySubjectID.Add(values[0], valueByTag);
                    }
                }
            }

            // Find mesh files.
            Regex meshRegex = new Regex(@"(sub-[a-zA-Z0-9.]+)(_ses-([a-zA-Z0-9.]+))?(_acq-([a-zA-Z0-9.]+))?(_ce-([a-zA-Z0-9.]+))?(_rec-([a-zA-Z0-9.]+))?(_run-([a-zA-Z0-9.]+))?(_desc-([a-zA-Z0-9.]+))?(_[a-zA-Z0-9.-]+)*(_hemi-([a-zA-Z0-9.-]))(_[a-zA-Z0-9.-]+)*_([a-zA-Z0-9.-]+)\.[a-zA-Z0-9]*\.gii$");
            FileInfo[] meshFiles = databaseDirectoryInfo.GetFiles("*.gii", SearchOption.AllDirectories);
            Dictionary<string, List<BIDSMeshFile>> meshesFilesBySubjectID = new Dictionary<string, List<BIDSMeshFile>>();
            foreach (var file in meshFiles)
            {
                token.ThrowIfCancellationRequested();
                Match match = meshRegex.Match(file.FullName);
                if (match.Success)
                {
                    BIDSMeshFile meshFile = new BIDSMeshFile();
                    GroupCollection groups = match.Groups;
                    meshFile.Subject = groups[1].Value;
                    meshFile.Session = groups[3].Value;
                    meshFile.DataAcquisition = groups[5].Value;
                    meshFile.Contrast = groups[7].Value;
                    meshFile.Reconstruction = groups[9].Value;
                    if (int.TryParse(groups[11].Value, out int run)) meshFile.Run = run;
                    meshFile.Description = groups[13].Value;
                    meshFile.Hemisphere = groups[16].Value;
                    meshFile.Name = groups[18].Value;
                    meshFile.Path = file.FullName;
                    if (meshesFilesBySubjectID.TryGetValue(meshFile.Subject, out List<BIDSMeshFile> files))
                    {
                        files.Add(meshFile);
                    }
                    else
                    {
                        meshesFilesBySubjectID[meshFile.Subject] = new List<BIDSMeshFile>() { meshFile };
                    }
                }
            }

            // Find MRI files.
            Regex mriRegex = new Regex(@"(sub-\w+)(_ses-(\w+))?(_acq-(\w+))?(_ce-(\w+))?(_rec-(\w+))?(_run-(\w+))?_(T1w|T2w|T1rho|T1map|T2map|T2star|FLAIR|FLASH|PD|PDmap|PDT2|inplaneT1|inplaneT2|angio)\.nii(\.gz)?$");
            FileInfo[] mriFiles = databaseDirectoryInfo.GetFiles("*.nii", SearchOption.AllDirectories);
            Dictionary<string, List<BIDSMRIFile>> mriFilesBySubjectID = new Dictionary<string, List<BIDSMRIFile>>();
            foreach (var file in mriFiles)
            {
                token.ThrowIfCancellationRequested();
                Match match = mriRegex.Match(file.FullName);
                if (match.Success)
                {
                    BIDSMRIFile mriFile = new BIDSMRIFile();
                    GroupCollection groups = match.Groups;
                    mriFile.Subject = groups[1].Value;
                    mriFile.Session = groups[3].Value;
                    mriFile.DataAcquisition = groups[5].Value;
                    mriFile.Contrast = groups[7].Value;
                    mriFile.Reconstruction = groups[9].Value;
                    if (int.TryParse(groups[11].Value, out int run)) mriFile.Run = run;
                    mriFile.Name = groups[12].Value;
                    mriFile.Path = file.FullName;
                    if (mriFilesBySubjectID.TryGetValue(mriFile.Subject, out List<BIDSMRIFile> files))
                    {
                        files.Add(mriFile);
                    }
                    else
                    {
                        mriFilesBySubjectID[mriFile.Subject] = new List<BIDSMRIFile>() { mriFile };
                    }
                }
            }

            // Find Electrodes files.
            Regex electrodesRegex = new Regex(@"(sub-\w+)(_ses-(\w+))?(_acq-(\w+))?(_ce-(\w+))?(_rec-(\w+))?(_run-(\w+))?(_space-(\w+))?_electrodes\.tsv?$");
            FileInfo[] electrodesFiles = databaseDirectoryInfo.GetFiles("*_electrodes.tsv", SearchOption.AllDirectories);
            Dictionary<string, List<BIDSElectrodeFile>> electrodesFilesBySubjectID = new Dictionary<string, List<BIDSElectrodeFile>>();
            foreach (var file in electrodesFiles)
            {
                token.ThrowIfCancellationRequested();
                Match match = electrodesRegex.Match(file.FullName);
                if (match.Success)
                {
                    BIDSElectrodeFile electrodeFile = new BIDSElectrodeFile();
                    GroupCollection groups = match.Groups;
                    electrodeFile.Subject = groups[1].Value;
                    electrodeFile.Session = groups[3].Value;
                    electrodeFile.DataAcquisition = groups[5].Value;
                    electrodeFile.Contrast = groups[7].Value;
                    electrodeFile.Reconstruction = groups[9].Value;
                    if (int.TryParse(groups[11].Value, out int run)) electrodeFile.Run = run;
                    electrodeFile.Space = groups[12].Value;
                    electrodeFile.Name = groups[12].Value;
                    electrodeFile.Path = file.FullName;
                    if (electrodesFilesBySubjectID.TryGetValue(electrodeFile.Subject, out List<BIDSElectrodeFile> files))
                    {
                        files.Add(electrodeFile);
                    }
                    else
                    {
                        electrodesFilesBySubjectID[electrodeFile.Subject] = new List<BIDSElectrodeFile>() { electrodeFile };
                    }
                }
            }

            // Create patients.
            int length = tagValuesBySubjectID.Count;
            int progress = 0;
            List<Patient> patientsList = new List<Patient>(tagValuesBySubjectID.Count);
            foreach (var pair in tagValuesBySubjectID)
            {
                token.ThrowIfCancellationRequested();
                updateProgress?.Invoke((float)progress++ / length, 0, new LoadingText("Loading patient ", pair.Key, " [" + (progress + 1) + "/" + length + "]"));

                // Meshes.
                List<BaseMesh> meshes = new List<BaseMesh>();
                if (meshesFilesBySubjectID.TryGetValue(pair.Key, out List<BIDSMeshFile> subjectMeshFiles))
                {
                    List<BIDSMeshFile> usedMeshFiles = new List<BIDSMeshFile>(subjectMeshFiles.Count);
                    
                    foreach (var meshFile in subjectMeshFiles)
                    {
                        if (!usedMeshFiles.Contains(meshFile) && !meshFile.IsMarsAtlasMesh)
                        {
                            string transformationPath = meshFile.FindTransformationFile();
                            
                            if (meshFile.IsLeft)
                            {
                                var rightMeshFile = subjectMeshFiles.FirstOrDefault(f => f.Same(meshFile) && f.IsRight);
                                rightMeshFile ??= new BIDSMeshFile();
                                usedMeshFiles.Add(rightMeshFile);
                                
                                // Find corresponding MarsAtlas files for "white" meshes
                                string leftMarsAtlas = "";
                                string rightMarsAtlas = "";
                                if (meshFile.Name == "white")
                                {
                                    var leftMarsAtlasFile = subjectMeshFiles.FirstOrDefault(f => f.SameFields(meshFile) && f.IsMarsAtlasMesh && f.IsLeft);
                                    var rightMarsAtlasFile = subjectMeshFiles.FirstOrDefault(f => f.SameFields(meshFile) && f.IsMarsAtlasMesh && f.IsRight);
                                    
                                    leftMarsAtlas = leftMarsAtlasFile?.Path ?? "";
                                    rightMarsAtlas = rightMarsAtlasFile?.Path ?? "";
                                    
                                    // Mark MarsAtlas files as used
                                    if (leftMarsAtlasFile != null) usedMeshFiles.Add(leftMarsAtlasFile);
                                    if (rightMarsAtlasFile != null) usedMeshFiles.Add(rightMarsAtlasFile);
                                }
                                
                                meshes.Add(new LeftRightMesh(meshFile.Name, transformationPath, meshFile.Path, rightMeshFile.Path, leftMarsAtlas, rightMarsAtlas));
                            }
                            else if (meshFile.IsRight)
                            {
                                var leftMeshFile = subjectMeshFiles.FirstOrDefault(f => f.Same(meshFile) && f.IsLeft);
                                leftMeshFile ??= new BIDSMeshFile();
                                usedMeshFiles.Add(leftMeshFile);
                                
                                // Find corresponding MarsAtlas files for "white" meshes
                                string leftMarsAtlas = "";
                                string rightMarsAtlas = "";
                                if (meshFile.Name == "white")
                                {
                                    var leftMarsAtlasFile = subjectMeshFiles.FirstOrDefault(f => f.SameFields(meshFile) && f.IsMarsAtlasMesh && f.IsLeft);
                                    var rightMarsAtlasFile = subjectMeshFiles.FirstOrDefault(f => f.SameFields(meshFile) && f.IsMarsAtlasMesh && f.IsRight);
                                    
                                    leftMarsAtlas = leftMarsAtlasFile?.Path ?? "";
                                    rightMarsAtlas = rightMarsAtlasFile?.Path ?? "";
                                    
                                    // Mark MarsAtlas files as used
                                    if (leftMarsAtlasFile != null) usedMeshFiles.Add(leftMarsAtlasFile);
                                    if (rightMarsAtlasFile != null) usedMeshFiles.Add(rightMarsAtlasFile);
                                }
                                
                                meshes.Add(new LeftRightMesh(meshFile.Name, transformationPath, leftMeshFile.Path, meshFile.Path, leftMarsAtlas, rightMarsAtlas));
                            }
                            else
                            {
                                // Single mesh case
                                string marsAtlas = "";
                                if (meshFile.Name == "white")
                                {
                                    var marsAtlasFile = subjectMeshFiles.FirstOrDefault(f => f.SameFields(meshFile) && f.IsMarsAtlasMesh && f.Hemisphere == meshFile.Hemisphere);
                                    
                                    marsAtlas = marsAtlasFile?.Path ?? "";
                                    
                                    // Mark MarsAtlas file as used
                                    if (marsAtlasFile != null) usedMeshFiles.Add(marsAtlasFile);
                                }
                                
                                meshes.Add(new SingleMesh(meshFile.Name, transformationPath, meshFile.Path, marsAtlas));
                            }
                            usedMeshFiles.Add(meshFile);
                        }
                    }
                }

                // MRIs.
                List<MRI> mris = new List<MRI>();
                if (mriFilesBySubjectID.TryGetValue(pair.Key, out List<BIDSMRIFile> subjectMRIFiles))
                {
                    mris = subjectMRIFiles.Select(f => new MRI(string.Format("{0}{1}", f.Name, !string.IsNullOrEmpty(f.Session) ? string.Format(" ({0})", f.Session) : ""), f.Path)).ToList();
                }

                // Sites.
                List<Site> sites = new List<Site>();
                if (electrodesFilesBySubjectID.TryGetValue(pair.Key, out List<BIDSElectrodeFile> subjectElectrodesFiles))
                {
                    foreach (var electrodeFile in subjectElectrodesFiles)
                    {
                        (new Site() as ILoadable<Site>).LoadFromFile(electrodeFile.Path, out Site[] fileSites);
                        foreach (var site in fileSites)
                        {
                            Site existingSite = sites.FirstOrDefault(s => s.Name == site.Name);
                            if (existingSite != null)
                            {
                                existingSite.Coordinates.AddRange(site.Coordinates);
                                
                                // Add tags only if they don't already exist (check by Tag property)
                                foreach (var newTag in site.Tags)
                                {
                                    if (!existingSite.Tags.Any(existingTag => existingTag.Tag == newTag.Tag))
                                    {
                                        existingSite.Tags.Add(newTag);
                                    }
                                }
                            }
                            else
                            {
                                sites.Add(site);
                            }
                        }
                    }
                }

                // Tags.
                List<BaseTagValue> tags = new List<BaseTagValue>();
                if (tagValuesBySubjectID.TryGetValue(pair.Key, out Dictionary<string, string> subjectTags))
                {
                    // Add tags to project.
                    IEnumerable<BaseTag> projectTags = PersistentDataManager.Tags.PatientsTags.Concat(PersistentDataManager.Tags.GeneralTags);
                    foreach (var tagName in subjectTags.Keys)
                    {
                        if (!projectTags.Any(t => t.Name == tagName))
                        {
                            PersistentDataManager.Tags.AddPatientTag(new StringTag(tagName));
                        }
                    }
                    // Add tags to patient with the same invalid value filtering as Intranat
                    projectTags = PersistentDataManager.Tags.PatientsTags.Concat(PersistentDataManager.Tags.GeneralTags);
                    foreach (var subjectTag in subjectTags)
                    {
                        BaseTag tag = projectTags.FirstOrDefault(t => t.Name == subjectTag.Key);
                        if (tag != null && !IsInvalidTagValue(subjectTag.Value))
                        {
                            var tagValue = tag.CreateValue(subjectTag.Value);
                            if (tagValue != null)
                            {
                                tags.Add(tagValue);
                            }
                        }
                    }
                }

                // Create patient.
                Patient patient = new Patient(pair.Key, meshes, mris, sites, tags, databaseReference.ID, pair.Key);
                patientsList.Add(patient);
            }
            patients = patientsList.ToArray();
            updateProgress?.Invoke(1.0f, 0, new LoadingText("Patients loaded successfully"));
        }
        public static void LoadAdditionalTagsFromTagsDatabase(DatabaseReference databaseReference, List<Patient> patients, out Patient[] modifiedPatients, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            modifiedPatients = new Patient[0];
            Dictionary<string, Patient> modifiedPatientsDict = new Dictionary<string, Patient>();
            if (string.IsNullOrEmpty(databaseReference.Path)) return;
            DirectoryInfo databaseDirectoryInfo = new DirectoryInfo(databaseReference.Path);
            if (!databaseDirectoryInfo.Exists) return;

            FileInfo patientsFileInfo = new FileInfo(Path.Combine(databaseDirectoryInfo.FullName, "patients.csv"));
            FileInfo patientsExcelFileInfo = new FileInfo(Path.Combine(databaseDirectoryInfo.FullName, "patients.xlsx"));
            FileInfo[] patientsFiles = databaseDirectoryInfo.GetFiles("*.csv", SearchOption.TopDirectoryOnly);
            FileInfo[] patientsExcelFiles = databaseDirectoryInfo.GetFiles("*.xlsx", SearchOption.TopDirectoryOnly);
            DirectoryInfo[] patientDirectories = databaseDirectoryInfo.GetDirectories();
            var progress = 0;
            var length = patientsFiles.Length + patientsExcelFiles.Length + patientDirectories.Length + 2;
            updateProgress?.Invoke(0, 0, new LoadingText("Loading additional tags"));

            // Helper method to get or create patient clone
            Patient GetOrCreatePatientClone(string patientId)
            {
                if (modifiedPatientsDict.TryGetValue(patientId, out Patient existingClone))
                {
                    return existingClone;
                }
                
                var originalPatient = patients.FirstOrDefault(p => p.ID == patientId);
                if (originalPatient != null)
                {
                    var clonedPatient = originalPatient.Clone() as Patient;
                    modifiedPatientsDict[patientId] = clonedPatient;
                    return clonedPatient;
                }
                
                return null;
            }

            // Helper method to merge patient tags with invalid value filtering
            void MergePatientTags(string patientId, List<BaseTagValue> newTags)
            {
                var patient = GetOrCreatePatientClone(patientId);
                if (patient != null)
                {
                    foreach (var tagValue in newTags)
                    {
                        // Apply the same invalid value filtering
                        if (!IsInvalidTagValue(tagValue.DisplayableValue))
                        {
                            var existingTagValue = patient.Tags.FirstOrDefault(t => t.Tag.Name == tagValue.Tag.Name);
                            if (existingTagValue != null)
                            {
                                existingTagValue.Copy(tagValue);
                            }
                            else
                            {
                                patient.Tags.Add(tagValue);
                            }
                        }
                    }
                }
            }

            // Read patients.csv
            updateProgress?.Invoke((float)progress++ / length, 0, new LoadingText("Loading additional tags from patients.csv"));
            if (patientsFileInfo.Exists)
            {
                var tagValuesByPatient = PersistentDataManager.Tags.GeneratePatientTagsFromCSV(patientsFileInfo.FullName);
                foreach (var kv in tagValuesByPatient)
                {
                    MergePatientTags(kv.Key, kv.Value);
                }
            }

            // Read patients.xlsx
            updateProgress?.Invoke((float)progress++ / length, 0, new LoadingText("Loading additional tags from patients.xlsx"));
            if (patientsExcelFileInfo.Exists)
            {
                var tagValuesByPatient = PersistentDataManager.Tags.GeneratePatientTagsFromExcel(patientsExcelFileInfo.FullName);
                foreach (var kv in tagValuesByPatient)
                {
                    MergePatientTags(kv.Key, kv.Value);
                }
            }

            // Helper method to merge site tags with invalid value filtering
            void MergeSiteTags(string patientId, Dictionary<string, List<BaseTagValue>> tagValuesBySite)
            {
                var patient = GetOrCreatePatientClone(patientId);
                if (patient != null)
                {
                    foreach (var kv in tagValuesBySite)
                    {
                        var site = patient.Sites.FirstOrDefault(s => s.Name == kv.Key);
                        if (site != null)
                        {
                            foreach (var tagValue in kv.Value)
                            {
                                // Apply the same invalid value filtering
                                if (!IsInvalidTagValue(tagValue.DisplayableValue))
                                {
                                    var existingTagValue = site.Tags.FirstOrDefault(t => t.Tag.Name == tagValue.Tag.Name);
                                    if (existingTagValue != null)
                                    {
                                        existingTagValue.Copy(tagValue);
                                    }
                                    else
                                    {
                                        site.Tags.Add(tagValue);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Read all individual patient CSV files in root directory
            foreach (var file in patientsFiles)
            {
                updateProgress?.Invoke((float)progress++ / length, 0, new LoadingText("Loading additional tags from ", file.Name));
                token.ThrowIfCancellationRequested();
                if (file.Name == "patients.csv") continue;
                var tagValuesBySite = PersistentDataManager.Tags.GenerateSiteTagsFromCSV(file.FullName);
                string patientId = Path.GetFileNameWithoutExtension(file.Name);
                MergeSiteTags(patientId, tagValuesBySite);
            }

            // Read all individual patient Excel files in root directory
            foreach (var file in patientsExcelFiles)
            {
                updateProgress?.Invoke((float)progress++ / length, 0, new LoadingText("Loading additional tags from ", file.Name));
                token.ThrowIfCancellationRequested();
                if (file.Name == "patients.xlsx") continue;
                var tagValuesBySite = PersistentDataManager.Tags.GenerateSiteTagsFromExcel(file.FullName);
                string patientId = Path.GetFileNameWithoutExtension(file.Name);
                MergeSiteTags(patientId, tagValuesBySite);
            }

            // Read all CSV and Excel files in patient-specific directories
            foreach (var directory in patientDirectories)
            {
                updateProgress?.Invoke((float)progress++ / length, 0, new LoadingText("Loading additional tags from directory ", directory.Name));
                token.ThrowIfCancellationRequested();

                string patientId = directory.Name;
                
                // Get all CSV and Excel files in this patient directory
                FileInfo[] csvFilesInDirectory = directory.GetFiles("*.csv", SearchOption.AllDirectories);
                FileInfo[] excelFilesInDirectory = directory.GetFiles("*.xlsx", SearchOption.AllDirectories);

                // Process CSV files
                foreach (var csvFile in csvFilesInDirectory)
                {
                    var siteTagsFromFile = PersistentDataManager.Tags.GenerateSiteTagsFromCSV(csvFile.FullName);
                    MergeSiteTags(patientId, siteTagsFromFile);
                }

                // Process Excel files
                foreach (var excelFile in excelFilesInDirectory)
                {
                    var siteTagsFromFile = PersistentDataManager.Tags.GenerateSiteTagsFromExcel(excelFile.FullName);
                    MergeSiteTags(patientId, siteTagsFromFile);
                }
            }

            modifiedPatients = modifiedPatientsDict.Values.ToArray();
        }
        /// <summary>
        /// Coroutine to load patients from database. Implementation of ILoadableFromDatabase.
        /// </summary>
        /// <param name="path">The specified path of the patient file.</param>
        /// <param name="OnChangeProgress">Action called on change progress.</param>
        /// <param name="result">The patients loaded.</param>
        /// <returns></returns>
        public static async UniTask<IEnumerable<Patient>> LoadFromDatabaseAsync(Action<float, float, LoadingText> updateProgress, Func<Patient, bool> filter)
        {
            updateProgress(0, 0, new LoadingText("Loading database"));
            await UniTask.WaitUntil(() => DatabaseManager.Database.IsLoaded);
            await UniTask.SwitchToThreadPool();
            var result = new List<Patient>();
            int length = DatabaseManager.Database.Patients.Count;
            int progress = 0;
            foreach (var patient in DatabaseManager.Database.Patients)
            {
                updateProgress((float)progress++ / length, 0, new LoadingText("Loading patients"));
                if (filter(patient)) result.Add(patient);
            }
            return result;
        }
        /// <summary>
        /// Coroutine to load patients from database. Implementation of ILoadableFromDatabase.
        /// </summary>
        /// <param name="paths">The specified path of the patient file.</param>
        /// <param name="updateProgress">Action called on change progress.</param>
        /// <param name="result">The patients loaded.</param>
        /// <returns></returns>
        public static async UniTask<IEnumerable<Patient>> LoadFromDirectoryAsync(string[] paths, Action<float, float, LoadingText> updateProgress)
        {
            List<Patient> patients = new List<Patient>(paths.Length);
            await UniTask.SwitchToThreadPool();
            foreach (var path in paths)
            {
                if (LoadFromDirectory(path, out Patient patient))
                {
                    patients.Add(patient);
                }
            }
            return patients;
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone the instance.
        /// </summary>
        /// <returns>object cloned.</returns>
        public override object Clone()
        {
            return new Patient(Name, Meshes.DeepClone(), MRIs.DeepClone(), Sites.DeepClone(), Tags.DeepClone(), CorrespondingDatabaseID, ID);
        }
        /// <summary>
        /// Copy the instance.
        /// </summary>
        /// <param name="obj">instance to copy.</param>
        public override void Copy(object obj)
        {
            base.Copy(obj);
            if (obj is Patient patient)
            {
                Name = patient.Name;
                Meshes = patient.Meshes;
                MRIs = patient.MRIs;
                Sites = patient.Sites;
                Tags = patient.Tags;
                CorrespondingDatabaseID = patient.CorrespondingDatabaseID;
            }
        }
        #endregion

        #region Interfaces
        string[] ILoadable<Patient>.GetExtensions()
        {
            return GetExtensions();
        }
        bool ILoadable<Patient>.LoadFromFile(string path, out Patient[] result)
        {
            bool success = LoadFromFile(path, out Patient patient);
            result = new Patient[] { patient };
            return success;
        }
        async UniTask<IEnumerable<Patient>> ILoadableFromDatabase<Patient>.LoadFromDatabaseAsync(Action<float, float, LoadingText> updateProgress, Func<Patient, bool> filter)
        {
            return await LoadFromDatabaseAsync(updateProgress, filter);
        }
        async UniTask<IEnumerable<Patient>> ILoadableFromDirectory<Patient>.LoadFromDirectory(string[] paths, Action<float, float, LoadingText> updateProgress)
        {
            return await LoadFromDirectoryAsync(paths, updateProgress);
        }
        #endregion

        class BIDSFile
        {
            public string Name = "";
            public string Subject = "";
            public string Session = "";
            public string DataAcquisition = "";
            public string Contrast = "";
            public string Reconstruction = "";
            public int Run = 0;
            public string Path = "";

            public bool Same(BIDSFile file)
            {
                return file.Name == Name && file.Subject == Subject && file.Session == Session && file.DataAcquisition == DataAcquisition && file.Contrast == Contrast && file.Reconstruction == Reconstruction && file.Run == Run;
            }
            public bool SameFields(BIDSFile file)
            {
                return file.Subject == Subject && file.Session == Session && file.DataAcquisition == DataAcquisition && file.Contrast == Contrast && file.Reconstruction == Reconstruction && file.Run == Run;
            }
        }
        class BIDSMeshFile : BIDSFile
        {
            public string Description;
            public string Hemisphere;

            public bool IsMarsAtlasMesh => Name == "dseg" && Description.ToLower() == "marsatlas";
            public bool IsLeft => Hemisphere == "L" || Hemisphere == "l" || Hemisphere == "left" || Hemisphere == "Left";
            public bool IsRight => Hemisphere == "R" || Hemisphere == "r" || Hemisphere == "right" || Hemisphere == "Right";

            public string FindTransformationFile()
            {
                if (string.IsNullOrEmpty(Path)) return "";
                string directory = System.IO.Path.GetDirectoryName(Path);
                if (string.IsNullOrEmpty(directory)) return "";

                var trmFiles = Directory.GetFiles(directory, "*.trm", SearchOption.TopDirectoryOnly);
                return trmFiles.Length > 0 ? trmFiles[0] : "";
            }
        }
        class BIDSMRIFile : BIDSFile
        {

        }
        class BIDSElectrodeFile : BIDSFile
        {
            public string Space;
        }
    }
}