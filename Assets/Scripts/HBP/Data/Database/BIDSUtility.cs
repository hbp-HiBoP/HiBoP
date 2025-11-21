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
using static AssetUsageDetectorNamespace.AssetUsageDetector;

namespace HBP.Data.BIDS
{
    public static class BIDSUtility
    {
        private enum CoordSystem
        {
            Patient,
            MNI
        }

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

        public static async UniTask<string> CreateRootDirectoryAndFilesAsync(string datasetName, IEnumerable<BIDSPatient> patients, string baseFolder)
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
            string participantsTsv = PatientsToParticipantsTSV(patients);
            string participantsTsvPath = Path.Combine(datasetPath, "participants.tsv");
            await File.WriteAllTextAsync(participantsTsvPath, participantsTsv);

            return datasetPath;
        }

        public static async UniTask ExportPatientAsync(BIDSPatient patient, string datasetFolder, BIDSParameters parameters)
        {
            var patientDirectory = Directory.CreateDirectory(Path.Combine(datasetFolder, patient.ParticipantId));
            var preDirectory = Directory.CreateDirectory(Path.Combine(patientDirectory.FullName, "ses-pre"));
            var preAnatDirectory = Directory.CreateDirectory(Path.Combine(preDirectory.FullName, "anat"));
            var postDirectory = Directory.CreateDirectory(Path.Combine(patientDirectory.FullName, "ses-post"));
            var postAnatDirectory = Directory.CreateDirectory(Path.Combine(postDirectory.FullName, "anat"));
            var postIeegDirectory = Directory.CreateDirectory(Path.Combine(postDirectory.FullName, "ieeg"));

            // Anatomy
            CopyPreAnatomy(patient, preAnatDirectory.FullName, parameters);
            CopyPostAnatomy(patient, postAnatDirectory.FullName, parameters);
            CopyCTAnatomy(patient, postAnatDirectory.FullName, parameters);

            // IEEG
            CreateElectrodesFiles(patient, postIeegDirectory.FullName, parameters);
            CreateFunctionalFiles(patient, postIeegDirectory.FullName);
        }

        private static string PatientsToParticipantsTSV(IEnumerable<BIDSPatient> bidsPatients)
        {
            if (bidsPatients == null || !bidsPatients.Any())
            {
                return "participant_id\n";
            }

            var bidsPatientsList = bidsPatients.ToList();

            // Step 1: Collect all distinct tags from all patients
            var allTags = new HashSet<BaseTag>();
            foreach (var bidsPatient in bidsPatientsList)
            {
                if (bidsPatient.Patient.Tags != null)
                {
                    foreach (var tagValue in bidsPatient.Patient.Tags)
                    {
                        if (tagValue.Tag != null)
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
        private static string SitesToElectrodesTSV(IEnumerable<Site> sites, CoordSystem coordSystem, string coordSystemString)
        {
            if (sites == null || !sites.Any())
            {
                return "name\tx\ty\tz\tsize\tmaterial\tmanufacturer\tgroup\themisphere\n";
            }

            var sitesList = sites.ToList();

            // Sort sites using SiteNameComparer
            var siteNameComparer = new SiteNameComparer();
            sitesList.Sort((a, b) => siteNameComparer.Compare(a.Name, b.Name));

            // Step 1: Collect all distinct tags from all sites
            var allTags = new HashSet<BaseTag>();
            foreach (var site in sitesList)
            {
                if (site.Tags != null)
                {
                    foreach (var tagValue in site.Tags)
                    {
                        if (tagValue.Tag != null)
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

        private static void CopyPreAnatomy(BIDSPatient patient, string destinationFolder, BIDSParameters parameters)
        {
            if (parameters.IncludePreMRI)
            {
                var preMRI = patient.Patient.MRIs.FirstOrDefault(m => m.Name == parameters.PreMRIName);
                if (preMRI != null) File.Copy(preMRI.File, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-pre_T1w.nii"), true);
            }

            if (parameters.IncludePreGreyMatterMesh)
            {
                var preGreyMatterMesh = patient.Patient.Meshes.FirstOrDefault(m => m.Name == parameters.PreGreyMatterMeshName);
                if (preGreyMatterMesh != null)
                {
                    if (preGreyMatterMesh is SingleMesh singleMesh)
                    {
                        File.Copy(singleMesh.Path, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-pre_pial.surf.gii"), true);
                        if (singleMesh.HasMarsAtlas)
                        {
                            File.Copy(singleMesh.MarsAtlasPath, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-pre_desc-marsatlas_dseg.label.gii"), true);
                        }
                    }
                    else if (preGreyMatterMesh is LeftRightMesh leftRightMesh)
                    {
                        File.Copy(leftRightMesh.LeftHemisphere, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-pre_hemi-L_pial.surf.gii"), true);
                        File.Copy(leftRightMesh.RightHemisphere, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-pre_hemi-R_pial.surf.gii"), true);
                        if (leftRightMesh.HasMarsAtlas)
                        {
                            File.Copy(leftRightMesh.LeftMarsAtlasHemisphere, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-pre_desc-marsatlas_hemi-L_dseg.label.gii"), true);
                            File.Copy(leftRightMesh.RightMarsAtlasHemisphere, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-pre_desc-marsatlas_hemi-R_dseg.label.gii"), true);
                        }
                    }
                    if (preGreyMatterMesh.HasTransformation)
                    {
                        File.Copy(preGreyMatterMesh.Transformation, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-pre.trm"), true);
                    }
                }
            }

            if (parameters.IncludePreWhiteMatterMesh)
            {
                var preWhiteMatterMesh = patient.Patient.Meshes.FirstOrDefault(m => m.Name == parameters.PreWhiteMatterMeshName);
                if (preWhiteMatterMesh != null)
                {
                    if (preWhiteMatterMesh is SingleMesh singleMesh)
                    {
                        File.Copy(singleMesh.Path, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-pre_white.surf.gii"), true);
                        if (singleMesh.HasMarsAtlas)
                        {
                            File.Copy(singleMesh.MarsAtlasPath, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-pre_desc-marsatlas_dseg.label.gii"), true);
                        }
                    }
                    else if (preWhiteMatterMesh is LeftRightMesh leftRightMesh)
                    {
                        File.Copy(leftRightMesh.LeftHemisphere, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-pre_hemi-L_white.surf.gii"), true);
                        File.Copy(leftRightMesh.RightHemisphere, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-pre_hemi-R_white.surf.gii"), true);
                        if (leftRightMesh.HasMarsAtlas)
                        {
                            File.Copy(leftRightMesh.LeftMarsAtlasHemisphere, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-pre_desc-marsatlas_hemi-L_dseg.label.gii"), true);
                            File.Copy(leftRightMesh.RightMarsAtlasHemisphere, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-pre_desc-marsatlas_hemi-R_dseg.label.gii"), true);
                        }
                    }
                    if (preWhiteMatterMesh.HasTransformation)
                    {
                        File.Copy(preWhiteMatterMesh.Transformation, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-pre.trm"), true);
                    }
                }
            }
        }
        private static void CopyPostAnatomy(BIDSPatient patient, string destinationFolder, BIDSParameters parameters)
        {
            if (parameters.IncludePostMRI)
            {
                var postMRI = patient.Patient.MRIs.FirstOrDefault(m => m.Name == parameters.PostMRIName);
                if (postMRI != null) File.Copy(postMRI.File, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post_T1w.nii"), true);
            }

            if (parameters.IncludePostGreyMatterMesh)
            {
                var postGreyMatterMesh = patient.Patient.Meshes.FirstOrDefault(m => m.Name == parameters.PostGreyMatterMeshName);
                if (postGreyMatterMesh != null)
                {
                    if (postGreyMatterMesh is SingleMesh singleMesh)
                    {
                        File.Copy(singleMesh.Path, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post_pial.surf.gii"), true);
                        if (singleMesh.HasMarsAtlas)
                        {
                            File.Copy(singleMesh.MarsAtlasPath, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post_desc-marsatlas_dseg.label.gii"), true);
                        }
                    }
                    else if (postGreyMatterMesh is LeftRightMesh leftRightMesh)
                    {
                        File.Copy(leftRightMesh.LeftHemisphere, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post_hemi-L_pial.surf.gii"), true);
                        File.Copy(leftRightMesh.RightHemisphere, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post_hemi-R_pial.surf.gii"), true);
                        if (leftRightMesh.HasMarsAtlas)
                        {
                            File.Copy(leftRightMesh.LeftMarsAtlasHemisphere, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post_desc-marsatlas_hemi-L_dseg.label.gii"), true);
                            File.Copy(leftRightMesh.RightMarsAtlasHemisphere, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post_desc-marsatlas_hemi-R_dseg.label.gii"), true);
                        }
                    }
                    if (postGreyMatterMesh.HasTransformation)
                    {
                        File.Copy(postGreyMatterMesh.Transformation, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post.trm"), true);
                    }
                }
            }

            if (parameters.IncludePostWhiteMatterMesh)
            {
                var postWhiteMatterMesh = patient.Patient.Meshes.FirstOrDefault(m => m.Name == parameters.PostWhiteMatterMeshName);
                if (postWhiteMatterMesh != null)
                {
                    if (postWhiteMatterMesh is SingleMesh singleMesh)
                    {
                        File.Copy(singleMesh.Path, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post_white.surf.gii"), true);
                        if (singleMesh.HasMarsAtlas)
                        {
                            File.Copy(singleMesh.MarsAtlasPath, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post_desc-marsatlas_dseg.label.gii"), true);
                        }
                    }
                    else if (postWhiteMatterMesh is LeftRightMesh leftRightMesh)
                    {
                        File.Copy(leftRightMesh.LeftHemisphere, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post_hemi-L_white.surf.gii"), true);
                        File.Copy(leftRightMesh.RightHemisphere, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post_hemi-R_white.surf.gii"), true);
                        if (leftRightMesh.HasMarsAtlas)
                        {
                            File.Copy(leftRightMesh.LeftMarsAtlasHemisphere, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post_desc-marsatlas_hemi-L_dseg.label.gii"), true);
                            File.Copy(leftRightMesh.RightMarsAtlasHemisphere, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post_desc-marsatlas_hemi-R_dseg.label.gii"), true);
                        }
                    }
                    if (postWhiteMatterMesh.HasTransformation)
                    {
                        File.Copy(postWhiteMatterMesh.Transformation, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post.trm"), true);
                    }
                }
            }
        }
        private static void CopyCTAnatomy(BIDSPatient patient, string destinationFolder, BIDSParameters parameters)
        {
            if (parameters.IncludeCTMRI)
            {
                var ctMRI = patient.Patient.MRIs.FirstOrDefault(m => m.Name == parameters.CTMRIName);
                if (ctMRI != null) File.Copy(ctMRI.File, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post_CT.nii"), true);
            }

            if (parameters.IncludeCTGreyMatterMesh)
            {
                var ctGreyMatterMesh = patient.Patient.Meshes.FirstOrDefault(m => m.Name == parameters.CTGreyMatterMeshName);
                if (ctGreyMatterMesh != null)
                {
                    if (ctGreyMatterMesh is SingleMesh singleMesh)
                    {
                        File.Copy(singleMesh.Path, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post_pial.surf.gii"), true);
                        if (singleMesh.HasMarsAtlas)
                        {
                            File.Copy(singleMesh.MarsAtlasPath, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post_desc-marsatlas_dseg.label.gii"), true);
                        }
                    }
                    else if (ctGreyMatterMesh is LeftRightMesh leftRightMesh)
                    {
                        File.Copy(leftRightMesh.LeftHemisphere, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post_hemi-L_pial.surf.gii"), true);
                        File.Copy(leftRightMesh.RightHemisphere, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post_hemi-R_pial.surf.gii"), true);
                        if (leftRightMesh.HasMarsAtlas)
                        {
                            File.Copy(leftRightMesh.LeftMarsAtlasHemisphere, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post_desc-marsatlas_hemi-L_dseg.label.gii"), true);
                            File.Copy(leftRightMesh.RightMarsAtlasHemisphere, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post_desc-marsatlas_hemi-R_dseg.label.gii"), true);
                        }
                    }
                    if (ctGreyMatterMesh.HasTransformation)
                    {
                        File.Copy(ctGreyMatterMesh.Transformation, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post.trm"), true);
                    }
                }
            }

            if (parameters.IncludeCTWhiteMatterMesh)
            {
                var ctWhiteMatterMesh = patient.Patient.Meshes.FirstOrDefault(m => m.Name == parameters.CTWhiteMatterMeshName);
                if (ctWhiteMatterMesh != null)
                {
                    if (ctWhiteMatterMesh is SingleMesh singleMesh)
                    {
                        File.Copy(singleMesh.Path, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post_white.surf.gii"), true);
                        if (singleMesh.HasMarsAtlas)
                        {
                            File.Copy(singleMesh.MarsAtlasPath, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post_desc-marsatlas_dseg.label.gii"), true);
                        }
                    }
                    else if (ctWhiteMatterMesh is LeftRightMesh leftRightMesh)
                    {
                        File.Copy(leftRightMesh.LeftHemisphere, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post_hemi-L_white.surf.gii"), true);
                        File.Copy(leftRightMesh.RightHemisphere, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post_hemi-R_white.surf.gii"), true);
                        if (leftRightMesh.HasMarsAtlas)
                        {
                            File.Copy(leftRightMesh.LeftMarsAtlasHemisphere, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post_desc-marsatlas_hemi-L_dseg.label.gii"), true);
                            File.Copy(leftRightMesh.RightMarsAtlasHemisphere, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post_desc-marsatlas_hemi-R_dseg.label.gii"), true);
                        }
                    }
                    if (ctWhiteMatterMesh.HasTransformation)
                    {
                        File.Copy(ctWhiteMatterMesh.Transformation, Path.Combine(destinationFolder, $"{patient.ParticipantId}_ses-post.trm"), true);
                    }
                }
            }
        }

        private static void CreateElectrodesFiles(BIDSPatient patient, string ieegFolder, BIDSParameters parameters)
        {
            var sites = patient.Patient.Sites;
            if (parameters.IncludePatientCoordSystem)
            {
                string electrodesTsvContent = SitesToElectrodesTSV(sites, CoordSystem.Patient, parameters.PatientCoordSystem);
                File.WriteAllText(Path.Combine(ieegFolder, $"{patient.ParticipantId}_ses-post_electrodes.tsv"), electrodesTsvContent);
                ClassLoaderSaver.SaveToJSon(new CoordSystemFile("scanner"), Path.Combine(ieegFolder, $"{patient.ParticipantId}_ses-post_coordsystem.json"));
            }
            if (parameters.IncludeMNICoordSystem)
            {
                var space = "MNI152Lin";
                string electrodesTsvContent = SitesToElectrodesTSV(sites, CoordSystem.MNI, parameters.MNICoordSystem);
                File.WriteAllText(Path.Combine(ieegFolder, $"{patient.ParticipantId}_ses-post_space-{space}_electrodes.tsv"), electrodesTsvContent);
                ClassLoaderSaver.SaveToJSon(new CoordSystemFile("MNI152Lin"), Path.Combine(ieegFolder, $"{patient.ParticipantId}_ses-post_space-{space}_coordsystem.json"));
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

    [JsonObject(MemberSerialization.OptOut), Preserve]
    public class BIDSParameters
    {
        public bool IncludePreMRI = true;
        public string PreMRIName = "Preimplantation";

        public bool IncludePostMRI = true;
        public string PostMRIName = "Postimplantation";

        public bool IncludeCTMRI = true;
        public string CTMRIName = "CT";

        public bool IncludePreGreyMatterMesh = true;
        public string PreGreyMatterMeshName = "Grey matter";

        public bool IncludePreWhiteMatterMesh = true;
        public string PreWhiteMatterMeshName = "White matter";

        public bool IncludePostGreyMatterMesh = false;
        public string PostGreyMatterMeshName = "Grey matter post";

        public bool IncludePostWhiteMatterMesh = false;
        public string PostWhiteMatterMeshName = "White matter post";

        public bool IncludeCTGreyMatterMesh = false;
        public string CTGreyMatterMeshName = "Grey matter CT";

        public bool IncludeCTWhiteMatterMesh = false;
        public string CTWhiteMatterMeshName = "White matter CT";

        public bool IncludePatientCoordSystem = true;
        public string PatientCoordSystem = "Patient";

        public bool IncludeMNICoordSystem = true;
        public string MNICoordSystem = "MNI";
    }
}