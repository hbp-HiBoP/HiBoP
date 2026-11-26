using System;
using System.Collections.Generic;
using System.IO;

namespace HBP.Core.Database
{
    public sealed class DatabaseAccessIssue
    {
        internal DatabaseAccessIssue(string sourceName, string folderPath, string patientID)
        {
            SourceName = sourceName;
            FolderPath = folderPath;
            PatientID = patientID;
        }

        public string SourceName { get; }
        public string FolderPath { get; }
        public string PatientID { get; }
    }

    internal sealed class DatabaseSourceAccess
    {
        private readonly string m_SourceName;
        private readonly List<DatabaseAccessIssue> m_Issues = new();
        private readonly HashSet<string> m_SkippedPatients = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> m_UnlistedFolders = new(StringComparer.OrdinalIgnoreCase);

        internal DatabaseSourceAccess(DatabaseReference reference)
        {
            m_SourceName = reference.Name;
        }

        internal IReadOnlyList<DatabaseAccessIssue> Issues => m_Issues;
        internal IReadOnlyCollection<string> UnlistedFolders => m_UnlistedFolders;
        internal bool PatientWasSkipped(string patientID) => m_SkippedPatients.Contains(patientID);

        internal void SkipPatient(string patientID, string folderPath)
        {
            if (m_SkippedPatients.Add(patientID)) m_Issues.Add(new DatabaseAccessIssue(m_SourceName, folderPath, patientID));
        }

        internal static T Read<T>(DatabaseSourceAccess access, DirectoryInfo directory, string patientID, Func<T> read)
        {
            if (access != null && (patientID == null ? access.m_UnlistedFolders.Contains(directory.FullName) : access.PatientWasSkipped(patientID))) return default;
            try
            {
                return read();
            }
            catch (UnauthorizedAccessException) when (access != null)
            {
                if (patientID != null) access.SkipPatient(patientID, directory.FullName);
                else if (access.m_UnlistedFolders.Add(directory.FullName)) access.m_Issues.Add(new DatabaseAccessIssue(access.m_SourceName, directory.FullName, null));
                return default;
            }
        }

        // FileSystemInfo.Exists also returns false when checking attributes is denied.
        internal static bool Exists(FileSystemInfo entry)
        {
            try
            {
                var isDirectory = (File.GetAttributes(entry.FullName) & FileAttributes.Directory) != 0;
                return isDirectory == (entry is DirectoryInfo);
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            catch (DirectoryNotFoundException)
            {
                return false;
            }
        }

        internal static DirectoryInfo[] ReadSourceDirectories(DatabaseSourceAccess access, DirectoryInfo directory)
        {
            return Read(access, directory, null, () => Exists(directory) ? directory.GetDirectories() : Array.Empty<DirectoryInfo>()) ?? Array.Empty<DirectoryInfo>();
        }
    }
}
