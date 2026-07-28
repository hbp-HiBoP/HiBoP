using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Data.Module3D;
using HBP.UI.Tools;
using HBP.Core.Tools;
using HBP.Core.Preferences;

namespace HBP.UI.Main
{
    public class VisualizationGestion : GestionWindow<Visualization>
    {
        #region Properties

        [SerializeField] Button m_DisplayButton;
        [SerializeField] VisualizationListGestion m_ListGestion;
        private Project m_ObservedProject;
        public override ListGestion<Visualization> ListGestion => m_ListGestion;

        public override bool Interactable
        {
            get { return base.Interactable; }

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

            DataManager.ConfigureMemoryBudget(PersistentDataManager.UserPreferences.General.System.MemoryCacheLimit, SystemInfo.systemMemorySize);
            UniTask loading = LoadingManager.LoadAsync((update, token) => Module3DMain.LoadAsync(visualizations, update, token));
            OK();
            await loading;
            await UniTask.SwitchToMainThread();
            UITools.ShowMemoryCacheBudgetWarningIfNeeded();
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
            bool validationPending = ApplicationState.LoadedProject?.NeedsValidationWait ?? false;
            m_DisplayButton.interactable = visualizationsSelected.Length > 0 && (validationPending || visualizationsSelected.All(visualization => visualization.IsVisualizable)) && Interactable;
        }

        protected override void SetFields()
        {
            base.SetFields();
            ObserveProjectValidation();
            SetList(ApplicationState.LoadedProject.Visualizations);
            SetDisplay();
        }

        private void ObserveProjectValidation()
        {
            Project project = ApplicationState.LoadedProject;
            if (m_ObservedProject == project)
            {
                return;
            }

            if (m_ObservedProject != null)
            {
                m_ObservedProject.OnValidationStateChanged -= SetDisplay;
            }

            m_ObservedProject = project;
            if (m_ObservedProject != null)
            {
                m_ObservedProject.OnValidationStateChanged += SetDisplay;
            }
        }

        private void OnDestroy()
        {
            if (m_ObservedProject != null)
            {
                m_ObservedProject.OnValidationStateChanged -= SetDisplay;
            }
        }

        #endregion
    }
}
