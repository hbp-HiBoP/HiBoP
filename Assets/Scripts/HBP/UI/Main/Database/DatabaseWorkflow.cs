using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Database;
using HBP.Core.Preferences;
using HBP.UI.Tools;
using HBP.Core.Tools;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;

namespace HBP.UI.Database
{
    public static class DatabaseWorkflow
    {
        private static LoadingRecoveryReport s_LastPresentedStructuralRecovery;

        public static async UniTask InitializeAsync()
        {
            await UniTask.WaitUntil(() => DatabaseManager.IsInitialized);

            var database = DatabaseManager.Database;
            await database.InitializeAsync();
            await UniTask.SwitchToMainThread();

            if (PersistentDataManager.TagInitializationException != null)
            {
                await database.LoadProtocolsAsync();
                UnityEngine.Debug.LogError("The global tag definitions could not be loaded. Database startup was skipped to preserve the original tag file. " + PersistentDataManager.TagInitializationException);
                return;
            }

            if (database.Settings.IsFirstUse)
            {
                await HandleDefaultProtocolsAsync(database);
            }

            await database.LoadProtocolsAsync();
            await UniTask.SwitchToMainThread();
            if (LoadingConcurrencyPolicy.BackgroundValidationEnabled)
            {
                try
                {
                    await database.StartLoadingSilentlyAsync();
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogWarning("Database startup was skipped. HiBoP will continue without a loaded database. " + exception.Message);
                }
            }
            else
            {
                await LoadingManager.LoadAsync<bool>(async update =>
                {
                    await database.LoadDatabaseAsync(update);
                    return true;
                });
            }
        }

        public static async UniTask<bool> LoadDatabaseAsync()
        {
            if (PersistentDataManager.TagInitializationException != null)
            {
                int decision = await DialogBoxManager.OpenScrollableAsync(Core.Enums.DialogBoxType.Error, "Tag definitions recovery required", "The global tag definitions could not be loaded. The original Tags.json file was preserved. You can open the database read-only: unresolved tag values will be omitted from the in-memory view and saving will remain disabled. Restore Tags.json and restart HiBoP, or resynchronize the database after restoring access to its source.\n\n" + PersistentDataManager.TagInitializationException, "Open read-only recovery", "Continue without database");
                if (decision != 0) return false;
            }

            try
            {
                bool loaded = await LoadingManager.LoadAsync<bool>(async (update, token) =>
                {
                    await DatabaseManager.Database.ReloadSelectedWorkspaceAsync(update, token);
                    return true;
                });
                await ShowDatabaseRecoveryReportsAsync();
                return loaded;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        public static async UniTask<bool> EnsureDatabaseReadyAndInformAsync()
        {
            GlobalDatabase database = DatabaseManager.Database;
            if (PersistentDataManager.TagInitializationException != null && !database.IsLoaded) return await LoadDatabaseAsync();
            try
            {
                if (database.NeedsReadyWait) await LoadingManager.LoadAsync((update, token) => database.EnsureDatabaseReadyAsync(update, token));
            }
            catch (Exception)
            {
                return false;
            }

            await ShowDatabaseRecoveryReportsAsync();
            return database.IsLoaded;
        }

        private static async UniTask ShowDatabaseRecoveryReportsAsync()
        {
            GlobalDatabase database = DatabaseManager.Database;
            await DeferredTagMigrationDialog.InformAsync(database.ConsumeTagMigrationReport());
            if (!ReferenceEquals(s_LastPresentedStructuralRecovery, database.StructuralRecoveryReport))
            {
                s_LastPresentedStructuralRecovery = database.StructuralRecoveryReport;
                await DeferredTagMigrationDialog.InformStructuralRecoveryAsync(database.StructuralRecoveryReport, "database workspace");
            }

            await ShowPendingFilterRepairAsync();
        }

        public static async UniTask ShowPendingFilterRepairAsync()
        {
            Exception initializationException = PersistentDataManager.ConsumeFilterInitializationWarning();
            if (initializationException != null)
            {
                await DialogBoxManager.OpenScrollableAsync(Core.Enums.DialogBoxType.Warning, "Filter presets recovery required", "The global filter preset file could not be loaded and its original contents were preserved. HiBoP will continue without those presets; filter saving is disabled. Repair or restore FilterConditionsPresets.json and restart HiBoP.\n\n" + initializationException, "Continue");
                return;
            }

            FilterPresetRepairReport report = PersistentDataManager.ConsumeFilterRepairReport();
            if (report == null || !report.HasChanges) return;
            StringBuilder message = new();
            message.AppendLine("Invalid global tag filters were repaired without disabling their presets:");
            message.AppendLine();
            message.AppendLine($"Presets migrated: {report.MigratedPresetCount}");
            message.AppendLine($"Filter conditions affected: {report.AffectedFilterCount}");
            message.AppendLine($"Filter conditions removed: {report.RemovedConditionCount}");
            foreach (FilterConditionRepair repair in report.Repairs.Take(15))
            {
                string name = string.IsNullOrEmpty(repair.PresetName) ? repair.PresetID : repair.PresetName;
                message.AppendLine($"• {name}, condition {repair.ConditionID}: {repair.Message}");
            }

            if (report.Repairs.Count > 15) message.AppendLine($"• … and {report.Repairs.Count - 15} other repairs");
            await DialogBoxManager.OpenScrollableAsync(Core.Enums.DialogBoxType.Warning, "Tag filters repaired", message.ToString(), "Continue");
        }

        public static async UniTask SaveDatabaseAsync(string expectedWorkspaceID = null)
        {
            if (DatabaseManager.Database.StructuralRecoveryReport.HasIssues) throw new InvalidOperationException("The database workspace is open in read-only structural recovery mode and cannot be saved.");
            await LoadingManager.LoadAsync<bool>(async update =>
            {
                await UniTask.SwitchToMainThread();
                if (PersistentDataManager.TagInitializationException != null) throw new InvalidOperationException("The database is open with an invalid Tags.json file. Repair or restore the tag definitions before saving.");
                if (PersistentDataManager.Tags.HasUnsavedTagMigration) PersistentDataManager.Tags.Save();
                if (PersistentDataManager.FilterInitializationException == null && PersistentDataManager.FilterConditionsPresets.HasUnsavedTagMigration) PersistentDataManager.FilterConditionsPresets.Save();
                await DatabaseManager.Database.SaveDatabaseAsync(update, expectedWorkspaceID);
                return true;
            });
        }

        public static async UniTask SaveDatabaseReferencesAsync()
        {
            if (PersistentDataManager.TagInitializationException != null) throw new InvalidOperationException("Database references cannot be saved while Tags.json is invalid.");
            if (DatabaseManager.Database.StructuralRecoveryReport.HasIssues) throw new InvalidOperationException("Database references cannot be saved while the workspace is open in structural recovery mode.");
            await DatabaseManager.Database.SaveDatabaseReferencesAsync();
            await SaveDatabaseAsync();
        }

        public static async UniTask SaveProtocolsAsync()
        {
            await DatabaseManager.Database.SaveProtocolsAsync();
        }

        public static async UniTask UpdateDatabasesAsync(IEnumerable<DatabaseReference> databaseReferences)
        {
            var database = DatabaseManager.Database;
            DatabaseUpdateTransaction transaction = await LoadingManager.LoadAsync((update, token) => database.UpdateDatabasesAsync(databaseReferences, update, token));
            await LoadingManager.LoadAsync<bool>(async update =>
            {
                await database.SaveDatabaseUpdateAsync(transaction, update);
                return true;
            });
            await UniTask.SwitchToMainThread();
            await ShowUpdateReportAsync(transaction.Report);
        }

        private static async UniTask HandleDefaultProtocolsAsync(GlobalDatabase database)
        {
            var result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Informational, "Default Protocols", "The default protocols have not yet been imported. Do you want to import them?", "Yes", "Later", "Never");
            if (result == 0)
            {
                database.ConfigureDefault();
                await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Informational, "Default Protocols", "The default protocols have been imported.", "OK");
                database.Settings.IsFirstUse = false;
            }
            else if (result == 2)
            {
                database.Settings.IsFirstUse = false;
            }

            database.SaveSettings();
        }

        private static async UniTask ShowUpdateReportAsync(DatabaseUpdateReport report)
        {
            if (!report.HasChanges) return;

            await DialogBoxManager.OpenScrollableAsync(Core.Enums.DialogBoxType.Informational, "Databases updated", BuildUpdateReport(report), "OK");
        }

        private static string BuildUpdateReport(DatabaseUpdateReport report)
        {
            StringBuilder stringBuilder = new();
            AppendPatients(stringBuilder, "Removed patients", report.RemovedPatients);
            AppendPatients(stringBuilder, "Added patients", report.AddedPatients);
            AppendPatients(stringBuilder, "Updated patients", report.UpdatedPatients);
            AppendCreatedTags(stringBuilder, report.TagDiagnostics.CreatedTags);
            AppendEnumExtensions(stringBuilder, report.TagDiagnostics.EnumExtensions);
            AppendValueDiagnostics(stringBuilder, "Ignored tag values", report.TagDiagnostics.IgnoredValues);
            AppendValueDiagnostics(stringBuilder, "Incompatible tag values", report.TagDiagnostics.IncompatibleValues);
            return stringBuilder.ToString();
        }

        private static void AppendCreatedTags(StringBuilder stringBuilder, IEnumerable<TagImportCreatedTag> tags)
        {
            TagImportCreatedTag[] orderedTags = tags.OrderBy(tag => tag.Category).ThenBy(tag => tag.TagName, StringComparer.OrdinalIgnoreCase).ThenBy(tag => tag.TagName, StringComparer.Ordinal).ToArray();
            if (orderedTags.Length == 0) return;

            stringBuilder.AppendLine("<b>Created tags:</b>");
            foreach (TagImportCreatedTag tag in orderedTags) stringBuilder.AppendLine($"{tag.Category}: {tag.TagName} ({tag.TagType})");
            stringBuilder.AppendLine();
        }

        private static void AppendEnumExtensions(StringBuilder stringBuilder, IEnumerable<TagImportEnumExtension> extensions)
        {
            TagImportEnumExtension[] orderedExtensions = extensions.OrderBy(extension => extension.TagName, StringComparer.OrdinalIgnoreCase).ThenBy(extension => extension.TagID, StringComparer.Ordinal).ToArray();
            if (orderedExtensions.Length == 0) return;

            stringBuilder.AppendLine("<b>Enum options added:</b>");
            foreach (TagImportEnumExtension extension in orderedExtensions) stringBuilder.AppendLine($"{extension.TagName}: {string.Join(", ", extension.Values)}");
            stringBuilder.AppendLine();
        }

        private static void AppendValueDiagnostics(StringBuilder stringBuilder, string title, IEnumerable<TagImportValueDiagnostic> diagnostics)
        {
            Dictionary<(TagCategory Category, string TagID, string TagName), int> countByTag = new();
            foreach (TagImportValueDiagnostic diagnostic in diagnostics)
            {
                var key = (diagnostic.Category, diagnostic.TagID, diagnostic.TagName);
                countByTag[key] = countByTag.TryGetValue(key, out int count) ? count + diagnostic.Count : diagnostic.Count;
            }

            if (countByTag.Count == 0) return;

            stringBuilder.AppendLine($"<b>{title}:</b>");
            foreach (var summary in countByTag.OrderBy(pair => pair.Key.Category).ThenBy(pair => pair.Key.TagName, StringComparer.OrdinalIgnoreCase).ThenBy(pair => pair.Key.TagName, StringComparer.Ordinal))
            {
                string valueLabel = summary.Value == 1 ? "value" : "values";
                stringBuilder.AppendLine($"{summary.Key.Category}: {summary.Key.TagName} ({summary.Value} {valueLabel})");
            }

            stringBuilder.AppendLine();
        }

        private static void AppendPatients(StringBuilder stringBuilder, string title, IEnumerable<Patient> patients)
        {
            Patient[] orderedPatients = patients.OrderBy(p => p.Name).ToArray();
            if (orderedPatients.Length == 0) return;

            stringBuilder.AppendLine($"<b>{title}:</b>");
            foreach (var patient in orderedPatients)
            {
                stringBuilder.AppendLine(patient.ID);
            }

            stringBuilder.AppendLine();
        }
    }
}
