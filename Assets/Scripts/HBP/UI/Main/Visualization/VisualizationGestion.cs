using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using HBP.Core.Data;
using HBP.Data.Module3D;
using HBP.UI.Tools;
using HBP.Core.Tools;
using HBP.Data.Preferences;

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
                m_ListGestion.Modifiable = value;
                SetDisplay();
            }
        }
        #endregion

        #region Public Methods
        public override void OK()
        {
            ApplicationState.LoadedProject.SetVisualizations(m_ListGestion.List.Objects);
            base.OK();
            UITools.CheckProjectIDAndAskForRegeneration().Forget();
        }
        public async void Display()
        {
            Visualization[] visualizations = m_ListGestion.List.ObjectsSelected;
            var alreadyOpenedVisualizations = visualizations.Where(v => Module3DMain.Scenes.Any(s => s.Visualization == v));
            if (alreadyOpenedVisualizations.Count() > 0)
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Visualization already opened", "The following visualizations are already opened:\n" + string.Concat(alreadyOpenedVisualizations.Select(v => v.Name + "\n"))).Forget();
                return;
            }
            if (PersistentDataManager.UserPreferences.Data.Anatomic.PreloadSinglePatientDataInMultiPatientVisualization)
            {
                int maxMemory = PersistentDataManager.UserPreferences.General.System.MemoryCacheLimit == 0 ? SystemInfo.systemMemorySize : PersistentDataManager.UserPreferences.General.System.MemoryCacheLimit;
                float patientThreshold = ((float)maxMemory / 400) - 3f; // raw approximation
                var maybeTooMuchMemoryVisualizations = visualizations.Where(v => v.Patients.Count > patientThreshold);
                if (maybeTooMuchMemoryVisualizations.Count() > 0)
                {
                    int result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Warning, "Memory warning", "One of the visualizations you are trying to display has been detected as a potential memory issue.\nIt may contain too many patients in order to be visualized using the \"Preload all patient data in multi-patient visualizations\" option considering the maximum memory cache set in the user preferences.\n\nDo you still want to display it?", "Display", "Cancel");
                    if (result == 1) return;
                }
            }
            LoadingManager.Load((update, token) => Module3DMain.LoadAsync(m_ListGestion.List.ObjectsSelected, update, token));
            OK();
        }
        public override void Close()
        {
            if (m_ListGestion.HasBeenModified)
                LoadingManager.Load(update => RestoreOldValuesAsync(ApplicationState.LoadedProject.Visualizations, update), false);
            base.Close();
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();
            m_ListGestion.List.OnSelect.AddListener((visualization) => SetDisplay());
            m_ListGestion.List.OnDeselect.AddListener((visualization) => SetDisplay());
            m_ListGestion.List.OnRemoveObject.AddListener((visualization) => SetDisplay());
            m_ListGestion.List.OnAddObject.AddListener((visualization) => SetDisplay());
        }
        void SetDisplay()
        {
            Visualization[] visualizationsSelected = m_ListGestion.List.ObjectsSelected;
            m_DisplayButton.interactable = visualizationsSelected.Length > 0 && visualizationsSelected.All(v => v.IsVisualizable) && Interactable;
        }
        protected override void SetFields()
        {
            base.SetFields();
            SetList(ApplicationState.LoadedProject.Visualizations);
            SetDisplay();
        }
        #endregion
    }
}
