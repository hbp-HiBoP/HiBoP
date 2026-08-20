using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Enums;

namespace HBP.UI.Tools
{
    public static class DeferredTagMigrationDialog
    {
        public static async UniTask<DeferredTagMigrationDecision> ConfirmAsync(DeferredTagMigrationPlan plan)
        {
            await UniTask.SwitchToMainThread();
            string scope = GetScopeName(plan.Scope);
            StringBuilder message = new();
            message.AppendLine($"This {scope} contains tag values saved with definitions that differ from the current tag definitions.");
            message.AppendLine();
            message.AppendLine("The following migration will be applied while opening it:");
            foreach (DeferredTagMigrationChange change in plan.Changes.Take(20))
            {
                string sourceTypes = change.SerializedTypes.Count == 0 ? "unknown" : string.Join(", ", change.SerializedTypes);
                message.AppendLine($"• {change.Name}: {sourceTypes} → {change.CurrentType} ({change.ValueCount} values, {change.FilterCount} filters)");
            }

            if (plan.Changes.Count > 20) message.AppendLine($"• … and {plan.Changes.Count - 20} other tags");
            if (plan.EnumAdditionCount > 0) message.AppendLine($"• {plan.EnumAdditionCount} new enum values will be added to the global tag definitions.");
            if (plan.Warnings.Count > 0)
            {
                message.AppendLine();
                message.AppendLine("Warnings:");
                foreach (string warning in plan.Warnings.Take(10)) message.AppendLine($"• {warning}");
            }

            if (plan.Issues.Count > 0)
            {
                message.AppendLine();
                message.AppendLine("Values that cannot be migrated automatically:");
                foreach (TagMigrationIssue issue in plan.Issues.Take(15)) message.AppendLine($"• {issue}");
                if (plan.Issues.Count > 15) message.AppendLine($"• … and {plan.Issues.Count - 15} other issues");
            }

            message.AppendLine();
            message.AppendLine($"After opening, save the {scope} to persist the migrated values.");

            if (plan.Issues.Count == 0)
            {
                int result = await DialogBoxManager.OpenScrollableAsync(plan.LossyConversionCount > 0 || plan.Warnings.Count > 0 ? DialogBoxType.Warning : DialogBoxType.Informational, "Tag migration required", message.ToString(), "Open and migrate", "Cancel");
                return result == 0 ? DeferredTagMigrationDecision.Apply : DeferredTagMigrationDecision.Cancel;
            }

            if (plan.CanRemoveIncompatibleValues)
            {
                message.AppendLine();
                message.AppendLine($"{plan.RecoveryCount} incompatible values will be preserved in recovery storage and excluded from the active tags.");
                int result = await DialogBoxManager.OpenScrollableAsync(DialogBoxType.Warning, "Tag migration recovery", message.ToString(), "Open and recover", "Cancel");
                return result == 0 ? DeferredTagMigrationDecision.ApplyWithRecovery : DeferredTagMigrationDecision.Cancel;
            }

            message.AppendLine();
            message.AppendLine("The migration includes incompatible filter presets and cannot be completed automatically. Update or remove those presets before reopening.");
            await DialogBoxManager.OpenScrollableAsync(DialogBoxType.Error, "Tag migration blocked", message.ToString(), "Cancel");
            return DeferredTagMigrationDecision.Cancel;
        }

        public static async UniTask InformAsync(DeferredTagMigrationPlan plan)
        {
            if (plan == null || !plan.RequiresConfirmation) return;
            await UniTask.SwitchToMainThread();
            string scope = GetScopeName(plan.Scope);
            StringBuilder message = new();
            message.AppendLine($"The {scope} has been opened with its tag values updated to the current global definitions.");
            message.AppendLine();
            message.AppendLine($"Patient values migrated: {plan.PatientValueCount}");
            message.AppendLine($"Site values migrated: {plan.SiteValueCount}");
            message.AppendLine($"Enum options added: {plan.EnumAdditionCount}");
            message.AppendLine($"Values preserved in recovery storage: {plan.RecoveryCount}");
            if (plan.Warnings.Count > 0)
            {
                message.AppendLine();
                message.AppendLine("Warnings:");
                foreach (string warning in plan.Warnings.Take(10)) message.AppendLine($"• {warning}");
            }

            if (plan.Issues.Count > 0)
            {
                message.AppendLine();
                message.AppendLine("Recovered values:");
                foreach (TagMigrationIssue issue in plan.Issues.Take(15)) message.AppendLine($"• {issue}");
            }

            message.AppendLine();
            message.AppendLine($"Save the {scope} to persist the migrated values and recovery records.");
            await DialogBoxManager.OpenScrollableAsync(plan.Issues.Count > 0 || plan.Warnings.Count > 0 ? DialogBoxType.Warning : DialogBoxType.Informational, "Tag migration completed", message.ToString(), "Continue");
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
