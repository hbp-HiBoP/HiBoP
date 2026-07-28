using Cysharp.Threading.Tasks;
using HBP.Core.Exceptions;
using HBP.Core.Interfaces;
using HBP.Core.Tools;
using HBP.Core.Database;
using HBP.Core.Preferences;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
        [JsonProperty("AssetValidationState")]
        private ValidationState m_AssetValidationState;
        [JsonIgnore]
        public ValidationState AssetValidationState =>
            m_AssetValidationState?.Clone();
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
            List<BaseTagValue> patientTagsToRemove = new();
            foreach (var tag in Tags)
                if (tag.Tag == null)
                    patientTagsToRemove.Add(tag);
            foreach (var tag in patientTagsToRemove)
                Tags.Remove(tag);

            // Site tags
            foreach (var site in Sites)
            {
                List<BaseTagValue> siteTagsToRemove = new();
                foreach (var tag in site.Tags)
                    if (tag.Tag == null)
                        siteTagsToRemove.Add(tag);
                foreach (var tag in siteTagsToRemove)
                    site.Tags.Remove(tag);
            }
        }

        public bool IsAssetValidationCurrent =>
            m_AssetValidationState != null &&
            (m_AssetValidationState.Status == ValidationStatus.Current ||
                m_AssetValidationState.Status ==
                    ValidationStatus.NotApplicable);

        public void MarkAssetValidationStale()
        {
            if (m_AssetValidationState != null &&
                m_AssetValidationState.Status !=
                    ValidationStatus.NotApplicable)
            {
                m_AssetValidationState =
                    m_AssetValidationState.WithStatus(
                        ValidationStatus.Stale);
            }
        }

        internal void ApplyAssetValidationState(
            ValidationState state)
        {
            m_AssetValidationState = state?.Clone();
        }
        public UniTask CheckTagsAsync(IEnumerable<BaseTag> tags)
        {
            if (tags == null)
            {
                throw new ArgumentNullException(nameof(tags));
            }

            HashSet<string> tagIds = new(
                tags.Where(tag => tag != null && !string.IsNullOrEmpty(tag.ID)).Select(tag => tag.ID),
                StringComparer.Ordinal);
            return CheckTagsAsync(tagIds);
        }
        public async UniTask CheckTagsAsync(ISet<string> tagIds)
        {
            if (tagIds == null)
            {
                throw new ArgumentNullException(nameof(tagIds));
            }

            await UniTask.SwitchToThreadPool();
            Tags.RemoveAll(t => t.Tag == null || !tagIds.Contains(t.Tag.ID));
            foreach (var site in Sites) site.Tags.RemoveAll(t => t.Tag == null || !tagIds.Contains(t.Tag.ID));
            List<BaseTagValue> tagsToUpdate = Tags.Where(t => t.Tag != null && tagIds.Contains(t.Tag.ID)).ToList();
            tagsToUpdate.AddRange(Sites.SelectMany(s => s.Tags).Where(t => t.Tag != null && tagIds.Contains(t.Tag.ID)));
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
            DirectoryInfo directory = new(path);
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
                DirectoryInfo directory = new(path);
                string patientName;
                List<BaseTagValue> patientTags = new();
                
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
                if (result == null)
                {
                    return false;
                }
                new LoadingContext(
                    PersistentDataManager.Tags.AllTags,
                    Array.Empty<Protocol>(),
                    new[] { result })
                    .ResolveDatabase(new[] { result }, Array.Empty<DataInfo>());
                result.CleanInvalidData();
                return true;
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
            DirectoryInfo directory = new(databaseReference.Path);
            if (!directory.Exists) return;

            IEnumerable<DirectoryInfo> patientDirectories = directory.GetDirectories().Where(d => IsPatientDirectory(d.FullName));
            int length = patientDirectories.Count();
            int progress = 0;
            List<Patient> patientsList = new(length);
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
            DirectoryInfo databaseDirectoryInfo = new(databaseReference.Path);
            if (!databaseDirectoryInfo.Exists) return;

            // Read participants.tsv.
            updateProgress?.Invoke(0, 0, new LoadingText("Reading participants.tsv file"));
            FileInfo participantsFileInfo = new(Path.Combine(databaseDirectoryInfo.FullName, "participants.tsv"));
            if (!participantsFileInfo.Exists) throw new HBPException("Missing file", "The mandatory file 'participants.tsv' is missing in the BIDS database directory.");
            Dictionary<string, Dictionary<string, string>> tagValuesBySubjectID = new();
            using (StreamReader streamReader = new(participantsFileInfo.FullName))
            {
                string[] lines = streamReader.ReadToEnd().Split(new string[] { Environment.NewLine, "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length == 0) return;
                string[] tags = lines[0].Split(new char[] { '\t' });
                for (int l = 1; l < lines.Length; l++)
                {
                    string[] values = lines[l].Split(new char[] { '\t' });
                    if (values.Length == tags.Length)
                    {
                        Dictionary<string, string> valueByTag = new();
                        for (int t = 1; t < tags.Length; t++)
                        {
                            valueByTag.Add(tags[t], values[t]);
                        }
                        tagValuesBySubjectID.Add(values[0], valueByTag);
                    }
                }
            }

            // Find mesh files.
            Dictionary<string, List<BIDSFile>> meshesFilesBySubjectID = new();
            foreach (var meshFile in BIDSParser.FindFiles(databaseDirectoryInfo.FullName, null, new[] { ".gii" }))
            {
                token.ThrowIfCancellationRequested();
                string meshSubjectId = "sub-" + meshFile.Entities["sub"];
                if (meshesFilesBySubjectID.TryGetValue(meshSubjectId, out List<BIDSFile> meshList))
                    meshList.Add(meshFile);
                else
                    meshesFilesBySubjectID[meshSubjectId] = new List<BIDSFile>() { meshFile };
            }

            // Find MRI files.
            string[] mriSuffixes = { "T1w", "T2w", "T1rho", "T1map", "T2map", "T2star", "FLAIR", "FLASH", "PD", "PDmap", "PDT2", "inplaneT1", "inplaneT2", "angio" };
            Dictionary<string, List<BIDSFile>> mriFilesBySubjectID = new();
            foreach (var mriFile in BIDSParser.FindFiles(databaseDirectoryInfo.FullName, mriSuffixes, new[] { ".nii", ".nii.gz" }))
            {
                token.ThrowIfCancellationRequested();
                string mriSubjectId = "sub-" + mriFile.Entities["sub"];
                if (mriFilesBySubjectID.TryGetValue(mriSubjectId, out List<BIDSFile> mriList))
                    mriList.Add(mriFile);
                else
                    mriFilesBySubjectID[mriSubjectId] = new List<BIDSFile>() { mriFile };
            }

            // Find Electrodes files.
            Dictionary<string, List<BIDSFile>> electrodesFilesBySubjectID = new();
            foreach (var electrodeFile in BIDSParser.FindFiles(databaseDirectoryInfo.FullName, new[] { "electrodes" }, new[] { ".tsv" }))
            {
                token.ThrowIfCancellationRequested();
                string electrodeSubjectId = "sub-" + electrodeFile.Entities["sub"];
                if (electrodesFilesBySubjectID.TryGetValue(electrodeSubjectId, out List<BIDSFile> electrodeList))
                    electrodeList.Add(electrodeFile);
                else
                    electrodesFilesBySubjectID[electrodeSubjectId] = new List<BIDSFile>() { electrodeFile };
            }

            // Create patients.
            int length = tagValuesBySubjectID.Count;
            int progress = 0;
            List<Patient> patientsList = new(tagValuesBySubjectID.Count);
            foreach (var pair in tagValuesBySubjectID)
            {
                token.ThrowIfCancellationRequested();
                updateProgress?.Invoke((float)progress++ / length, 0, new LoadingText("Loading patient ", pair.Key, " [" + (progress + 1) + "/" + length + "]"));

                // Meshes.
                List<BaseMesh> meshes = new();
                if (meshesFilesBySubjectID.TryGetValue(pair.Key, out List<BIDSFile> subjectMeshFiles))
                {
                    static bool IsMarsAtlasMesh(BIDSFile f)
                    {
                        if (f.Suffix != "dseg") return false;
                        f.Entities.TryGetValue("desc", out string d);
                        return d != null && d.Equals("marsatlas", StringComparison.OrdinalIgnoreCase);
                    }

                    static bool IsLeft(BIDSFile f)
                    {
                        f.Entities.TryGetValue("hemi", out string h);
                        return h == "L" || h == "l" || h == "left" || h == "Left";
                    }

                    static bool IsRight(BIDSFile f)
                    {
                        f.Entities.TryGetValue("hemi", out string h);
                        return h == "R" || h == "r" || h == "right" || h == "Right";
                    }

                    static string FindTransformationFile(BIDSFile f)
                    {
                        if (string.IsNullOrEmpty(f.Path)) return "";
                        string directory = System.IO.Path.GetDirectoryName(f.Path);
                        if (string.IsNullOrEmpty(directory)) return "";
                        foreach (var trmFile in Directory.GetFiles(directory, "*.trm", SearchOption.TopDirectoryOnly))
                        {
                            string trmSuffix = System.IO.Path.GetFileNameWithoutExtension(trmFile).Split('_').LastOrDefault();
                            if (trmSuffix == f.Suffix) return trmFile;
                        }
                        return "";
                    }

                    string[] sameEntityKeys = { "sub", "ses", "acq", "ce", "rec", "run" };

                    List<BIDSFile> usedMeshFiles = new(subjectMeshFiles.Count);

                    foreach (var meshFile in subjectMeshFiles)
                    {
                        if (!usedMeshFiles.Contains(meshFile) && !IsMarsAtlasMesh(meshFile))
                        {
                            string transformationPath = FindTransformationFile(meshFile);

                            if (IsLeft(meshFile))
                            {
                                var rightMeshFile = subjectMeshFiles.FirstOrDefault(f => f.HasSameEntities(meshFile, sameEntityKeys, includeSuffix: true) && IsRight(f));
                                string rightPath = rightMeshFile?.Path ?? "";
                                if (rightMeshFile != null) usedMeshFiles.Add(rightMeshFile);

                                // Find corresponding MarsAtlas files for "white" meshes
                                string leftMarsAtlas = "";
                                string rightMarsAtlas = "";
                                if (meshFile.Suffix == "white")
                                {
                                    var leftMarsAtlasFile = subjectMeshFiles.FirstOrDefault(f => f.HasSameEntities(meshFile, sameEntityKeys) && IsMarsAtlasMesh(f) && IsLeft(f));
                                    var rightMarsAtlasFile = subjectMeshFiles.FirstOrDefault(f => f.HasSameEntities(meshFile, sameEntityKeys) && IsMarsAtlasMesh(f) && IsRight(f));
                                    leftMarsAtlas = leftMarsAtlasFile?.Path ?? "";
                                    rightMarsAtlas = rightMarsAtlasFile?.Path ?? "";
                                    if (leftMarsAtlasFile != null) usedMeshFiles.Add(leftMarsAtlasFile);
                                    if (rightMarsAtlasFile != null) usedMeshFiles.Add(rightMarsAtlasFile);
                                }

                                meshes.Add(new LeftRightMesh(meshFile.Suffix, transformationPath, meshFile.Path, rightPath, leftMarsAtlas, rightMarsAtlas));
                            }
                            else if (IsRight(meshFile))
                            {
                                var leftMeshFile = subjectMeshFiles.FirstOrDefault(f => f.HasSameEntities(meshFile, sameEntityKeys, includeSuffix: true) && IsLeft(f));
                                string leftPath = leftMeshFile?.Path ?? "";
                                if (leftMeshFile != null) usedMeshFiles.Add(leftMeshFile);

                                // Find corresponding MarsAtlas files for "white" meshes
                                string leftMarsAtlas = "";
                                string rightMarsAtlas = "";
                                if (meshFile.Suffix == "white")
                                {
                                    var leftMarsAtlasFile = subjectMeshFiles.FirstOrDefault(f => f.HasSameEntities(meshFile, sameEntityKeys) && IsMarsAtlasMesh(f) && IsLeft(f));
                                    var rightMarsAtlasFile = subjectMeshFiles.FirstOrDefault(f => f.HasSameEntities(meshFile, sameEntityKeys) && IsMarsAtlasMesh(f) && IsRight(f));
                                    leftMarsAtlas = leftMarsAtlasFile?.Path ?? "";
                                    rightMarsAtlas = rightMarsAtlasFile?.Path ?? "";
                                    if (leftMarsAtlasFile != null) usedMeshFiles.Add(leftMarsAtlasFile);
                                    if (rightMarsAtlasFile != null) usedMeshFiles.Add(rightMarsAtlasFile);
                                }

                                meshes.Add(new LeftRightMesh(meshFile.Suffix, transformationPath, leftPath, meshFile.Path, leftMarsAtlas, rightMarsAtlas));
                            }
                            else
                            {
                                // Single mesh case
                                string marsAtlas = "";
                                if (meshFile.Suffix == "white")
                                {
                                    meshFile.Entities.TryGetValue("hemi", out string meshHemi);
                                    var marsAtlasFile = subjectMeshFiles.FirstOrDefault(f =>
                                    {
                                        if (!f.HasSameEntities(meshFile, sameEntityKeys) || !IsMarsAtlasMesh(f)) return false;
                                        f.Entities.TryGetValue("hemi", out string h);
                                        return h == meshHemi;
                                    });
                                    marsAtlas = marsAtlasFile?.Path ?? "";
                                    if (marsAtlasFile != null) usedMeshFiles.Add(marsAtlasFile);
                                }

                                meshes.Add(new SingleMesh(meshFile.Suffix, transformationPath, meshFile.Path, marsAtlas));
                            }
                            usedMeshFiles.Add(meshFile);
                        }
                    }
                }

                // MRIs.
                List<MRI> mris = new();
                if (mriFilesBySubjectID.TryGetValue(pair.Key, out List<BIDSFile> subjectMRIFiles))
                {
                    mris = subjectMRIFiles.Select(f =>
                    {
                        f.Entities.TryGetValue("ses", out string ses);
                        string mriName = string.Format("{0}{1}", f.Suffix, !string.IsNullOrEmpty(ses) ? string.Format(" ({0})", ses) : "");
                        return new MRI(mriName, f.Path);
                    }).ToList();
                }

                // Sites.
                List<Site> sites = new();
                if (electrodesFilesBySubjectID.TryGetValue(pair.Key, out List<BIDSFile> subjectElectrodesFiles))
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
                List<BaseTagValue> tags = new();
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
                Patient patient = new(pair.Key, meshes, mris, sites, tags, databaseReference.ID, pair.Key);
                patientsList.Add(patient);
            }
            patients = patientsList.ToArray();
            updateProgress?.Invoke(1.0f, 0, new LoadingText("Patients loaded successfully"));
        }
        public static void LoadAdditionalTagsFromTagsDatabase(DatabaseReference databaseReference, List<Patient> patients, out Patient[] modifiedPatients, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            modifiedPatients = new Patient[0];
            Dictionary<string, Patient> modifiedPatientsDict = new();
            if (string.IsNullOrEmpty(databaseReference.Path)) return;
            DirectoryInfo databaseDirectoryInfo = new(databaseReference.Path);
            if (!databaseDirectoryInfo.Exists) return;

            FileInfo patientsFileInfo = new(Path.Combine(databaseDirectoryInfo.FullName, "patients.csv"));
            FileInfo patientsExcelFileInfo = new(Path.Combine(databaseDirectoryInfo.FullName, "patients.xlsx"));
            DirectoryInfo patientsDirectoryInfo = new(Path.Combine(databaseDirectoryInfo.FullName, "patients"));
            FileInfo[] patientsFiles = databaseDirectoryInfo.GetFiles("*.csv", SearchOption.TopDirectoryOnly);
            FileInfo[] patientsExcelFiles = databaseDirectoryInfo.GetFiles("*.xlsx", SearchOption.TopDirectoryOnly);
            DirectoryInfo[] patientDirectories = databaseDirectoryInfo.GetDirectories();
            var progress = 0;
            var length = patientsFiles.Length + patientsExcelFiles.Length + patientDirectories.Length + 3;
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

            // Read patients folder
            updateProgress?.Invoke((float)progress++ / length, 0, new LoadingText("Loading additional tags from patients folder"));
            if (patientsDirectoryInfo.Exists)
            {
                // Get all CSV and Excel files in the patients directory
                FileInfo[] patientsCsvFiles = patientsDirectoryInfo.GetFiles("*.csv", SearchOption.AllDirectories);
                FileInfo[] patientsExcelFilesInDir = patientsDirectoryInfo.GetFiles("*.xlsx", SearchOption.AllDirectories);

                // Process CSV files
                foreach (var csvFile in patientsCsvFiles)
                {
                    var tagValuesByPatient = PersistentDataManager.Tags.GeneratePatientTagsFromCSV(csvFile.FullName);
                    foreach (var kv in tagValuesByPatient)
                    {
                        MergePatientTags(kv.Key, kv.Value);
                    }
                }

                // Process Excel files
                foreach (var excelFile in patientsExcelFilesInDir)
                {
                    var tagValuesByPatient = PersistentDataManager.Tags.GeneratePatientTagsFromExcel(excelFile.FullName);
                    foreach (var kv in tagValuesByPatient)
                    {
                        MergePatientTags(kv.Key, kv.Value);
                    }
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
            GlobalDatabase database = DatabaseManager.Database;
            float databaseWeight = database.NeedsReadyWait ? 0.8f : 0;
            if (databaseWeight > 0)
            {
                await database.EnsureDatabaseReadyAsync(
                    (progress, duration, text) => updateProgress(
                        progress * databaseWeight,
                        duration,
                        text));
            }
            await UniTask.SwitchToThreadPool();
            var result = new List<Patient>();
            int length = database.Patients.Count;
            int progress = 0;
            foreach (var patient in database.Patients)
            {
                updateProgress(
                    databaseWeight +
                        (length == 0 ? 1 : (float)progress++ / length) *
                        (1 - databaseWeight),
                    0,
                    new LoadingText("Loading patients"));
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
            List<Patient> patients = new(paths.Length);
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
            Patient clone = new(
                Name,
                Meshes.DeepClone(),
                MRIs.DeepClone(),
                Sites.DeepClone(),
                Tags.DeepClone(),
                CorrespondingDatabaseID,
                ID);
            clone.m_AssetValidationState =
                m_AssetValidationState?.Clone();
            return clone;
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
                m_AssetValidationState =
                    patient.m_AssetValidationState?.Clone();
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

    }
}
