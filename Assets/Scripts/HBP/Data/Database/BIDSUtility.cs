using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Data.Container;
using HBP.Core.Tools;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace HBP.Data.BIDS
{
    public static class BIDSUtility
    {
        public static List<BIDSPatient> CreateBIDSPatients(IEnumerable<Patient> patients, IEnumerable<Protocol> protocols, IEnumerable<string> dataNames, bool anonymize = false)
        {
            var bidsPatients = new List<BIDSPatient>();

            if (anonymize)
            {
                // Anonymized mode: randomize order and assign zero-padded numbers
                var randomizedPatients = patients.OrderBy(x => System.Guid.NewGuid()).ToList();

                for (int i = 0; i < randomizedPatients.Count; i++)
                {
                    var bidsPatient = BIDSPatient.CreateAnonymized(randomizedPatients[i], protocols, dataNames, i + 1);
                    bidsPatients.Add(bidsPatient);
                }
            }
            else
            {
                // Non-anonymized mode: use alphanumeric-only patient names
                foreach (var patient in patients)
                {
                    var bidsPatient = BIDSPatient.CreateNonAnonymized(patient, protocols, dataNames);
                    bidsPatients.Add(bidsPatient);
                }
            }

            return bidsPatients;
        }

        public static async UniTask ExportCorrespondenceTableAsync(List<BIDSPatient> bidsPatients, List<Patient> originalPatients, string filePath)
        {
            var lines = new List<string>
            {
                "BIDS_ID,Original_Patient_ID"
            };
            
            foreach (var bidsPatient in bidsPatients)
            {
                if (bidsPatient.Patient != null)
                {
                    lines.Add($"{bidsPatient.ParticipantId},{bidsPatient.Patient.ID}");
                }
            }
            
            await File.WriteAllLinesAsync(filePath, lines);
        }

        public static async UniTask<string> CreateRootDirectoryAndFilesAsync(string datasetName, IEnumerable<BIDSPatient> patients, string baseFolder, IEnumerable<BaseTag> selectedPatientTags)
        {
            // Create root directory
            string datasetPath = Path.Combine(baseFolder, datasetName);
            if (Directory.Exists(datasetPath)) Directory.Delete(datasetPath, true);
            Directory.CreateDirectory(datasetPath);

            // Create dataset_description.json
            await UniTask.SwitchToMainThread();
            var datasetDescription = new DatasetDescriptionFile(datasetName);
            await UniTask.SwitchToThreadPool();
            ClassLoaderSaver.SaveToJSon(datasetDescription, Path.Combine(datasetPath, "dataset_description.json"), true);

            // Create participants.tsv
            string participantsTsv = PatientsToParticipantsTSV(patients, selectedPatientTags);
            string participantsTsvPath = Path.Combine(datasetPath, "participants.tsv");
            await File.WriteAllTextAsync(participantsTsvPath, participantsTsv);

            return datasetPath;
        }

        public static async UniTask ExportPatientAsync(BIDSPatient patient, string datasetFolder, BIDSExportConfiguration config, IEnumerable<BaseTag> selectedSiteTags)
        {
            // Create directories based on rules (dynamically determined from config)
            var sessionsNeeded = config.AnatomicalRules.Select(r => r.BIDSSession).Distinct();
            
            foreach (var session in sessionsNeeded)
            {
                var sessionDir = Directory.CreateDirectory(Path.Combine(datasetFolder, patient.ParticipantId, $"ses-{session}"));
                var anatDir = Directory.CreateDirectory(Path.Combine(sessionDir.FullName, "anat"));
            }
            
            // Hardcoded post session for IEEG (as per requirements)
            var postIeegDirectory = Directory.CreateDirectory(
                Path.Combine(datasetFolder, patient.ParticipantId, "ses-post", "ieeg"));
            
            // Export using rules
            ExportAnatomicalData(patient, datasetFolder, config);
            CreateElectrodesFiles(patient, postIeegDirectory.FullName, config, selectedSiteTags);
            CreateFunctionalFiles(patient, postIeegDirectory.FullName);
        }

        private static string PatientsToParticipantsTSV(IEnumerable<BIDSPatient> bidsPatients, IEnumerable<BaseTag> selectedPatientTags)
        {
            if (bidsPatients == null || !bidsPatients.Any())
            {
                return "participant_id\n";
            }

            var bidsPatientsList = bidsPatients.ToList();
            var selectedTagsList = selectedPatientTags?.ToList() ?? new List<BaseTag>();

            // Step 1: Collect all distinct tags from all patients that are in the selected tags list
            var allTags = new HashSet<BaseTag>();
            foreach (var bidsPatient in bidsPatientsList)
            {
                if (bidsPatient.Patient.Tags != null)
                {
                    foreach (var tagValue in bidsPatient.Patient.Tags)
                    {
                        if (tagValue.Tag != null && selectedTagsList.Contains(tagValue.Tag))
                        {
                            allTags.Add(tagValue.Tag);
                        }
                    }
                }
            }

            // Step 2: Convert tag names to snake_case and create headers
            var headers = new List<string> { "participant_id" }; // Start with mandatory participant_id column
            var tagToHeaderMap = new Dictionary<BaseTag, string>();

            foreach (var tag in allTags.OrderBy(t => t.Name))
            {
                string headerName = tag.Name.ToSnakeCase();
                headers.Add(headerName);
                tagToHeaderMap[tag] = headerName;
            }

            // Step 3: Build TSV content
            var tsvBuilder = new StringBuilder();

            // Add header line
            tsvBuilder.AppendLine(string.Join("\t", headers));

            // Step 4: Add data lines for each BIDS patient
            foreach (var bidsPatient in bidsPatientsList)
            {
                var rowValues = new List<string>();

                // Add participant_id (using BIDS patient ID)
                rowValues.Add(bidsPatient.ParticipantId);

                // Add tag values or "n/a" for missing tags
                foreach (var tag in allTags.OrderBy(t => t.Name))
                {
                    string value = "";

                    if (bidsPatient.Patient.Tags != null)
                    {
                        var tagValue = bidsPatient.Patient.Tags.FirstOrDefault(tv => tv.Tag == tag);
                        if (tagValue != null && tagValue.DisplayableValue != null)
                        {
                            value = tagValue.DisplayableValue.DeblankCompletely();
                        }
                    }

                    if (string.IsNullOrEmpty(value))
                    {
                        // Handle specific cases for empty values
                        if (tag.Name.ToLower() == "sex")
                            value = "o";
                        else
                            value = "n/a";
                    }

                    rowValues.Add(value);
                }

                tsvBuilder.AppendLine(string.Join("\t", rowValues));
            }

            return tsvBuilder.ToString();
        }
        
        private static void ExportAnatomicalData(BIDSPatient patient, string datasetFolder, BIDSExportConfiguration config)
        {
            // Group rules by session
            var rulesBySession = config.AnatomicalRules.GroupBy(r => r.BIDSSession);
            
            foreach (var sessionGroup in rulesBySession)
            {
                string sessionName = sessionGroup.Key;
                var anatDir = Path.Combine(datasetFolder, patient.ParticipantId, $"ses-{sessionName}", "anat");
                
                foreach (var rule in sessionGroup)
                {
                    if (rule.DataType == "MRI")
                    {
                        ExportMRI(patient, anatDir, rule);
                    }
                    else if (rule.DataType == "Mesh")
                    {
                        ExportMesh(patient, anatDir, rule);
                    }
                }
            }
        }
        
        private static void ExportMRI(BIDSPatient patient, string destinationFolder, AnatomicalDataRule rule)
        {
            var mri = patient.Patient.MRIs.FirstOrDefault(m => m.Name == rule.SourceName);
            if (mri == null) return;
            
            string filename = $"{patient.ParticipantId}_ses-{rule.BIDSSession}_{rule.BIDSSuffix}.nii";
            File.Copy(mri.File, Path.Combine(destinationFolder, filename), true);
        }
        
        private static void ExportMesh(BIDSPatient patient, string destinationFolder, AnatomicalDataRule rule)
        {
            var mesh = patient.Patient.Meshes.FirstOrDefault(m => m.Name == rule.SourceName);
            if (mesh == null) return;
            
            if (mesh is SingleMesh singleMesh)
            {
                string meshFilename = $"{patient.ParticipantId}_ses-{rule.BIDSSession}_{rule.BIDSSuffix}.surf.gii";
                File.Copy(singleMesh.Path, Path.Combine(destinationFolder, meshFilename), true);
                
                if (singleMesh.HasMarsAtlas)
                {
                    string atlasFilename = $"{patient.ParticipantId}_ses-{rule.BIDSSession}_desc-marsatlas_dseg.label.gii";
                    File.Copy(singleMesh.MarsAtlasPath, Path.Combine(destinationFolder, atlasFilename), true);
                }
            }
            else if (mesh is LeftRightMesh leftRightMesh)
            {
                string leftFilename = $"{patient.ParticipantId}_ses-{rule.BIDSSession}_hemi-L_{rule.BIDSSuffix}.surf.gii";
                string rightFilename = $"{patient.ParticipantId}_ses-{rule.BIDSSession}_hemi-R_{rule.BIDSSuffix}.surf.gii";
                File.Copy(leftRightMesh.LeftHemisphere, Path.Combine(destinationFolder, leftFilename), true);
                File.Copy(leftRightMesh.RightHemisphere, Path.Combine(destinationFolder, rightFilename), true);
                
                if (leftRightMesh.HasMarsAtlas)
                {
                    string leftAtlasFilename = $"{patient.ParticipantId}_ses-{rule.BIDSSession}_desc-marsatlas_hemi-L_dseg.label.gii";
                    string rightAtlasFilename = $"{patient.ParticipantId}_ses-{rule.BIDSSession}_desc-marsatlas_hemi-R_dseg.label.gii";
                    File.Copy(leftRightMesh.LeftMarsAtlasHemisphere, Path.Combine(destinationFolder, leftAtlasFilename), true);
                    File.Copy(leftRightMesh.RightMarsAtlasHemisphere, Path.Combine(destinationFolder, rightAtlasFilename), true);
                }
            }
            
            // Export transformation if exists
            if (mesh.HasTransformation)
            {
                string trmFilename = $"{patient.ParticipantId}_ses-{rule.BIDSSession}_{rule.BIDSSuffix}.trm";
                File.Copy(mesh.Transformation, Path.Combine(destinationFolder, trmFilename), true);
            }
        }
        
        private static string SitesToElectrodesTSV(IEnumerable<Site> sites, string coordSystemString, IEnumerable<BaseTag> selectedSiteTags)
        {
            if (sites == null || !sites.Any())
            {
                return "name\tx\ty\tz\tsize\tmaterial\tmanufacturer\tgroup\themisphere\n";
            }

            var sitesList = sites.ToList();
            var selectedTagsList = selectedSiteTags?.ToList() ?? new List<BaseTag>();

            // Sort sites using SiteNameComparer
            var siteNameComparer = new SiteNameComparer();
            sitesList.Sort((a, b) => siteNameComparer.Compare(a.Name, b.Name));

            // Step 1: Collect all distinct tags from all sites that are in the selected tags list
            var allTags = new HashSet<BaseTag>();
            foreach (var site in sitesList)
            {
                if (site.Tags != null)
                {
                    foreach (var tagValue in site.Tags)
                    {
                        if (tagValue.Tag != null && selectedTagsList.Contains(tagValue.Tag))
                        {
                            allTags.Add(tagValue.Tag);
                        }
                    }
                }
            }

            // Step 2: Create headers - fixed fields first, then tag names converted to snake_case
            var headers = new List<string> { "name", "x", "y", "z", "size", "material", "manufacturer", "group", "hemisphere" };
            var tagToHeaderMap = new Dictionary<BaseTag, string>();

            foreach (var tag in allTags.OrderBy(t => t.Name))
            {
                string headerName = tag.Name.ToSnakeCase();
                headers.Add(headerName);
                tagToHeaderMap[tag] = headerName;
            }

            // Step 3: Build TSV content
            var tsvBuilder = new StringBuilder();

            // Add header line
            tsvBuilder.AppendLine(string.Join("\t", headers));

            // Step 4: Add data lines for each site
            foreach (var site in sitesList)
            {
                var rowValues = new List<string>();

                // Fix and add name using SiteTools.FixName if SiteNameCorrection is enabled
                string siteName = site.Name ?? "";
                if (!string.IsNullOrEmpty(siteName))
                {
                    siteName = SiteTools.FixName(siteName);
                }
                rowValues.Add(siteName);

                // Find coordinates for the specified coordinate system
                var coordinate = site.Coordinates?.FirstOrDefault(c => c.ReferenceSystem == coordSystemString);
                float x = 0, y = 0, z = 0;
                if (coordinate != null)
                {
                    x = coordinate.Position.x;
                    y = coordinate.Position.y;
                    z = coordinate.Position.z;
                }

                // Add x, y, z coordinates
                rowValues.Add(x.ToString("F3"));
                rowValues.Add(y.ToString("F3"));
                rowValues.Add(z.ToString("F3"));

                // Add size (fixed to 0 for now)
                rowValues.Add("0");

                // Add material (fixed to "platinum")
                rowValues.Add("platinum");

                // Add manufacturer (fixed to "DIXI")
                rowValues.Add("DIXI");

                // Parse site name to extract group and hemisphere
                string group = "";
                string hemisphere = "R"; // Default to R if no prime

                if (!string.IsNullOrEmpty(siteName))
                {
                    // Site name format: letters + optional prime + number
                    // Group = letters + optional prime
                    // Hemisphere = L if there's a prime, R if there isn't
                    var match = Regex.Match(siteName, @"^([A-Za-z]+)('?)(\d+)$");
                    if (match.Success)
                    {
                        string letters = match.Groups[1].Value;
                        string prime = match.Groups[2].Value;
                        
                        group = letters + prime;
                        hemisphere = !string.IsNullOrEmpty(prime) ? "L" : "R";
                    }
                    else
                    {
                        // Fallback: if regex doesn't match, try to extract letters from the beginning
                        var letterMatch = Regex.Match(siteName, @"^([A-Za-z']+)");
                        if (letterMatch.Success)
                        {
                            group = letterMatch.Groups[1].Value;
                            hemisphere = group.Contains("'") ? "L" : "R";
                        }
                        else
                        {
                            group = siteName; // Use full name as fallback
                        }
                    }
                }

                // Add group and hemisphere
                rowValues.Add(group);
                rowValues.Add(hemisphere);

                // Add tag values or "n/a" for missing tags
                foreach (var tag in allTags.OrderBy(t => t.Name))
                {
                    string value = "";

                    if (site.Tags != null)
                    {
                        var tagValue = site.Tags.FirstOrDefault(tv => tv.Tag == tag);
                        if (tagValue != null && tagValue.DisplayableValue != null)
                        {
                            value = tagValue.DisplayableValue.DeblankCompletely();
                        }
                    }

                    if (string.IsNullOrEmpty(value))
                    {
                        value = "n/a";
                    }

                    rowValues.Add(value);
                }

                tsvBuilder.AppendLine(string.Join("\t", rowValues));
            }

            return tsvBuilder.ToString();
        }
        
        private static string EEGFileToChannelsTSV(Core.DLL.EEG.File file)
        {
            var electrodes = file.Electrodes;
            if (electrodes == null || !electrodes.Any())
            {
                return "name\ttype\tunits\tlow_cutoff\thigh_cutoff\treference\tgroup\tsampling_frequency\n";
            }

            var siteNameComparer = new SiteNameComparer();
            electrodes.Sort((a, b) => siteNameComparer.Compare(a.Label, b.Label));

            // Fill data with fixed headers
            var tsvBuilder = new StringBuilder();
            tsvBuilder.AppendLine("name\ttype\tunits\tlow_cutoff\thigh_cutoff\treference\tgroup\tsampling_frequency");
            foreach (var electrode in electrodes)
            {
                var label = electrode.Label;
                string group = "";
                string type = "";
                if (!string.IsNullOrEmpty(label))
                {
                    // Site name format: letters + optional prime + number
                    // Group = letters + optional prime
                    // Hemisphere = L if there's a prime, R if there isn't
                    var match = Regex.Match(label, @"^([A-Za-z]+)('?)(\d+)$");
                    if (match.Success)
                    {
                        string letters = match.Groups[1].Value;
                        string prime = match.Groups[2].Value;

                        group = letters + prime;
                        type = "SEEG";
                    }
                    else
                    {
                        group = "n/a";
                        type = "MISC";
                    }
                }

                string referenceLabel = electrode.ReferenceLabel;
                if (string.IsNullOrEmpty(referenceLabel))
                {
                    referenceLabel = "intracranial";
                }

                var rowValues = new List<string>
                {
                    electrode.Label,
                    type, // type
                    electrode.Unit,   // units
                    electrode.PrefilteringLowPassLimit.ToString(),  // low_cutoff
                    electrode.PrefilteringHighPassLimit.ToString(),  // high_cutoff
                    electrode.ReferenceLabel,  // reference
                    group,  // group
                    file.SamplingFrequency.Value.ToString() // sampling_frequency
                };
                tsvBuilder.AppendLine(string.Join("\t", rowValues));
            }

            return tsvBuilder.ToString();
        }
        
        private static TaskFile EEGFileToTaskFile(Core.DLL.EEG.File file, Patient patient, DataInfo dataInfo)
        {
            int numberOfSEEGChannels = 0;
            int numberOfMiscChannels = 0;
            var electrodes = file.Electrodes;
            foreach (var electrode in electrodes)
            {
                var label = electrode.Label;
                var match = Regex.Match(label, @"^([A-Za-z]+)('?)(\d+)$");
                if (match.Success)
                {
                    numberOfSEEGChannels++;
                }
                else
                {
                    numberOfMiscChannels++;
                }
            }

            var institutionName = "n/a";
            var institutionAddress = "n/a";
            var placeTag = patient.Tags.FirstOrDefault(t => t.Tag.Name == "Place");
            if (placeTag != null)
            {
                if (placeTag.DisplayableValue == "GRE")
                {
                    institutionName = "CHU Grenoble Alpes";
                    institutionAddress = "Boulevard de la Chantourne, 38700 La Tronche, France";
                }
                else if (placeTag.DisplayableValue == "LYONNEURO")
                {
                    institutionName = "Hôpital Pierre Wertheimer";
                    institutionAddress = "59 Boulevard Pinel, 69677 Bron, France";
                }
            }

            return new TaskFile()
            {
                TaskName = dataInfo.Protocol.Name,
                SamplingFrequency = file.SamplingFrequency.Value,
                RecordingDuration = file.SamplingFrequency.ConvertNumberOfSamplesToSeconds(file.NumberOfSamples),
                SEEGChannelCount = numberOfSEEGChannels,
                MiscChannelCount = numberOfMiscChannels,
                InstitutionName = institutionName,
                InstitutionAddress = institutionAddress
            };
        }

        private static void CreateElectrodesFiles(BIDSPatient patient, string ieegFolder, BIDSExportConfiguration config, IEnumerable<BaseTag> selectedSiteTags)
        {
            var sites = patient.Patient.Sites;
            
            foreach (var coordRule in config.CoordinateSystemRules)
            {
                string electrodesTsvContent = SitesToElectrodesTSV(sites, coordRule.CoordinateSystemName, selectedSiteTags);
                
                string spaceEntity = string.IsNullOrEmpty(coordRule.BIDSSpace) ? "" : $"_space-{coordRule.BIDSSpace}";
                string electrodesTsvPath = Path.Combine(ieegFolder, $"{patient.ParticipantId}_ses-post{spaceEntity}_electrodes.tsv");
                File.WriteAllText(electrodesTsvPath, electrodesTsvContent);
                
                // Create coordsystem.json
                string coordSystemValue = string.IsNullOrEmpty(coordRule.BIDSSpace) ? "scanner" : coordRule.BIDSSpace;
                string coordSystemPath = Path.Combine(ieegFolder, $"{patient.ParticipantId}_ses-post{spaceEntity}_coordsystem.json");
                ClassLoaderSaver.SaveToJSon(new CoordSystemFile(coordSystemValue), coordSystemPath);
            }
        }
        
        private static void CreateFunctionalFiles(BIDSPatient patient, string ieegFolder)
        {
            foreach (var dataInfo in patient.DataInfos)
            {
                // Read Data
                Core.DLL.EEG.File.FileType type;
                string[] files;
                if (dataInfo.DataContainer is BrainVision brainVisionDataContainer)
                {
                    type = Core.DLL.EEG.File.FileType.BrainVision;
                    files = new string[] { brainVisionDataContainer.Header };
                }
                else if (dataInfo.DataContainer is EDF edfDataContainer)
                {
                    type = Core.DLL.EEG.File.FileType.EDF;
                    files = new string[] { edfDataContainer.File };
                }
                else if (dataInfo.DataContainer is Elan elanDataContainer)
                {
                    type = Core.DLL.EEG.File.FileType.ELAN;
                    files = new string[] { elanDataContainer.EEG, elanDataContainer.POS, elanDataContainer.Notes };
                }
                else if (dataInfo.DataContainer is Micromed micromedDataContainer)
                {
                    type = Core.DLL.EEG.File.FileType.Micromed;
                    files = new string[] { micromedDataContainer.Path };
                }
                else if (dataInfo.DataContainer is FIF fifDataContainer)
                {
                    type = Core.DLL.EEG.File.FileType.FIF;
                    files = new string[] { fifDataContainer.File };
                }
                else
                {
                    throw new Exception("Invalid data container type");
                }
                Core.DLL.EEG.File file = new Core.DLL.EEG.File(type, true, files);

                // Create files
                if (dataInfo.Name.ToLower() == "raw")
                {
                    file.Convert(Path.Combine(ieegFolder, $"{patient.ParticipantId}_ses-post_task-{dataInfo.Protocol.Name}_ieeg.edf"));
                    File.WriteAllText(Path.Combine(ieegFolder, $"{patient.ParticipantId}_ses-post_task-{dataInfo.Protocol.Name}_channels.tsv"), EEGFileToChannelsTSV(file));
                    ClassLoaderSaver.SaveToJSon(EEGFileToTaskFile(file, patient.Patient, dataInfo), Path.Combine(ieegFolder, $"{patient.ParticipantId}_ses-post_task-{dataInfo.Protocol.Name}_ieeg.json"));
                }
                else
                {
                    file.Convert(Path.Combine(ieegFolder, $"{patient.ParticipantId}_ses-post_task-{dataInfo.Protocol.Name}_acq-{dataInfo.Name}_ieeg.edf"));
                    File.WriteAllText(Path.Combine(ieegFolder, $"{patient.ParticipantId}_ses-post_task-{dataInfo.Protocol.Name}_acq-{dataInfo.Name}_channels.tsv"), EEGFileToChannelsTSV(file));
                    ClassLoaderSaver.SaveToJSon(EEGFileToTaskFile(file, patient.Patient, dataInfo), Path.Combine(ieegFolder, $"{patient.ParticipantId}_ses-post_task-{dataInfo.Protocol.Name}_acq-{dataInfo.Name}_ieeg.json"));
                }
            }
        }
    }
}