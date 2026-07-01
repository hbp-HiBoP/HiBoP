using System;
using System.Reflection;
using HBP.Core.Data;
using HBP.Core.Tools;

namespace HBP.Tests.Serialization.Helpers
{
    internal sealed class ApplicationStateTestScope : IDisposable
    {
        private readonly Project m_LoadedProject;
        private readonly string m_LoadedProjectLocation;
        private readonly string m_TmpFolder;
        private readonly string m_ExtractProjectFolder;
        private readonly string m_DataPath;
        private readonly string m_DatabasePath;

        public ApplicationStateTestScope(string tempRoot)
        {
            m_LoadedProject = ApplicationState.LoadedProject;
            m_LoadedProjectLocation = ApplicationState.LoadedProjectLocation;
            m_TmpFolder = ApplicationState.TMPFolder;
            m_ExtractProjectFolder = ApplicationState.ExtractProjectFolder;
            m_DataPath = ApplicationState.DataPath;
            m_DatabasePath = ApplicationState.DatabasePath;

            SetPrivateStaticProperty(nameof(ApplicationState.TMPFolder), System.IO.Path.Combine(tempRoot, "tmp"));
            SetPrivateStaticProperty(nameof(ApplicationState.ExtractProjectFolder), System.IO.Path.Combine(tempRoot, "extract"));
            SetPrivateStaticProperty(nameof(ApplicationState.DataPath), System.IO.Path.Combine(tempRoot, "data"));
            SetPrivateStaticProperty(nameof(ApplicationState.DatabasePath), System.IO.Path.Combine(tempRoot, "database"));
            System.IO.Directory.CreateDirectory(ApplicationState.TMPFolder);
            System.IO.Directory.CreateDirectory(ApplicationState.DataPath);
            System.IO.Directory.CreateDirectory(ApplicationState.DatabasePath);
            ApplicationState.LoadedProject = null;
            ApplicationState.LoadedProjectLocation = string.Empty;
        }

        public void Dispose()
        {
            ApplicationState.LoadedProject = m_LoadedProject;
            ApplicationState.LoadedProjectLocation = m_LoadedProjectLocation;
            SetPrivateStaticProperty(nameof(ApplicationState.TMPFolder), m_TmpFolder);
            SetPrivateStaticProperty(nameof(ApplicationState.ExtractProjectFolder), m_ExtractProjectFolder);
            SetPrivateStaticProperty(nameof(ApplicationState.DataPath), m_DataPath);
            SetPrivateStaticProperty(nameof(ApplicationState.DatabasePath), m_DatabasePath);
        }

        private static void SetPrivateStaticProperty(string propertyName, string value)
        {
            PropertyInfo property = typeof(ApplicationState).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
            property.SetValue(null, value);
        }
    }
}
