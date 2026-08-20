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
            database.TagMigrationDecisionProvider = DeferredTagMigrationDialog.ConfirmAsync;

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
                int decision = await DialogBoxManager.OpenScrollableAsync(Core.Enums.DialogBoxType.Error, "Tag definitions recovery required", "The global tag definitions could not be loaded. The original Tags.json file was preserved. You can open the database read-only: tag values will be kept in a recovery quarantine and saving will remain disabled. To leave recovery mode, repair or restore Tags.json and restart HiBoP.\n\n" + PersistentDataManager.TagInitializationException, "Open read-only recovery", "Continue without database");
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

            await ShowPendingFilterRecoveryAsync();
        }

        public static async UniTask ShowPendingFilterRecoveryAsync()
        {
            Exception initializationException = PersistentDataManager.ConsumeFilterInitializationWarning();
            if (initializationException != null)
            {
                await DialogBoxManager.OpenScrollableAsync(Core.Enums.DialogBoxType.Warning, "Filter presets recovery required", "The global filter preset file could not be loaded and its original contents were preserved. HiBoP will continue without those presets; filter saving is disabled. Repair or restore FilterConditionsPresets.json and restart HiBoP.\n\n" + initializationException, "Continue");
                return;
            }

            FilterPresetRecoveryReport report = PersistentDataManager.ConsumeFilterRecoveryReport();
            if (report == null || !report.HasChanges) return;
            StringBuilder message = new();
            message.AppendLine("Invalid global tag filters were recovered without blocking your data:");
            message.AppendLine();
            message.AppendLine($"Current filters reset: {report.ResetCurrentPresetCount}");
            message.AppendLine($"Named presets disabled and preserved: {report.DisabledNamedPresetCount}");
            message.AppendLine($"Presets migrated: {report.MigratedPresetCount}");
            foreach (FilterPresetRecoveryIssue issue in report.Issues.Take(10))
            {
                string name = string.IsNullOrEmpty(issue.PresetName) ? issue.PresetID : issue.PresetName;
                message.AppendLine($"• {name}: {string.Join("; ", issue.Reasons)}");
            }

            await DialogBoxManager.OpenScrollableAsync(Core.Enums.DialogBoxType.Warning, "Tag filters recovered", message.ToString(), "Continue");
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
            AppendValueDiagnostics(stringBuilder, "Ignored tag values", report.TagDiagnostics.IgnoredValues, false);
            AppendValueDiagnostics(stringBuilder, "Incompatible tag values", report.TagDiagnostics.IncompatibleValues, true);
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

        private static void AppendValueDiagnostics(StringBuilder stringBuilder, string title, IEnumerable<TagImportValueDiagnostic> diagnostics, bool includeReason)
        {
            TagImportValueDiagnostic[] orderedDiagnostics = diagnostics.OrderBy(diagnostic => diagnostic.TagName, StringComparer.OrdinalIgnoreCase).ThenBy(diagnostic => diagnostic.Source, StringComparer.Ordinal).ThenBy(diagnostic => diagnostic.Owner, StringComparer.Ordinal).ThenBy(diagnostic => diagnostic.RawValue, StringComparer.Ordinal).ToArray();
            if (orderedDiagnostics.Length == 0) return;

            stringBuilder.AppendLine($"<b>{title}:</b>");
            foreach (TagImportValueDiagnostic diagnostic in orderedDiagnostics)
            {
                string count = diagnostic.Count > 1 ? $" x{diagnostic.Count}" : string.Empty;
                string owner = string.IsNullOrEmpty(diagnostic.Owner) ? string.Empty : $" [{diagnostic.Owner}]";
                string source = string.IsNullOrEmpty(diagnostic.Source) ? string.Empty : $" — {diagnostic.Source}";
                string reason = includeReason && !string.IsNullOrEmpty(diagnostic.Reason) ? $" — {diagnostic.Reason}" : string.Empty;
                stringBuilder.AppendLine($"{diagnostic.Category}: {diagnostic.TagName}{owner} = '{diagnostic.RawValue}'{count}{source}{reason}");
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
