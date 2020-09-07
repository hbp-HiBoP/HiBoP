using CielaSpike;
using System.Collections;
using System.Collections.Generic;
using Tools.Unity;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.QuickStart
{
    public class FinalizationPanel : QuickStartPanel
    {
        #region Properties
        [SerializeField] private InputField m_ProjectName;
        [SerializeField] private FolderSelector m_ProjectLocation;
        #endregion

        #region Public Methods
        public override void ClosePanel()
        {
            base.ClosePanel();

            // Add visualization
            if (ApplicationState.ProjectLoaded.Protocols.Count == 0) // Anatomical
            {
                Data.Visualization.Visualization visualization = new Data.Visualization.Visualization("Anatomy", ApplicationState.ProjectLoaded.Patients, new Data.Visualization.Column[] { new Data.Visualization.AnatomicColumn("Anatomy", new Data.Visualization.BaseConfiguration()) });
                ApplicationState.ProjectLoaded.SetVisualizations(new Data.Visualization.Visualization[] { visualization });
            }
            else // Functional
            {

            }

            ApplicationState.ProjectLoaded.Preferences.Name = m_ProjectName.text;
            ApplicationState.ProjectLoadedLocation = m_ProjectLocation.Folder;
            GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
            ApplicationState.LoadingManager.Load(ApplicationState.ProjectLoaded.c_Save(ApplicationState.ProjectLoadedLocation, (progress, duration, text) => onChangeProgress.Invoke(progress, duration, text)), onChangeProgress, (state) =>
            {
                if (state == TaskState.Done)
                {
                    FindObjectOfType<MenuButtonState>().SetInteractables();
                    ApplicationState.Module3D.LoadScenes(ApplicationState.ProjectLoaded.Visualizations);
                }
            });
        }
        #endregion
    }
}