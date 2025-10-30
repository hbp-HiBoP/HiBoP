using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.UI.Tools;
using System.Linq;

namespace HBP.UI.Module3D
{
    public class CreateGroupSection : SiteToolSection
    {
        #region Public Methods
        public override async UniTask ApplyAsync()
        {
            await UniTask.SwitchToMainThread();
            var patients = Sites.Select(s => s.Information.Patient).Distinct();
            Group group = new Group("New group", patients);
            ObjectModifier<Group> modifier = WindowsManager.OpenModifier(group, null);
            modifier.OnOk.AddListener(() =>
            {
                // Generate unique name
                var projectGroups = ApplicationState.LoadedProject.Groups;
                if (projectGroups.Any(g => g.Name == group.Name))
                {
                    int count = 1;
                    string name = string.Format("{0}({1})", group.Name, count);
                    while (projectGroups.Any(g => g.Name == name))
                    {
                        count++;
                        name = string.Format("{0}({1})", group.Name, count);
                    }
                    group.Name = name;
                }
                ApplicationState.LoadedProject.AddGroup(group);
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Informational, "Group added to project", string.Format("The group {0} containing the {1} patients of the filtered sites has been added to the project.", group.Name, patients.Count())).Forget();
            });
        }
        public override void StoreSettings()
        {
            // No settings to store
        }
        public override void LoadSettings()
        {
            // No settings to load
        }
        #endregion
    }
}