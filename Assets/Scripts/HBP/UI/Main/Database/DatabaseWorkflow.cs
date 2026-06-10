using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Database;
using HBP.UI.Tools;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HBP.UI.Database
{
    public static class DatabaseWorkflow
    {
        public static async UniTask InitializeAsync()
        {
            await UniTask.WaitUntil(() => DatabaseManager.IsInitialized);

            var database = DatabaseManager.Database;
            await database.InitializeAsync();
            await UniTask.SwitchToMainThread();

            if (database.Settings.IsFirstUse)
            {
                await HandleDefaultProtocolsAsync(database);
            }

            await database.LoadProtocolsAsync();
            await LoadDatabaseAsync();
        }
        public static async UniTask LoadDatabaseAsync()
        {
            var database = DatabaseManager.Database;
            await database.LoadDatabaseReferencesAsync();
            await LoadingManager.LoadAsync(update => database.LoadDatabaseAsync(update));
        }
        public static async UniTask SaveDatabaseAsync()
        {
            await LoadingManager.LoadAsync(update => DatabaseManager.Database.SaveDatabaseAsync(update));
        }
        public static async UniTask SaveDatabaseReferencesAsync()
        {
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
            var report = await LoadingManager.LoadAsync((update, token) => database.UpdateDatabasesAsync(databaseReferences, update, token));
            await SaveDatabaseAsync();
            await UniTask.SwitchToMainThread();
            database.OnUpdateDatabases.Invoke();
            await ShowUpdateReportAsync(report);
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
            return stringBuilder.ToString();
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
