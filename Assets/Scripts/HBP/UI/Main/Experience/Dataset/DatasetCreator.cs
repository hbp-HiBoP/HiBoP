using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Interfaces;
using HBP.UI.Tools;
using System.Linq;

namespace HBP.UI.Main
{
    /// <summary>
    /// Component to create datasets.
    /// </summary>
    public class DatasetCreator : ObjectCreator<Dataset>
    {
        #region Private Methods
        protected override async void SaveSelector(ObjectSelector<Dataset> selector, bool generateNewIDs = true)
        {
            var selectedDatasets = ExistingObjects.Where(o => selector.ObjectsSelected.Contains(o)).ToList();
            int numberOfExistingObjects = selectedDatasets.Count;

            if (numberOfExistingObjects > 0)
            {
                string message;
                if (numberOfExistingObjects == 1)
                {
                    message = $"Dataset '{selectedDatasets[0].Name}' will be overridden. Are you sure you want to override it?";
                }
                else
                {
                    var patientNames = selectedDatasets.Take(5).Select(p => p.Name).ToList();
                    string nameList = string.Join(", ", patientNames);
                    if (numberOfExistingObjects > 5)
                    {
                        nameList += ", ...";
                    }
                    message = $"{numberOfExistingObjects} datasets will be overridden ({nameList}). Are you sure you want to override them?";
                }

                int result = await DialogBoxManager.OpenAsync(DialogBoxType.Warning, "Override datasets", message, "Override", "Cancel");
                if (result == 1) return;
            }

            base.SaveSelector(selector, generateNewIDs);
        }
        #endregion
    }
}