using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using HBP.Core.Data;
using HBP.Display.Module3D;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class VisualizationGestion : GestionWindow<Visualization>
    {
        #region Properties
        [SerializeField] Button m_DisplayButton;
        [SerializeField] VisualizationListGestion m_ListGestion;
        public override ListGestion<Visualization> ListGestion => m_ListGestion;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;
                SetDisplay();
            }
        }
        #endregion

        #region Public Methods
        public override void OK()
        {
            ApplicationState.ProjectLoaded.SetVisualizations(m_ListGestion.List.Objects);
            base.OK();
        }
        public void Display()
        {
            Visualization[] visualizations = m_ListGestion.List.ObjectsSelected;
            var alreadyOpenedVisualizations = visualizations.Where(v => Module3DMain.Scenes.Any(s => s.Visualization == v));
            if (alreadyOpenedVisualizations.Count() > 0)
            {
                DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Visualization already opened", "The following visualizations are already opened:\n" + string.Concat(alreadyOpenedVisualizations.Select(v => v.Name + "\n")));
                return;
            }
            if (ApplicationState.UserPreferences.Data.Anatomic.PreloadSinglePatientDataInMultiPatientVisualization)
            {
                int maxMemory = ApplicationState.UserPreferences.General.System.MemoryCacheLimit == 0 ? SystemInfo.systemMemorySize : ApplicationState.UserPreferences.General.System.MemoryCacheLimit;
                float patientThreshold = ((float)maxMemory / 400) - 3f; // raw approximation
                var maybeTooMuchMemoryVisualizations = visualizations.Where(v => v.Patients.Count > patientThreshold);
                if (maybeTooMuchMemoryVisualizations.Count() > 0)
                {
                    DialogBoxManager.Open(DialogBoxManager.AlertType.WarningMultiOptions, "Memory warning", "One of the visualizations you are trying to display has been detected as a potential memory issue.\nIt may contain too many patients in order to be visualized using the \"Preload all patient data in multi-patient visualizations\" option considering the maximum memory cache set in the user preferences.\n\nDo you still want to display it?",
                        () =>
                        {
                            Module3DMain.LoadScenes(m_ListGestion.List.ObjectsSelected);
                            OK();
                        }, "Display", () => { }, "Cancel");
                    return;
                }
            }
            Module3DMain.LoadScenes(m_ListGestion.List.ObjectsSelected);
            OK();
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();
            ListGestion.List.OnSelect.AddListener((visualization) => SetDisplay());
            ListGestion.List.OnDeselect.AddListener((visualization) => SetDisplay());
        }
        void SetDisplay()
        {
            Visualization[] visualizationsSelected = m_ListGestion.List.ObjectsSelected;
            m_DisplayButton.interactable = visualizationsSelected.Length > 0 && visualizationsSelected.All(v => v.IsVisualizable) && Interactable;
        }
        protected override void SetFields()
        {
            base.SetFields();
            m_ListGestion.List.Set(ApplicationState.ProjectLoaded.Visualizations);
            SetDisplay();
        }
        #endregion
    }
}