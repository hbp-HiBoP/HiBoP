using System;
using System.IO;
using System.Reflection;
using HBP.Core.Database;
using HBP.Core.Data;
using HBP.Core.Object3D;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using UnityEngine;

namespace HBP.Tests.PlayMode.Utilities
{
    public sealed class PlayModeApplicationStateScope : IDisposable
    {
        private readonly Project m_LoadedProject;
        private readonly string m_LoadedProjectLocation;
        private readonly string m_TmpFolder;
        private readonly string m_ExtractProjectFolder;
        private readonly string m_DataPath;
        private readonly string m_DatabasePath;
        private readonly string m_DatabaseSettingsPath;

        public PlayModeApplicationStateScope(string tempRoot)
        {
            m_LoadedProject = ApplicationState.LoadedProject;
            m_LoadedProjectLocation = ApplicationState.LoadedProjectLocation;
            m_TmpFolder = ApplicationState.TMPFolder;
            m_ExtractProjectFolder = ApplicationState.ExtractProjectFolder;
            m_DataPath = ApplicationState.DataPath;
            m_DatabasePath = ApplicationState.DatabasePath;
            m_DatabaseSettingsPath = GlobalDatabaseSettings.PATH;

            SetPrivateStaticProperty(nameof(ApplicationState.TMPFolder), Path.Combine(tempRoot, "tmp"));
            SetPrivateStaticProperty(nameof(ApplicationState.ExtractProjectFolder), Path.Combine(tempRoot, "extract"));
            SetPrivateStaticProperty(nameof(ApplicationState.DataPath), Path.Combine(tempRoot, "data"));
            SetPrivateStaticProperty(nameof(ApplicationState.DatabasePath), Path.Combine(tempRoot, "database"));
            GlobalDatabaseSettings.PATH = Path.Combine(ApplicationState.DatabasePath, "Settings.json");
            Directory.CreateDirectory(ApplicationState.TMPFolder);
            Directory.CreateDirectory(ApplicationState.ExtractProjectFolder);
            Directory.CreateDirectory(ApplicationState.DataPath);
            Directory.CreateDirectory(ApplicationState.DatabasePath);
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
            GlobalDatabaseSettings.PATH = m_DatabaseSettingsPath;
            ResetSingleton<DatabaseManager>();
            ResetSingleton<PersistentDataManager>();
            Object3DManager.Reset();
        }

        private static void SetPrivateStaticProperty(string propertyName, string value)
        {
            PropertyInfo property = typeof(ApplicationState).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
            property.SetValue(null, value);
        }

        private static void ResetSingleton<T>() where T : MonoBehaviour
        {
            FieldInfo field = typeof(Singleton<T>).GetField("m_Instance", BindingFlags.NonPublic | BindingFlags.Static);
            field.SetValue(null, null);
        }
    }
}
