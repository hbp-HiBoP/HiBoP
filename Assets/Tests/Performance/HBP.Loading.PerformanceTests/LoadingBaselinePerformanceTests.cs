using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Database;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HBP.Tests.LoadingPerformance
{
    [Category("LoadingPerformance")]
    public class LoadingBaselinePerformanceTests
    {
        private static readonly Action<float, float, LoadingText> NoProgress = (_, _, _) => { };

        [Test]
        [Explicit("Opt-in loading baseline. Requires HBP_LOADING_BENCHMARK_ROOT.")]
        public async Task Database_WritesBaselineSummary()
        {
            string persistentRoot = RequiredEnvironmentVariable("HIBOP_LOADING_BENCHMARK_ROOT");
            string output = OutputDirectory();
            using BenchmarkEnvironment environment = new(persistentRoot);

            LoadingDiagnostics.SetOutputDirectoryForTests(output);
            try
            {
                await LoadDatabaseAsync(environment.Database);
                Assert.That(File.Exists(LoadingDiagnostics.LastSummaryPath), Is.True);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                LoadingDiagnostics.SetOutputDirectoryForTests(null);
            }
        }

        [Test]
        [Explicit("Opt-in loading baseline. Requires HBP_LOADING_BENCHMARK_ROOT and HBP_LOADING_BENCHMARK_PROJECT.")]
        public async Task Project_WritesBaselineSummary()
        {
            string persistentRoot = RequiredEnvironmentVariable("HIBOP_LOADING_BENCHMARK_ROOT");
            string projectPath = RequiredEnvironmentVariable("HIBOP_LOADING_BENCHMARK_PROJECT");
            string output = OutputDirectory();
            Assert.That(File.Exists(projectPath), Is.True, "The configured benchmark project does not exist.");

            using BenchmarkEnvironment environment = new(persistentRoot);
            LoadingDiagnostics.SetOutputDirectoryForTests(output);
            try
            {
                await InitializeDatabaseDependenciesAsync(environment.Database);

                using (LoadingDiagnostics.SessionScope session = LoadingDiagnostics.BeginSession(LoadingDiagnostics.Operation.Project))
                {
                    try
                    {
                        ProjectInfo info = new(projectPath);
                        Project project = new(info.Name, new HBP.Core.Data.ProjectPreferences());
                        ApplicationState.LoadedProject = project;
                        await project.LoadAsync(info, NoProgress, CancellationToken.None);
                        session.MarkSucceeded();
                    }
                    catch (OperationCanceledException)
                    {
                        session.MarkCanceled();
                        throw;
                    }
                    catch (Exception exception)
                    {
                        session.MarkFailed(exception);
                        throw;
                    }
                }

                Assert.That(File.Exists(LoadingDiagnostics.LastSummaryPath), Is.True);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                LoadingDiagnostics.SetOutputDirectoryForTests(null);
            }
        }

        private static async Task LoadDatabaseAsync(GlobalDatabase database)
        {
            using LoadingDiagnostics.SessionScope session = LoadingDiagnostics.BeginSession(LoadingDiagnostics.Operation.Database);
            try
            {
                await database.InitializeAsync();
                await database.LoadProtocolsAsync();
                await database.LoadDatabaseReferencesAsync();
                await database.LoadDatabaseAsync(NoProgress);
                session.MarkSucceeded();
            }
            catch (OperationCanceledException)
            {
                session.MarkCanceled();
                throw;
            }
            catch (Exception exception)
            {
                session.MarkFailed(exception);
                throw;
            }
        }

        private static async Task InitializeDatabaseDependenciesAsync(GlobalDatabase database)
        {
            await database.InitializeAsync();
            await database.LoadProtocolsAsync();
            await database.LoadDatabaseReferencesAsync();
            await database.LoadDatabaseAsync(NoProgress);
        }

        private static string RequiredEnvironmentVariable(string name)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                Assert.Ignore($"Set {name} to run the loading baseline.");
            }
            return value;
        }

        private static string OutputDirectory()
        {
            string output = Environment.GetEnvironmentVariable("HIBOP_LOADING_BENCHMARK_OUTPUT");
            return string.IsNullOrWhiteSpace(output)
                ? Path.Combine(Directory.GetCurrentDirectory(), ".test-results", "loading")
                : output;
        }

        private sealed class BenchmarkEnvironment : IDisposable
        {
            private readonly string m_TempRoot;
            private readonly string m_PreviousTmpFolder;
            private readonly string m_PreviousExtractProjectFolder;
            private readonly string m_PreviousDatabasePath;
            private readonly string m_PreviousUserPreferencesPath;
            private readonly string m_PreviousTagsPath;
            private readonly string m_PreviousAliasesPath;
            private readonly string m_PreviousFilterPresetsPath;
            private readonly string m_PreviousDatabaseSettingsPath;
            private readonly Project m_PreviousProject;
            private readonly GameObject m_PersistentDataObject;
            private readonly GameObject m_DatabaseObject;
            private readonly GlobalDatabase m_Database;

            public GlobalDatabase Database => m_Database;

            public BenchmarkEnvironment(string persistentRoot)
            {
                string databasePath = Path.Combine(persistentRoot, "Database");
                Assert.That(Directory.Exists(databasePath), Is.True, "The configured benchmark root does not contain a database.");
                Assert.That(File.Exists(Path.Combine(persistentRoot, "Tags.json")), Is.True, "The configured benchmark root does not contain Tags.json.");

                m_TempRoot = Path.Combine(Path.GetTempPath(), "hibop-loading-benchmark-" + Guid.NewGuid().ToString("N"));
                m_PreviousTmpFolder = ApplicationState.TMPFolder;
                m_PreviousExtractProjectFolder = ApplicationState.ExtractProjectFolder;
                m_PreviousDatabasePath = ApplicationState.DatabasePath;
                m_PreviousUserPreferencesPath = UserPreferences.PATH;
                m_PreviousTagsPath = TagCollection.PATH;
                m_PreviousAliasesPath = AliasCollection.PATH;
                m_PreviousFilterPresetsPath = FilterConditionsPresetCollection.PATH;
                m_PreviousDatabaseSettingsPath = GlobalDatabaseSettings.PATH;
                m_PreviousProject = ApplicationState.LoadedProject;

                SetApplicationStatePath(nameof(ApplicationState.TMPFolder), Path.Combine(m_TempRoot, "tmp"));
                SetApplicationStatePath(nameof(ApplicationState.ExtractProjectFolder), Path.Combine(m_TempRoot, "extract"));
                SetApplicationStatePath(nameof(ApplicationState.DatabasePath), databasePath);
                Directory.CreateDirectory(ApplicationState.TMPFolder);
                Directory.CreateDirectory(ApplicationState.ExtractProjectFolder);

                UserPreferences.PATH = Path.Combine(persistentRoot, "Preferences.txt");
                TagCollection.PATH = Path.Combine(persistentRoot, "Tags.json");
                AliasCollection.PATH = Path.Combine(persistentRoot, "Aliases.json");
                FilterConditionsPresetCollection.PATH = Path.Combine(persistentRoot, "FilterConditionsPresets.json");
                GlobalDatabaseSettings.PATH = Path.Combine(databasePath, "Settings.json");

                ResetSingleton<PersistentDataManager>();
                ResetSingleton<DatabaseManager>();
                m_PersistentDataObject = new GameObject("PersistentDataManager_LoadingBenchmark");
                PersistentDataManager persistentDataManager = m_PersistentDataObject.AddComponent<PersistentDataManager>();
                SetSingleton(persistentDataManager);
                SetPrivateField(persistentDataManager, "m_UserPreferences", UserPreferences.Initialize());
                SetPrivateField(persistentDataManager, "m_Tags", TagCollection.Initialize());
                SetPrivateField(persistentDataManager, "m_Aliases", AliasCollection.Initialize());
                SetPrivateField(
                    persistentDataManager,
                    "m_FilterConditionsPresets",
                    FilterConditionsPresetCollection.Initialize());

                m_DatabaseObject = new GameObject("DatabaseManager_LoadingBenchmark");
                DatabaseManager databaseManager = m_DatabaseObject.AddComponent<DatabaseManager>();
                SetSingleton(databaseManager);
                m_Database = new GlobalDatabase();
                SetPrivateField(databaseManager, "m_Database", m_Database);
            }

            public void Dispose()
            {
                if (m_DatabaseObject != null) Object.DestroyImmediate(m_DatabaseObject);
                if (m_PersistentDataObject != null) Object.DestroyImmediate(m_PersistentDataObject);
                ResetSingleton<DatabaseManager>();
                ResetSingleton<PersistentDataManager>();

                ApplicationState.LoadedProject = m_PreviousProject;
                SetApplicationStatePath(nameof(ApplicationState.TMPFolder), m_PreviousTmpFolder);
                SetApplicationStatePath(nameof(ApplicationState.ExtractProjectFolder), m_PreviousExtractProjectFolder);
                SetApplicationStatePath(nameof(ApplicationState.DatabasePath), m_PreviousDatabasePath);
                UserPreferences.PATH = m_PreviousUserPreferencesPath;
                TagCollection.PATH = m_PreviousTagsPath;
                AliasCollection.PATH = m_PreviousAliasesPath;
                FilterConditionsPresetCollection.PATH = m_PreviousFilterPresetsPath;
                GlobalDatabaseSettings.PATH = m_PreviousDatabaseSettingsPath;

                if (Directory.Exists(m_TempRoot))
                {
                    Directory.Delete(m_TempRoot, true);
                }
            }

            private static void SetApplicationStatePath(string propertyName, string value)
            {
                PropertyInfo property = typeof(ApplicationState).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
                property.SetValue(null, value);
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
}
