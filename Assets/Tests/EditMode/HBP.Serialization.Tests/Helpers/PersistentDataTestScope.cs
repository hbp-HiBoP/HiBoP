using System;
using System.IO;
using System.Reflection;
using HBP.Core.Database;
using HBP.Core.Data;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HBP.Tests.Serialization.Helpers
{
    internal sealed class PersistentDataTestScope : IDisposable
    {
        private readonly string m_UserPreferencesPath;
        private readonly string m_TagCollectionPath;
        private readonly string m_AliasCollectionPath;
        private readonly string m_FilterPresetPath;
        private readonly string m_DatabaseSettingsPath;
        private readonly GameObject m_PersistentDataObject;
        private readonly GameObject m_DatabaseObject;

        public PersistentDataTestScope(string tempRoot)
        {
            m_UserPreferencesPath = UserPreferences.PATH;
            m_TagCollectionPath = TagCollection.PATH;
            m_AliasCollectionPath = AliasCollection.PATH;
            m_FilterPresetPath = FilterConditionsPresetCollection.PATH;
            m_DatabaseSettingsPath = GlobalDatabaseSettings.PATH;

            UserPreferences.PATH = Path.Combine(tempRoot, "Preferences.json");
            TagCollection.PATH = Path.Combine(tempRoot, "Tags.json");
            AliasCollection.PATH = Path.Combine(tempRoot, "Aliases.json");
            FilterConditionsPresetCollection.PATH = Path.Combine(tempRoot, "FilterPresets.json");
            GlobalDatabaseSettings.PATH = Path.Combine(tempRoot, "DatabaseSettings.json");

            ResetSingleton<PersistentDataManager>();
            ResetSingleton<DatabaseManager>();

            m_PersistentDataObject = new GameObject("PersistentDataManager_Test");
            EnsureInitialized(m_PersistentDataObject.AddComponent<PersistentDataManager>());

            m_DatabaseObject = new GameObject("DatabaseManager_Test");
            EnsureInitialized(m_DatabaseObject.AddComponent<DatabaseManager>());
        }

        public void Dispose()
        {
            if (m_DatabaseObject != null) Object.DestroyImmediate(m_DatabaseObject);
            if (m_PersistentDataObject != null) Object.DestroyImmediate(m_PersistentDataObject);

            ResetSingleton<DatabaseManager>();
            ResetSingleton<PersistentDataManager>();

            UserPreferences.PATH = m_UserPreferencesPath;
            TagCollection.PATH = m_TagCollectionPath;
            AliasCollection.PATH = m_AliasCollectionPath;
            FilterConditionsPresetCollection.PATH = m_FilterPresetPath;
            GlobalDatabaseSettings.PATH = m_DatabaseSettingsPath;
        }

        private static void ResetSingleton<T>() where T : MonoBehaviour
        {
            FieldInfo field = typeof(Singleton<T>).GetField("m_Instance", BindingFlags.NonPublic | BindingFlags.Static);
            field.SetValue(null, null);
        }

        private static void EnsureInitialized<T>(T manager) where T : MonoBehaviour
        {
            if (Singleton<T>.IsInitialized) return;

            MethodInfo awake = typeof(Singleton<T>).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            awake.Invoke(manager, null);
        }
    }
}
