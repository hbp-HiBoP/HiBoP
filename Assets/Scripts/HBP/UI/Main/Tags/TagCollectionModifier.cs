using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System;
using UnityEngine;
using HBP.UI.Tools;
using HBP.Core.Tools;
using HBP.Core.Preferences;
using HBP.Core.Database;
using HBP.UI.Database;

namespace HBP.UI.Main
{
    public class TagCollectionModifier : ObjectModifier<Core.Data.TagCollection>
    {
        #region Properties

        [SerializeField] GeneralTagsSubModifiers m_GeneralSubModifiers;
        [SerializeField] PatientsTagsSubModifiers m_PatientsSubModifiers;
        [SerializeField] SitesTagsSubModifiers m_SitesSubModifiers;
        int m_ReplanAttempts;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                m_GeneralSubModifiers.Interactable = value;
                m_PatientsSubModifiers.Interactable = value;
                m_SitesSubModifiers.Interactable = value;
            }
        }

        public ReadOnlyCollection<Core.Data.BaseTag> ModifiedTags
        {
            get
            {
                List<Core.Data.BaseTag> tags = new();
                tags.AddRange(m_GeneralSubModifiers.ModifiedTags);
                tags.AddRange(m_PatientsSubModifiers.ModifiedTags);
                tags.AddRange(m_SitesSubModifiers.ModifiedTags);
                return new ReadOnlyCollection<Core.Data.BaseTag>(tags);
            }
        }

        #endregion

        #region Public Methods

        public override async void OK()
        {
            if (PersistentDataManager.TagInitializationException != null)
            {
                await DialogBoxManager.OpenScrollableAsync(Core.Enums.DialogBoxType.Error, "Tag definitions recovery required", "Tags.json is invalid and was preserved. Tag definitions cannot be edited or saved until that file is repaired or restored and HiBoP is restarted.", "Continue");
                return;
            }

            if (PersistentDataManager.FilterInitializationException != null)
            {
                await DialogBoxManager.OpenScrollableAsync(Core.Enums.DialogBoxType.Error, "Filter presets recovery required", "FilterConditionsPresets.json is invalid and was preserved. Tag definitions cannot be changed safely until that file is repaired or restored and HiBoP is restarted.", "Continue");
                return;
            }

            WindowsReferencer.SaveAll();
            if (m_GeneralSubModifiers.TagListGestion.HasBeenModified || m_PatientsSubModifiers.TagListGestion.HasBeenModified || m_SitesSubModifiers.TagListGestion.HasBeenModified)
            {
                m_GeneralSubModifiers.Save();
                m_PatientsSubModifiers.Save();
                m_SitesSubModifiers.Save();

                HashSet<string> modifiedTagIds = new(ModifiedTags.Where(tag => tag != null && !string.IsNullOrEmpty(tag.ID)).Select(tag => tag.ID), StringComparer.Ordinal);
                foreach (Core.Data.BaseTag removedTag in Object.AllTags.Where(tag => !ObjectTemp.ContainsTagId(tag.ID))) modifiedTagIds.Add(removedTag.ID);

                List<Core.Data.Patient> patients = GetPatientsToCheck();
                Core.Data.Project loadedProject = ApplicationState.LoadedProject;
                bool databaseWasLoaded = DatabaseManager.Database.IsLoaded;
                string workspaceID = DatabaseManager.Database.Settings.SelectedWorkspace?.ID;
                Core.Data.TagSchemaMigrationService migrationService = new();
                Core.Data.TagSchemaMigrationPlan plan = null;
                try
                {
                    await LoadingManager.LoadAsync<bool>(async update =>
                    {
                        await Cysharp.Threading.Tasks.UniTask.SwitchToMainThread();
                        update(0, 0, new LoadingText("Planning tag migration"));
                        plan = migrationService.Plan(Object, ObjectTemp, patients, PersistentDataManager.FilterConditionsPresets, modifiedTagIds, Core.Data.TagParsingPolicy.Default);
                        update(1, 0, new LoadingText("Tag migration planned"));
                        return true;
                    });
                    await Cysharp.Threading.Tasks.UniTask.SwitchToMainThread();
                }
                catch
                {
                    await Cysharp.Threading.Tasks.UniTask.SwitchToMainThread();
                    return;
                }

                if (!migrationService.Validate(plan))
                {
                    string issueDetails = string.Join("\n", plan.Issues.Take(8).Select(issue => "• " + issue));
                    if (plan.Issues.Count > 8) issueDetails += $"\n• ... and {plan.Issues.Count - 8} more issue(s)";
                    bool canQuarantine = plan.Issues.All(issue => issue.Scope is Core.Data.TagMigrationIssueScope.PatientValue or Core.Data.TagMigrationIssueScope.SiteValue);
                    string recoveryMessage = "Some values or filters cannot be converted:\n\n" + issueDetails + "\n\nYou can preserve incompatible values in recovery storage, keep the affected tag definitions unchanged while applying independent changes, or cancel.";
                    int recoveryDecision = canQuarantine ? await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Warning, "Some tag changes need recovery", recoveryMessage, "Apply with recovery", "Keep affected tags unchanged", "Cancel") : await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Warning, "Some tag changes need recovery", recoveryMessage, "Keep affected tags unchanged", "Cancel");
                    if ((canQuarantine && recoveryDecision == 2) || (!canQuarantine && recoveryDecision != 0)) return;

                    if (canQuarantine && recoveryDecision == 0)
                    {
                        plan = migrationService.Plan(Object, ObjectTemp, patients, PersistentDataManager.FilterConditionsPresets, modifiedTagIds, Core.Data.TagParsingPolicy.Default, true);
                    }
                    else
                    {
                        HashSet<string> blockedTagIDs = new(plan.Issues.Where(issue => !string.IsNullOrEmpty(issue.TagID)).Select(issue => issue.TagID), StringComparer.Ordinal);
                        if (blockedTagIDs.Count == 0) return;
                        Core.Data.TagCollection partialProposal = BuildPartialProposal(Object, ObjectTemp, blockedTagIDs);
                        modifiedTagIds.ExceptWith(blockedTagIDs);
                        if (modifiedTagIds.Count == 0) return;
                        plan = migrationService.Plan(Object, partialProposal, patients, PersistentDataManager.FilterConditionsPresets, modifiedTagIds, Core.Data.TagParsingPolicy.Default);
                        if (!migrationService.Validate(plan))
                        {
                            await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Error, "Remaining tag changes blocked", string.Join("\n", plan.Issues.Take(8).Select(issue => "• " + issue)), "OK");
                            return;
                        }

                        ObjectTemp.Copy(partialProposal);
                    }
                }

                string summary = BuildMigrationSummary(plan, loadedProject != null);
                int result = await DialogBoxManager.OpenAsync(plan.LossyConversionCount > 0 || plan.DestructiveConversionCount > 0 ? Core.Enums.DialogBoxType.Warning : Core.Enums.DialogBoxType.Informational, "Confirm tag migration", summary, "Apply", "Cancel");
                if (result != 0) return;

                if (!IsMigrationContextCurrent(loadedProject, databaseWasLoaded, workspaceID) || !plan.MatchesOwnerGraph(GetPatientsToCheck()))
                {
                    if (m_ReplanAttempts++ == 0)
                    {
                        OK();
                        return;
                    }

                    m_ReplanAttempts = 0;
                    await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Warning, "Tag migration changed again", "The loaded project or database workspace changed repeatedly while the migration was being reviewed. Retry when background changes have completed.", "OK");
                    return;
                }

                try
                {
                    migrationService.Commit(plan);
                    plan.CopyPreparedTagsTo(ObjectTemp);
                    PersistentDataManager.Tags.Save();
                    PersistentDataManager.FilterConditionsPresets.Save();
                    if (databaseWasLoaded) await DatabaseWorkflow.SaveDatabaseAsync(workspaceID);
                    await Cysharp.Threading.Tasks.UniTask.SwitchToMainThread();
                    if (!IsMigrationContextCurrent(loadedProject, databaseWasLoaded, workspaceID) || !plan.MatchesOwnerGraph(GetPatientsToCheck()))
                    {
                        throw new InvalidOperationException("The loaded project or database workspace changed while the migration was being saved.");
                    }

                    base.OK();
                    m_ReplanAttempts = 0;
                }
                catch (Exception exception)
                {
                    await Cysharp.Threading.Tasks.UniTask.SwitchToMainThread();
                    plan.Rollback();
                    bool persistenceRestored = await TryRestorePersistentState(databaseWasLoaded, workspaceID);
                    await Cysharp.Threading.Tasks.UniTask.SwitchToMainThread();
                    Debug.LogException(exception);
                    string recoveryMessage = persistenceRestored ? "In-memory and persisted changes were rolled back." : "In-memory changes were rolled back, but restoring the persisted files also failed. See the logs before closing HiBoP.";
                    await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Error, "Tag migration failed", $"The migration could not be saved. {recoveryMessage}", "OK");
                    return;
                }
            }
            else
            {
                base.OK();
            }
        }

        #endregion

        #region Private Methods

        private List<Core.Data.Patient> GetPatientsToCheck()
        {
            List<Core.Data.Patient> patients = new();
            if (ApplicationState.LoadedProject != null) AddPatientsByReference(patients, ApplicationState.LoadedProject.Patients);
            if (DatabaseManager.Database.IsLoaded) AddPatientsByReference(patients, DatabaseManager.Database.Patients);
            return patients;
        }

        private static void AddPatientsByReference(List<Core.Data.Patient> destination, IEnumerable<Core.Data.Patient> patients)
        {
            foreach (Core.Data.Patient patient in patients.Where(patient => patient != null))
            {
                if (!destination.Any(existing => ReferenceEquals(existing, patient)))
                {
                    destination.Add(patient);
                }
            }
        }

        private static string BuildMigrationSummary(Core.Data.TagSchemaMigrationPlan plan, bool hasLoadedProject)
        {
            List<string> lines = new()
            {
                $"Tag definitions changed: {plan.ChangedDefinitionCount}",
                $"Patient values migrated: {plan.PatientValueCount}",
                $"Site values migrated: {plan.SiteValueCount}",
                $"Filters migrated: {plan.FilterCount}",
                $"Lossy conversions: {plan.LossyConversionCount}",
                $"Destructive conversions: {plan.DestructiveConversionCount}"
            };
            if (plan.RecoveredValueCount > 0) lines.Add($"Values preserved in recovery storage: {plan.RecoveredValueCount}");
            if (plan.Warnings.Count > 0) lines.Add($"Warnings: {plan.Warnings.Count}");
            if (plan.DefinitionChanges.Count > 0)
            {
                lines.Add("\nChanges:");
                lines.AddRange(plan.DefinitionChanges.Take(8).Select(change => $"• {change.Name} ({change.Category}): {change.PreviousType} → {change.NewType}"));
                if (plan.DefinitionChanges.Count > 8) lines.Add($"• ... and {plan.DefinitionChanges.Count - 8} more change(s)");
            }

            if (hasLoadedProject) lines.Add("\nThe loaded project will be updated in memory. Save the project afterwards to persist these changes.");
            if (!DatabaseManager.Database.IsLoaded) lines.Add("\nThe selected database workspace is not loaded. Its values will be migrated with recovery when that workspace is next opened.");
            return string.Join("\n", lines);
        }

        private static Core.Data.TagCollection BuildPartialProposal(Core.Data.TagCollection current, Core.Data.TagCollection proposed, ISet<string> blockedTagIDs)
        {
            IEnumerable<Core.Data.BaseTag> Merge(IEnumerable<Core.Data.BaseTag> currentCategory, IEnumerable<Core.Data.BaseTag> proposedCategory)
            {
                List<Core.Data.BaseTag> result = proposedCategory.Where(tag => tag != null && !blockedTagIDs.Contains(tag.ID)).ToList();
                result.AddRange(currentCategory.Where(tag => tag != null && blockedTagIDs.Contains(tag.ID)));
                return result;
            }

            return new Core.Data.TagCollection(Merge(current.GeneralTags, proposed.GeneralTags), Merge(current.PatientsTags, proposed.PatientsTags), Merge(current.SitesTags, proposed.SitesTags), proposed.ID);
        }

        private static bool IsMigrationContextCurrent(Core.Data.Project loadedProject, bool databaseWasLoaded, string workspaceID)
        {
            return ReferenceEquals(ApplicationState.LoadedProject, loadedProject) && DatabaseManager.Database.IsLoaded == databaseWasLoaded && DatabaseManager.Database.Settings.SelectedWorkspace?.ID == workspaceID;
        }

        private static async Cysharp.Threading.Tasks.UniTask<bool> TryRestorePersistentState(bool databaseWasLoaded, string workspaceID)
        {
            try
            {
                PersistentDataManager.Tags.Save();
                PersistentDataManager.FilterConditionsPresets.Save();
                if (databaseWasLoaded)
                {
                    if (DatabaseManager.Database.Settings.SelectedWorkspace?.ID != workspaceID) return false;
                    await DatabaseWorkflow.SaveDatabaseAsync(workspaceID);
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
        }

        #endregion

        #region Protected Methods

        protected override void Initialize()
        {
            base.Initialize();
            m_GeneralSubModifiers.Initialize();
            m_PatientsSubModifiers.Initialize();
            m_SitesSubModifiers.Initialize();
        }

        protected override void SetFields(Core.Data.TagCollection objectToDisplay)
        {
            m_GeneralSubModifiers.Object = objectToDisplay;
            m_PatientsSubModifiers.Object = objectToDisplay;
            m_SitesSubModifiers.Object = objectToDisplay;
        }

        #endregion
    }
}
