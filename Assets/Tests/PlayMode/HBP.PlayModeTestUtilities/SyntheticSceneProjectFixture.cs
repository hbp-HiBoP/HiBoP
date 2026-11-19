#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace HBP.Tests.PlayMode.Utilities
{
    /// <summary>Builds test-owned projects from versioned sources, without Python or a prepared local workspace.</summary>
    public static class SyntheticSceneProjectFixture
    {
        private static readonly UTF8Encoding Encoding = new(false);

        public static string CreateIEEG(string repository, string directory)
        {
            Directory.CreateDirectory(directory);
            string contacts = Path.Combine(repository, "Validation/Quest/fixtures/mni-contacts");
            JObject protocol = ReadJson(Path.Combine(repository, "Assets/Data/DefaultDatabase/Protocols/VISU.prov"));
            JObject bloc = (JObject)protocol["Blocs"].Single(b => (string)b["Name"] == "FACE").DeepClone();
            protocol["Name"] = "QUEST-020 synthetic";
            protocol["ID"] = "quest-020-protocol";
            protocol["Blocs"] = new JArray(bloc);
            bloc["ID"] = "quest-020-bloc";
            bloc["IllustrationPath"] = "";
            bloc["SubBlocs"][0]["ID"] = "quest-020-subbloc";
            bloc["SubBlocs"][0]["Events"][0]["ID"] = "quest-020-event";
            File.WriteAllText(Path.Combine(directory, "quest-020.prov"), protocol.ToString(), Encoding);

            JObject visualization = ReadJson(Path.Combine(contacts, "MNI Contacts.visualization"));
            visualization["Name"] = "MNI iEEG";
            visualization["ID"] = "quest-020-visualization";
            JObject column = (JObject)visualization["Columns"][0];
            column["$type"] = "HBP.Core.Data.IEEGColumn, HBP.Core.Runtime";
            column["Name"] = "Synthetic uV";
            column["ID"] = "quest-020-column";
            column["Dataset"] = "quest-020-dataset";
            column["Bloc"] = (string)bloc["ID"];
            column["DataName"] = "QUEST-020 synthetic";
            column.Remove("AnatomicConfiguration");
            column["DynamicConfiguration"] = new JObject { ["ID"] = "quest-020-dynamic", ["Site Maximum Influence"] = 15, ["Span Min"] = -10, ["Middle"] = 0, ["Span Max"] = 10 };
            column["BaseConfiguration"]["ConfigurationBySite"] = new JObject { ["quest-013-patient-right_A2"] = new JObject { ["ID"] = "quest-020-blacklist", ["IsBlacklisted"] = true } };
            var data = new JArray();
            var dataset = new JObject { ["Name"] = "QUEST-020 synthetic", ["ID"] = "quest-020-dataset", ["Protocol"] = (string)protocol["ID"], ["Data"] = data };
            string[] sides = { "left", "right" };
            for (int patient = 0; patient < sides.Length; patient++)
            {
                string side = sides[patient];
                string stem = "synthetic-" + side;
                File.WriteAllText(Path.Combine(directory, stem + ".vhdr"), $"Brain Vision Data Exchange Header File Version 1.0\n[Common Infos]\nDataFile={stem}.eeg\nMarkerFile={stem}.vmrk\nDataFormat=BINARY\nDataOrientation=MULTIPLEXED\nNumberOfChannels=3\nSamplingInterval=10000\n\n[Binary Infos]\nBinaryFormat=IEEE_FLOAT_32\n\n[Channel Infos]\nCh1=A1,,1,uV\nCh2=A2,,1,uV\nCh3=B1,,1,uV\n", Encoding);
                File.WriteAllText(Path.Combine(directory, stem + ".vmrk"), $"Brain Vision Data Exchange Marker File, Version 1.0\n[Common Infos]\nDataFile={stem}.eeg\n\n[Marker Infos]\nMk1=New Segment,,1,1,0,00000000000000000000\nMk2=Stimulus,S 20,101,1,0\n", Encoding);
                using (var writer = new BinaryWriter(File.Create(Path.Combine(directory, stem + ".eeg"))))
                    for (int index = 0; index < 300; index++)
                    {
                        float amplitude = (float)(index == 60 || index == 180 ? 10.0 : 1.0 + (index - 100) / 100.0);
                        writer.Write(-amplitude);
                        writer.Write(0f);
                        writer.Write(amplitude);
                    }

                data.Add(new JObject
                {
                    ["$type"] = "HBP.Core.Data.IEEGDataInfo, HBP.Core.Runtime", ["Patient"] = (string)visualization["Patients"][patient],
                    ["Name"] = "QUEST-020 synthetic", ["ID"] = "quest-020-data-" + side, ["m_ProtocolID"] = (string)protocol["ID"],
                    ["Normalization"] = 0, ["CorrespondingDatabaseID"] = "", ["m_Errors"] = new JArray(), ["m_Warnings"] = new JArray(),
                    ["DataContainer"] = new JObject
                    {
                        ["$type"] = "HBP.Core.Data.Container.BrainVision, HBP.Core.Runtime", ["ID"] = "quest-020-container-" + side,
                        ["Header"] = Path.Combine(directory, stem + ".vhdr"), ["m_Errors"] = new JArray(), ["m_Warnings"] = new JArray()
                    }
                });
            }

            var entries = new Dictionary<string, JObject>
            {
                // Keep the historical serialization shape as compatibility coverage.
                ["quest-mni-ieeg.settings"] = new() { ["Version"] = "6.1.0", ["ID"] = "quest-020-project" },
                ["Datasets/QUEST-020.dataset"] = dataset,
                ["Visualizations/MNI iEEG.visualization"] = visualization
            };
            foreach (string path in Directory.GetFiles(contacts, "*.patient").OrderBy(p => p, StringComparer.Ordinal))
                entries["Patients/" + Path.GetFileName(path)] = ReadJson(path);
            string output = Path.Combine(directory, "quest-mni-ieeg.hibop");
            WriteArchive(output, entries);
            return output;
        }

        public static void CreateMultimodal(string repository, string directory)
        {
            var entries = ReadArchive(CreateIEEG(repository, directory));
            var volumes = new Dictionary<string, string>();
            foreach (string name in new[] { "fmri_4d.nii.gz", "fmri_3d.nii", "mask_binary.nii", "megv_mask.nii" })
            {
                using var input = File.OpenRead(Path.Combine(repository, "Assets/Tests/Fixtures/Native/Nifti", name));
                using var buffer = new MemoryStream();
                if (name.EndsWith(".gz", StringComparison.Ordinal))
                {
                    using var gzip = new GZipStream(input, CompressionMode.Decompress);
                    gzip.CopyTo(buffer);
                }
                else input.CopyTo(buffer);

                using (var writer = new BinaryWriter(buffer, Encoding, true))
                {
                    buffer.Position = 80;
                    writer.Write(50f);
                    writer.Write(50f);
                    writer.Write(50f);
                    buffer.Position = 252;
                    writer.Write((short)0);
                    writer.Write((short)2);
                    buffer.Position = 280;
                    foreach (float value in new[] { 50f, 0, 0, -100, 0, 50, 0, -125, 0, 0, 50, -75 }) writer.Write(value);
                }

                string target = Path.Combine(directory, name.Replace(".nii.gz", "").Replace(".nii", "") + "-mni.nii");
                File.WriteAllBytes(target, buffer.ToArray());
                volumes[name] = target;
            }

            var templates = ReadArchive(Path.Combine(repository, "Assets/Tests/Fixtures/Projects/Generated/native-fixture-reference.hibop"));
            var visualization = entries["Visualizations/MNI iEEG.visualization"];
            entries.Remove("Visualizations/MNI iEEG.visualization");
            visualization["Name"] = "SCENE-008 six modalities";
            visualization["ID"] = "scene-008-visualization";
            var dataset = entries["Datasets/QUEST-020.dataset"];
            var signal = (JObject)visualization["Columns"][0].DeepClone();
            var columns = new JArray();
            foreach (JObject template in templates["Visualizations/visualization-alpha.visualization"]["Columns"])
            {
                var column = (JObject)template.DeepClone();
                string kind = ((string)column["$type"]).Split(',')[0].Split('.').Last().Replace("Column", "");
                string id = "scene-008-" + kind;
                column["ID"] = id;
                column["Name"] = kind;
                column["BaseConfiguration"] = signal["BaseConfiguration"].DeepClone();
                column["BaseConfiguration"]["ID"] = id + "-base";
                foreach (var site in ((JObject)column["BaseConfiguration"]["ConfigurationBySite"]).Properties())
                    site.Value["ID"] = id + "-site-" + site.Name;
                if (column.ContainsKey("Dataset")) column["Dataset"] = (string)dataset["ID"];
                if (column.ContainsKey("Bloc")) column["Bloc"] = (string)signal["Bloc"];
                if (kind == "IEEG")
                {
                    column["DataName"] = (string)signal["DataName"];
                    column["DynamicConfiguration"] = signal["DynamicConfiguration"].DeepClone();
                    column["DynamicConfiguration"]["ID"] = id + "-dynamic";
                }
                else if (kind == "CCEP") column["DataName"] = "SCENE-008 responses";

                columns.Add(column);
            }

            visualization["Columns"] = columns;
            var data = (JArray)dataset["Data"];
            foreach (JObject item in data.ToArray())
            {
                var response = (JObject)item.DeepClone();
                response["$type"] = "HBP.Core.Data.CCEPDataInfo, HBP.Core.Runtime";
                response["Name"] = "SCENE-008 responses";
                response["StimulatedChannel"] = "A1";
                response["ID"] = (string)item["ID"] + "-ccep";
                response.Remove("Normalization");
                response["DataContainer"]["ID"] = (string)response["DataContainer"]["ID"] + "-ccep";
                data.Add(response);
            }

            foreach (JObject item in templates["Datasets/native-fixture-dataset.dataset"]["Data"])
            {
                if (!new[] { "FMRIDataInfo", "MEGcDataInfo", "MEGvDataInfo", "StaticDataInfo" }.Any(kind => ((string)item["$type"]).Contains(kind))) continue;
                string[] patients = item.ContainsKey("Patient") ? visualization["Patients"].Values<string>().ToArray() : new string[] { null };
                for (int index = 0; index < patients.Length; index++)
                {
                    var resource = (JObject)Resolve(item, repository, volumes);
                    resource["m_ProtocolID"] = (string)dataset["Protocol"];
                    if (patients[index] != null) resource["Patient"] = patients[index];
                    resource["ID"] = (string)resource["ID"] + "-" + index;
                    foreach (string key in new[] { "DataContainer", "MaskDataContainer" })
                        if (resource.ContainsKey(key))
                            resource[key]["ID"] = (string)resource[key]["ID"] + "-" + index;
                    data.Add(resource);
                }
            }

            JObject patient = entries.Single(e => e.Key.EndsWith(".patient", StringComparison.Ordinal) && (string)e.Value["ID"] == (string)visualization["Patients"][0]).Value;
            JObject patientTemplate = templates["Patients/native-patient-001.patient"];
            foreach (string key in new[] { "Meshes", "MRIs" })
                if (patientTemplate.ContainsKey(key))
                    patient[key] = Resolve(patientTemplate[key], repository, volumes);
            entries.Remove("quest-mni-ieeg.settings");
            entries["scene-008.settings"] = new JObject { ["Version"] = "6.1.0", ["ID"] = "scene-008-project" };
            entries["Visualizations/SCENE-008.visualization"] = visualization;
            WriteArchive(Path.Combine(directory, "scene-008.hibop"), entries);
            File.Copy(Path.Combine(directory, "quest-020.prov"), Path.Combine(directory, "scene-008.prov"));
            visualization["Patients"] = new JArray((string)visualization["Patients"][0]);
            visualization["Configuration"]["Mesh"] = (string)patient["Meshes"][0]["Name"];
            visualization["Configuration"]["MRI"] = (string)patient["MRIs"][0]["Name"];
            WriteArchive(Path.Combine(directory, "scene-008-patient.hibop"), entries);
        }

        private static JToken Resolve(JToken value, string repository, Dictionary<string, string> volumes)
        {
            if (value is JObject obj) return new JObject(obj.Properties().Select(p => new JProperty(p.Name, Resolve(p.Value, repository, volumes))));
            if (value is JArray array) return new JArray(array.Select(item => Resolve(item, repository, volumes)));
            if (value.Type != JTokenType.String || !((string)value).StartsWith("[HIBOP_NATIVE_FIXTURES]", StringComparison.Ordinal)) return value.DeepClone();
            string relative = ((string)value).Split(']')[1].TrimStart('/', '\\').Replace('\\', '/');
            return volumes.TryGetValue(Path.GetFileName(relative), out string volume) ? volume : Path.Combine(repository, "Assets/Tests/Fixtures/Native", relative);
        }

        private static JObject ReadJson(string path) => JObject.Parse(File.ReadAllText(path));

        private static Dictionary<string, JObject> ReadArchive(string path)
        {
            using var archive = ZipFile.OpenRead(path);
            return archive.Entries.Where(e => !e.FullName.EndsWith("/", StringComparison.Ordinal)).ToDictionary(e => e.FullName, e =>
            {
                using var reader = new StreamReader(e.Open());
                return JObject.Parse(reader.ReadToEnd());
            });
        }

        private static void WriteArchive(string path, Dictionary<string, JObject> entries)
        {
            using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
            foreach (string folder in new[] { "Patients/", "Groups/", "Datasets/", "Visualizations/" })
                archive.CreateEntry(folder).LastWriteTime = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
            foreach (var item in entries)
            {
                var entry = archive.CreateEntry(item.Key, CompressionLevel.NoCompression);
                entry.LastWriteTime = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
                using var writer = new StreamWriter(entry.Open(), Encoding);
                writer.Write(item.Value.ToString());
            }
        }
    }
}
#endif
