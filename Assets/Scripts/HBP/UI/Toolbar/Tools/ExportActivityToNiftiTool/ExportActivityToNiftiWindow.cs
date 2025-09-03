using Cysharp.Threading.Tasks;
using HBP.Core.Exceptions;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Data.Preferences;
using HBP.UI.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Toolbar
{
    public class ExportActivityToNiftiWindow : DialogWindow
    {
        #region Properties
        [SerializeField] private GameObject m_ColumnItemPrefab;
        [SerializeField] private Transform m_ColumnItemContainer;
        private List<ExportActivityColumnItem> m_ColumnItems = new List<ExportActivityColumnItem>();

        [SerializeField] private Toggle m_NiiToggle;
        [SerializeField] private Toggle m_NiiGzToggle;
        [SerializeField] private FolderSelector m_ExportFolderSelector;
        #endregion

        #region Public Methods
        public override async void OK()
        {
            if (!Module3DMain.SelectedScene.IsGeneratorUpToDate)
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Activity not projected", "The activity is not projected on the brain. Please project the activity before exporting it as Nifti.", "OK").Forget();
                return;
            }
            if (!new DirectoryInfo(m_ExportFolderSelector.Folder).Exists)
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Folder does not exist", "The selected export folder does not exist. Please select an existing folder.", "OK").Forget();
                return;
            }
            if (m_ColumnItems.Any(c => c.FileName == string.Empty))
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Empty file name", "Please fill all the file names.", "OK").Forget();
                return;
            }
            if (m_ColumnItems.Where(c => c.IsSelected).Count() == 0)
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "No column selected", "Please select at least one column to export.", "OK").Forget();
                return;
            }
            base.OK();
            await LoadingManager.LoadAsync(SaveActivityAsNifti);
            DialogBoxManager.Open(Core.Enums.DialogBoxType.Informational, "Export complete", "The export of the activity to Nifti is complete.", "OK").Forget();
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();

            OnSelectScene(Module3DMain.SelectedScene);
            Module3DMain.OnSelectScene.AddListener(OnSelectScene);

            m_NiiToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    foreach (ExportActivityColumnItem item in m_ColumnItems)
                    {
                        item.Extension = ".nii";
                    }
                }
            });
            m_NiiGzToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    foreach (ExportActivityColumnItem item in m_ColumnItems)
                    {
                        item.Extension = ".nii.gz";
                    }
                }
            });

            m_ExportFolderSelector.Folder = PersistentDataManager.UserPreferences.General.Project.DefaultExportLocation;
        }
        private void OnSelectScene(Base3DScene scene)
        {
            foreach (GameObject item in m_ColumnItemContainer)
            {
                Destroy(item);
            }

            if (scene != null)
            {
                foreach (Column3D column in scene.Columns)
                {
                    ExportActivityColumnItem item = Instantiate(m_ColumnItemPrefab, m_ColumnItemContainer).GetComponent<ExportActivityColumnItem>();
                    item.Initialize(column);
                    m_ColumnItems.Add(item);
                }
            }
        }
        private async UniTask SaveActivityAsNifti(Action<float, float, LoadingText> onChangeProgress, CancellationToken token)
        {
            var selectedColumns = m_ColumnItems.Where(c => c.IsSelected).ToList();
            Core.DLL.ActivityGenerator currentGenerator = null;
            LoadingText currentMessage = new();
            int currentColumn = 0;
            int numberOfColumns = selectedColumns.Count;
            async UniTaskVoid checkProgress(CancellationToken cancellationToken)
            {
                while (true)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    float currentProgress = 0;
                    if (currentGenerator != null)
                    {
                        currentProgress = ((float)currentColumn / numberOfColumns) + (currentGenerator.Progress / numberOfColumns);
                    }
                    onChangeProgress.Invoke(currentProgress, 0, currentMessage);
                    await UniTask.WaitForSeconds(0.05f);
                }
            }
            CancellationTokenSource source = new();
            checkProgress(source.Token).Forget();

            await UniTask.SwitchToThreadPool();

            foreach (ExportActivityColumnItem item in selectedColumns)
            {
                if (token.IsCancellationRequested) break;
                if (item.AssociatedColumn is Column3DIEEG column)
                {
                    currentGenerator = column.ActivityGenerator;
                    currentMessage = new LoadingText($"Exporting ", $"{column.Name} as {item.FileName}", $" [{currentColumn + 1}/{numberOfColumns}]");
                    string path = Path.Join(m_ExportFolderSelector.Folder, item.FileName);
                    bool success = currentGenerator.SaveActivityAsNifti(path, column.Timeline.CurrentSubtimeline, $"IEEG Activity of {column.ColumnIEEGData.Dataset.Protocol.Name} - {column.ColumnIEEGData.Bloc.Name} - {column.ColumnIEEGData.DataName}");
                    if (!success)
                    {
                        throw new HBPException("Export failed", $"The export of the activity for column {item.AssociatedColumn.Name} failed.");
                    }
                }
                currentColumn++;
            }
            source.Cancel();
        }
        #endregion
    }
}