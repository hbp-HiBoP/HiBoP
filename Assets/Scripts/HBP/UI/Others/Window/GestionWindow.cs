using System;
using System.IO;
using Tools.Unity;
using Tools.Unity.Components;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI
{
    /// <summary>
    /// Abstract generic class for every gestion window. A gestion window is a window to modify a list of elements.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class GestionWindow<T> : DialogWindow where T : Data.BaseData, new()
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
        #endregion

        #region Protected Methods
        protected override void Initialize()
        {
            base.Initialize();
            ListGestion.WindowsReferencer.OnOpenWindow.AddListener(window => WindowsReferencer.Add(window));
            m_ExportButton.onClick.AddListener(ExportSelected);
            ListGestion.List.OnSelect.AddListener((obj) => SetExport());
            ListGestion.List.OnDeselect.AddListener((obj) => SetExport());
        }
        protected void ExportSelected()
        {
            string directory = FileBrowser.GetExistingDirectoryName();
            if (string.IsNullOrEmpty(directory)) return;

            var selectedObjects = ListGestion.List.ObjectsSelected;
            foreach (var selectedObject in selectedObjects)
            {
                if (selectedObject is ILoadable<T> loadable && selectedObject is INameable nameable)
                {
                    ClassLoaderSaver.SaveToJSon(selectedObject, Path.Combine(directory, string.Format("{0}.{1}", nameable.Name, loadable.GetExtensions()[0])));
                }
            }
            ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Informational, "Selected items have been saved", string.Format("{0} items have been saved at {1}.", selectedObjects.Length, directory));
        }
        protected void SetExport()
        {
            var selectedObjects = ListGestion.List.ObjectsSelected;
            m_ExportButton.interactable = selectedObjects.Length > 0 && Interactable;
        }
        #endregion
    }
}