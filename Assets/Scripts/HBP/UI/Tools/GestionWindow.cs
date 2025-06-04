using System.IO;
using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Interfaces;
using HBP.Core.Tools;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using System;

namespace HBP.UI.Tools
{
    /// <summary>
    /// Abstract generic class for every gestion window. A gestion window is a window to modify a list of elements.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class GestionWindow<T> : DialogWindow where T : Core.Data.BaseData, new()
    {
        #region Properties
        /// <summary>
        /// Class which manage the list of elements.
        /// </summary>
        public abstract ListGestion<T> ListGestion { get; }

        [SerializeField] protected Button m_ExportButton;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                ListGestion.Interactable = value;
                ListGestion.Modifiable = value;
                SetExport();
            }
        }

        protected List<T> m_OldValues = null;
        #endregion

        #region Protected Methods
        protected override void Initialize()
        {
            base.Initialize();
            ListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_ExportButton.onClick.AddListener(ExportSelected);
            ListGestion.List.OnSelect.AddListener((obj) => SetExport());
            ListGestion.List.OnDeselect.AddListener((obj) => SetExport());
        }
        protected async void ExportSelected()
        {
            string directory = await FileBrowser.GetExistingDirectoryNameAsync();
            if (string.IsNullOrEmpty(directory)) return;

            var selectedObjects = ListGestion.List.ObjectsSelected;
            foreach (var selectedObject in selectedObjects)
            {
                if (selectedObject is ILoadable<T> loadable && selectedObject is INameable nameable)
                {
                    ClassLoaderSaver.SaveToJSon(selectedObject, Path.Combine(directory, string.Format("{0}.{1}", nameable.Name, loadable.GetExtensions()[0])));
                }
            }
            DialogBoxManager.Open(Core.Enums.DialogBoxType.Informational, "Selected items have been saved", string.Format("{0} items have been saved at {1}.", selectedObjects.Length, directory)).Forget();
        }
        protected void SetExport()
        {
            var selectedObjects = ListGestion.List.ObjectsSelected;
            m_ExportButton.interactable = selectedObjects.Length > 0 && Interactable;
        }
        protected async virtual void SetList(IEnumerable<T> values)
        {
            ListGestion.List.Set(values);
            await UniTask.SwitchToThreadPool();
            m_OldValues = values.DeepClone().ToList();
        }
        protected async UniTask RestoreOldValuesAsync(IEnumerable<T> currentValues, Action<float, float, LoadingText> updateProgress)
        {
            await UniTask.WaitUntil(() => m_OldValues != null);
            await UniTask.SwitchToThreadPool();
            int length = currentValues.Count();
            int count = 0;
            foreach (var value in currentValues)
            {
                updateProgress.Invoke((float)count++ / length, 0, new LoadingText("Cancelling"));
                var oldValue = m_OldValues.FirstOrDefault(v => v.ID == value.ID);
                if (oldValue != null)
                {
                    value.Copy(oldValue);
                }
            }
        }
        #endregion
    }
}