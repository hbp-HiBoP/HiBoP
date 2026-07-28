using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using HBP.Core.Exceptions;
using HBP.Core.Tools;
using Ionic.Zip;

namespace HBP.Core.Data
{
    public sealed class ProjectManifest
    {
        public const int LegacySchemaVersion = 0;

        private readonly IReadOnlyDictionary<string, long> m_Entries;
        private readonly IReadOnlyList<string> m_SettingsEntries;
        private readonly IReadOnlyList<string> m_PatientEntries;
        private readonly IReadOnlyList<string> m_GroupEntries;
        private readonly IReadOnlyList<string> m_DatasetEntries;
        private readonly IReadOnlyList<string> m_VisualizationEntries;

        public int SchemaVersion => LegacySchemaVersion;
        public string ProductVersion => Preferences?.Version ?? string.Empty;
        public string Name { get; }
        public string Path { get; }
        public IReadOnlyDictionary<string, long> Entries => m_Entries;
        public ProjectPreferences Preferences { get; }
        public Exception PreferencesLoadException { get; }
        public int Patients => m_PatientEntries.Count;
        public int Groups => m_GroupEntries.Count;
        public int Datasets => m_DatasetEntries.Count;
        public int Visualizations => m_VisualizationEntries.Count;

        internal IReadOnlyList<string> SettingsEntries => m_SettingsEntries;
        internal IReadOnlyList<string> PatientEntries => m_PatientEntries;
        internal IReadOnlyList<string> GroupEntries => m_GroupEntries;
        internal IReadOnlyList<string> DatasetEntries => m_DatasetEntries;
        internal IReadOnlyList<string> VisualizationEntries => m_VisualizationEntries;

        private long ArchiveLength { get; }
        private DateTime ArchiveLastWriteTimeUtc { get; }

        private ProjectManifest(
            string path,
            Dictionary<string, long> entries,
            List<string> settingsEntries,
            List<string> patientEntries,
            List<string> groupEntries,
            List<string> datasetEntries,
            List<string> visualizationEntries,
            ProjectPreferences preferences,
            Exception preferencesLoadException,
            long archiveLength,
            DateTime archiveLastWriteTimeUtc)
        {
            Path = path;
            Name = System.IO.Path.GetFileNameWithoutExtension(path);
            m_Entries = new ReadOnlyDictionary<string, long>(entries);
            m_SettingsEntries = new ReadOnlyCollection<string>(settingsEntries);
            m_PatientEntries = new ReadOnlyCollection<string>(patientEntries);
            m_GroupEntries = new ReadOnlyCollection<string>(groupEntries);
            m_DatasetEntries = new ReadOnlyCollection<string>(datasetEntries);
            m_VisualizationEntries = new ReadOnlyCollection<string>(visualizationEntries);
            Preferences = preferences;
            PreferencesLoadException = preferencesLoadException;
            ArchiveLength = archiveLength;
            ArchiveLastWriteTimeUtc = archiveLastWriteTimeUtc;
        }

        internal static ProjectManifest Read(string path, bool readPreferences)
        {
            FileInfo archiveFile = new(path);
            if (!archiveFile.Exists || archiveFile.Extension != Project.EXTENSION)
            {
                throw new DirectoryNotProjectException(path);
            }

            Dictionary<string, long> entries = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> normalizedEntryNames = new(StringComparer.OrdinalIgnoreCase);
            List<string> settingsEntries = new();
            List<string> patientEntries = new();
            List<string> groupEntries = new();
            List<string> datasetEntries = new();
            List<string> visualizationEntries = new();
            bool hasPatientsDirectory = false;
            bool hasGroupsDirectory = false;
            bool hasDatasetsDirectory = false;
            bool hasVisualizationsDirectory = false;

            using ZipFile zip = ZipFile.Read(path);
            foreach (ZipEntry entry in zip)
            {
                string entryName = NormalizeAndValidateEntryName(entry.FileName);
                if (IsProtocolsEntry(entryName))
                {
                    continue;
                }
                if (!normalizedEntryNames.Add(entryName))
                {
                    throw new InvalidDataException($"Duplicate project archive entry '{entryName}'.");
                }
                entries.Add(entry.FileName, entry.UncompressedSize);

                if (entryName == "Patients/")
                {
                    hasPatientsDirectory = true;
                }
                else if (entryName == "Groups/")
                {
                    hasGroupsDirectory = true;
                }
                else if (entryName == "Datasets/")
                {
                    hasDatasetsDirectory = true;
                }
                else if (entryName == "Visualizations/")
                {
                    hasVisualizationsDirectory = true;
                }
                else if (IsTopLevelEntry(entryName, ProjectPreferences.EXTENSION))
                {
                    settingsEntries.Add(entry.FileName);
                }
                else if (IsDirectChildEntry(entryName, "Patients/", Patient.EXTENSION))
                {
                    patientEntries.Add(entry.FileName);
                }
                else if (IsDirectChildEntry(entryName, "Groups/", Group.EXTENSION))
                {
                    groupEntries.Add(entry.FileName);
                }
                else if (IsDirectChildEntry(entryName, "Datasets/", Dataset.EXTENSION))
                {
                    datasetEntries.Add(entry.FileName);
                }
                else if (IsDirectChildEntry(entryName, "Visualizations/", Visualization.EXTENSION))
                {
                    visualizationEntries.Add(entry.FileName);
                }
            }

            if (!hasPatientsDirectory || !hasGroupsDirectory || !hasDatasetsDirectory
                || !hasVisualizationsDirectory || settingsEntries.Count == 0)
            {
                throw new DirectoryNotProjectException(path);
            }

            ProjectPreferences preferences = new();
            Exception preferencesLoadException = null;
            if (readPreferences)
            {
                string settingsEntryName = settingsEntries.Last();
                try
                {
                    ZipEntry settingsEntry = zip[settingsEntryName];
                    using Stream stream = settingsEntry.OpenReader();
                    preferences = ClassLoaderSaver.LoadFromJson<ProjectPreferences>(stream);
                }
                catch (Exception exception)
                {
                    preferencesLoadException = exception;
                    preferences = new ProjectPreferences
                    {
                        CanLoadProject = false
                    };
                }
            }

            archiveFile.Refresh();
            return new ProjectManifest(
                path,
                entries,
                settingsEntries,
                patientEntries,
                groupEntries,
                datasetEntries,
                visualizationEntries,
                preferences,
                preferencesLoadException,
                archiveFile.Length,
                archiveFile.LastWriteTimeUtc);
        }

        internal bool IsCurrent()
        {
            FileInfo archiveFile = new(Path);
            return archiveFile.Exists
                && archiveFile.Length == ArchiveLength
                && archiveFile.LastWriteTimeUtc == ArchiveLastWriteTimeUtc;
        }

        private static string NormalizeAndValidateEntryName(string entryName)
        {
            if (string.IsNullOrEmpty(entryName))
            {
                throw new InvalidDataException("The project archive contains an empty entry name.");
            }

            string normalized = entryName.Replace('\\', '/');
            if (normalized.StartsWith("/", StringComparison.Ordinal)
                || (normalized.Length >= 2 && char.IsLetter(normalized[0]) && normalized[1] == ':'))
            {
                throw new InvalidDataException($"Absolute project archive entry '{entryName}' is not allowed.");
            }

            string[] segments = normalized.Split('/');
            if (segments.Any(segment => segment == "." || segment == ".."))
            {
                throw new InvalidDataException($"Unsafe project archive entry '{entryName}' is not allowed.");
            }
            return normalized;
        }

        private static bool IsProtocolsEntry(string entryName)
        {
            return entryName.Equals("Protocols/", StringComparison.OrdinalIgnoreCase)
                || entryName.StartsWith("Protocols/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTopLevelEntry(string entryName, string extension)
        {
            return entryName.IndexOf('/') < 0
                && entryName.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDirectChildEntry(string entryName, string directory, string extension)
        {
            if (!entryName.StartsWith(directory, StringComparison.Ordinal)
                || !entryName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string relativeName = entryName[directory.Length..];
            return relativeName.Length > extension.Length && relativeName.IndexOf('/') < 0;
        }
    }
}
