using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Enums;

namespace HBP.UI.Tools
{
    public static class DeferredTagMigrationDialog
    {
        public static async UniTask InformAsync(DeferredTagMigrationPlan plan)
        {
            if (plan == null || !plan.RequiresConfirmation) return;
            await UniTask.SwitchToMainThread();
            string scope = GetScopeName(plan.Scope);
            StringBuilder message = new();
            message.AppendLine($"The {scope} was opened after automatically repairing its tag data.");
            message.AppendLine();
            message.AppendLine($"Patient values migrated: {plan.PatientValueCount}");
            message.AppendLine($"Site values migrated: {plan.SiteValueCount}");
            message.AppendLine($"Values removed: {plan.RemovedValueCount}");
            message.AppendLine($"Filter conditions repaired: {plan.FilterCount}");
            message.AppendLine($"Enum options added: {plan.EnumAdditionCount}");

            if (plan.RemovedValues.Count > 0)
            {
                message.AppendLine();
                message.AppendLine("Removed tag values by patient:");
                var summaries = plan.RemovedValues.GroupBy(removal => new { removal.PatientID, removal.PatientName }).Select(group => new
                {
                    Name = string.IsNullOrEmpty(group.Key.PatientName) ? "Unknown patient" : group.Key.PatientName,
                    PatientValues = group.Count(removal => removal.Scope == TagMigrationIssueScope.PatientValue),
                    SiteValues = group.Count(removal => removal.Scope == TagMigrationIssueScope.SiteValue)
                }).OrderBy(summary => summary.Name, System.StringComparer.OrdinalIgnoreCase).ThenBy(summary => summary.Name, System.StringComparer.Ordinal).ToArray();
                foreach (var summary in summaries.Take(15)) message.AppendLine($"• {summary.Name}: {FormatValueCount(summary.PatientValues, "patient")}, {FormatValueCount(summary.SiteValues, "site")} removed");

                if (summaries.Length > 15) message.AppendLine($"• … and {summaries.Length - 15} other patients");
            }

            if (plan.FilterRepairs.Count > 0)
            {
                message.AppendLine();
                message.AppendLine("Repaired filters:");
                foreach (FilterConditionRepair repair in plan.FilterRepairs.Take(15))
                {
                    string preset = string.IsNullOrEmpty(repair.PresetName) ? repair.PresetID : repair.PresetName;
                    message.AppendLine($"• {preset}, condition {repair.ConditionID}: {repair.Message}");
                }

                if (plan.FilterRepairs.Count > 15) message.AppendLine($"• … and {plan.FilterRepairs.Count - 15} other filter repairs");
            }

            if (plan.Warnings.Count > 0)
            {
                message.AppendLine();
                message.AppendLine("Conversion warnings:");
                foreach (string warning in plan.Warnings.Take(10)) message.AppendLine($"• {warning}");
            }

            message.AppendLine();
            message.AppendLine(plan.Scope == DeferredTagMigrationScope.Workspace ? "Synchronize the database again to restore values from a corrected source, or save the workspace to persist these repairs." : $"Save the {scope} to persist these repairs.");
            DialogBoxType type = plan.RemovedValueCount > 0 || plan.FilterRepairs.Count > 0 || plan.Warnings.Count > 0 ? DialogBoxType.Warning : DialogBoxType.Informational;
            await DialogBoxManager.OpenScrollableAsync(type, "Tag data repaired", message.ToString(), "Continue");
        }

        private static string FormatValueCount(int count, string scope)
        {
            return $"{count} {scope} tag {(count == 1 ? "value" : "values")}";
        }

        public static async UniTask InformStructuralRecoveryAsync(LoadingRecoveryReport report, string scope)
        {
            if (report == null || !report.HasIssues) return;
            await UniTask.SwitchToMainThread();
            StringBuilder message = new();
            message.AppendLine($"The {scope} was opened in read-only recovery mode.");
            message.AppendLine("Objects with unresolved structural references were excluded from the active data, while the source file was left unchanged.");
            message.AppendLine();
            foreach (LoadingRecoveryItem item in report.Items.Take(20))
            {
                message.AppendLine($"• {item.Kind} {item.ID}: {string.Join("; ", item.Reasons)}");
            }

            if (report.Items.Count > 20) message.AppendLine($"• … and {report.Items.Count - 20} other objects");
            message.AppendLine();
            message.AppendLine("Saving is disabled until these references are repaired or the data is reloaded from a corrected source.");
            await DialogBoxManager.OpenScrollableAsync(DialogBoxType.Warning, "Data opened in recovery mode", message.ToString(), "Continue");
        }

        private static string GetScopeName(DeferredTagMigrationScope scope)
        {
            return scope switch
            {
                DeferredTagMigrationScope.Project => "project",
                DeferredTagMigrationScope.Workspace => "database workspace",
                _ => "global filter presets"
            };
        }
    }
}
