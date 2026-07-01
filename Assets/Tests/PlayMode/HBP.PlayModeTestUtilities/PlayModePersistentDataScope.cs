using System;
using System.IO;
using System.Reflection;
using HBP.Core.Data;
using HBP.Core.Database;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HBP.Tests.PlayMode.Utilities
{
    public sealed class PlayModePersistentDataScope : IDisposable
    {
        private readonly string m_UserPreferencesPath;
        private readonly string m_TagCollectionPath;
        private readonly string m_AliasCollectionPath;
        private readonly string m_FilterPresetPath;
        private readonly string m_DatabaseSettingsPath;
        private readonly GameObject m_PersistentDataObject;
        private readonly GameObject m_DatabaseObject;

        public PlayModePersistentDataScope(string tempRoot)
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

            m_PersistentDataObject = new GameObject("PersistentDataManager_PlayModeTest");
            PersistentDataManager persistentDataManager = m_PersistentDataObject.AddComponent<PersistentDataManager>();
            SetSingleton(persistentDataManager);
            SetPrivateField(persistentDataManager, "m_UserPreferences", UserPreferences.Initialize());
            SetPrivateField(persistentDataManager, "m_Tags", TagCollection.Initialize());
            SetPrivateField(persistentDataManager, "m_Aliases", AliasCollection.Initialize());
            SetPrivateField(persistentDataManager, "m_FilterConditionsPresets", FilterConditionsPresetCollection.Initialize());

            m_DatabaseObject = new GameObject("DatabaseManager_PlayModeTest");
            DatabaseManager databaseManager = m_DatabaseObject.AddComponent<DatabaseManager>();
            SetSingleton(databaseManager);
            SetPrivateField(databaseManager, "m_Database", new GlobalDatabase());
        }

        public void Dispose()
        {
            if (m_DatabaseObject != null) Object.Destroy(m_DatabaseObject);
            if (m_PersistentDataObject != null) Object.Destroy(m_PersistentDataObject);

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

        private static void SetSingleton<T>(T manager) where T : MonoBehaviour
        {
            FieldInfo field = typeof(Singleton<T>).GetField("m_Instance", BindingFlags.NonPublic | BindingFlags.Static);
            field.SetValue(null, manager);
        }

        private static void SetPrivateField<T>(T target, string fieldName, object value)
        {
            FieldInfo field = typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(target, value);
        }
    }
}
