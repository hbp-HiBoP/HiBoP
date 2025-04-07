using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Interfaces;
using HBP.Data.Database;
using HBP.UI.Tools;
using System.Linq;

namespace HBP.UI.Main
{
    /// <summary>
    /// Component to create DataInfo.
    /// </summary>
    public class DataInfoCreator : ObjectCreator<DataInfo>
    {
        #region Public Methods
        /// <summary>
        /// Create a new DataInfo from scratch.
        /// </summary>
        public override void CreateFromScratch()
        {
            // FIXME: this is ugly
            var dataInfo = new IEEGDataInfo();
            var parentModifier = GetComponentInParent<DatasetModifier>();
            if (parentModifier != null)
            {
                dataInfo.Protocol = parentModifier.SelectedProtocol;
            }
            else
            {
                dataInfo.Protocol = DatabaseManager.Database.Protocols.First();
            }
            OpenModifier(dataInfo);
        }
        #endregion

        #region Private Methods
        protected override async UniTaskVoid SaveSelector(ObjectSelector<DataInfo> selector, bool generateNewIDs)
        {
            if (!generateNewIDs)
            {
                var selectedDataInfos = ExistingObjects.Where(o => selector.ObjectsSelected.Contains(o)).ToList();
                int numberOfExistingObjects = selectedDataInfos.Count;

                if (numberOfExistingObjects > 0)
                {
                    string message;
                    if (numberOfExistingObjects == 1)
                    {
                        message = $"Data '{selectedDataInfos[0].Name}' will be overridden. Are you sure you want to override it?";
                    }
                    else
                    {
                        var dataInfoNames = selectedDataInfos.Take(5).Select(p => p.Name).Distinct().ToList();
                        string nameList = string.Join(", ", dataInfoNames);
                        if (numberOfExistingObjects > 5)
                        {
                            nameList += ", ...";
                        }
                        message = $"{numberOfExistingObjects} data will be overridden ({nameList}). Are you sure you want to override them?";
                    }

                    int result = await DialogBoxManager.OpenAsync(DialogBoxType.Warning, "Override data", message, "Override", "Cancel");
                    if (result == 1) return;
                }
            }

            base.SaveSelector(selector, generateNewIDs).Forget();
        }
        protected override async UniTaskVoid LoadFromDirectory()
        {
#if UNITY_STANDALONE_OSX
            FileBrowser.GetExistingDirectoryNamesAsync(async (paths) =>
            {
                if (paths.Length > 0)
                {
                    ILoadableFromDirectory<DataInfo> loadable = new DataInfo() as ILoadableFromDirectory<DataInfo>;
                    var result = await LoadingManager.LoadAsync(update => loadable.LoadFromDirectory(paths, update));
                    foreach (var dataInfo in result) dataInfo.RequireErrorCheck = true;
                    var length = result.Count();
                    if (length > 0)
                    {
                        if (length == 1)
                            OnObjectCreated.Invoke(result.First());
                        else
                            OpenSelector(result, true, false, false);
                    }
                }
            });
#else
            string[] paths = FileBrowser.GetExistingDirectoryNames();
            if (paths.Length > 0)
            {
                ILoadableFromDirectory<DataInfo> loadable = new DataInfo() as ILoadableFromDirectory<DataInfo>;
                var result = await LoadingManager.LoadAsync(update => loadable.LoadFromDirectory(paths, update));
                foreach (var dataInfo in result) dataInfo.RequireErrorCheck = true;
                var length = result.Count();
                if (length > 0)
                {
                    if (length == 1)
                        OnObjectCreated.Invoke(result.First());
                    else
                        OpenSelector(result, true, false, false);
                }
            }
#endif
        }
        #endregion
    }
}