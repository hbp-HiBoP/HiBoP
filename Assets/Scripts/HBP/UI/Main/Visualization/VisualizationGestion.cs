using UnityEngine;
using UnityEngine.UI;
using System.Linq;
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
        public void Display()
        {
            Visualization[] visualizations = m_ListGestion.List.ObjectsSelected;
            var alreadyOpenedVisualizations = visualizations.Where(v => Module3DMain.Scenes.Any(s => s.Visualization == v));
            if (alreadyOpenedVisualizations.Count() > 0)
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Visualization already opened", "The following visualizations are already opened:\n" + string.Concat(alreadyOpenedVisualizations.Select(v => v.Name + "\n"))).Forget();
                return;
            }
            DataManager.ConfigureMemoryBudget(PersistentDataManager.UserPreferences.General.System.MemoryCacheLimit, SystemInfo.systemMemorySize);
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
