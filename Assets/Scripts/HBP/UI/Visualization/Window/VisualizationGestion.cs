using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace HBP.UI.Visualization
{
    public class VisualizationGestion : SavableWindow
    {
        #region Properties
        [SerializeField] Button m_AddButton, m_RemoveButton, m_DisplayButton;
        [SerializeField] VisualizationListGestion m_VisualizationListGestion;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;

                m_AddButton.interactable = value;
                m_RemoveButton.interactable = value;
                m_VisualizationListGestion.Interactable = value;
                SetDisplay();
            }
        }
        #endregion

        #region Public Methods
        public override void Save()
        {
            ApplicationState.ProjectLoaded.SetVisualizations(m_VisualizationListGestion.Objects);
            base.Save();
        }
        public void Display()
        {
            ApplicationState.Module3D.LoadScenes(m_VisualizationListGestion.List.ObjectsSelected);
            Save();
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            m_VisualizationListGestion.Initialize(m_SubWindows);
            m_VisualizationListGestion.Objects = ApplicationState.ProjectLoaded.Visualizations.ToList();
            m_VisualizationListGestion.List.OnSelectionChanged.AddListener(() => SetDisplay());
            SetDisplay();

            base.Initialize();
        }
        void SetDisplay()
        {
            Data.Visualization.Visualization[] visualizationsSelected = m_VisualizationListGestion.List.ObjectsSelected;
            m_DisplayButton.interactable = visualizationsSelected.Length > 0 && visualizationsSelected.All(v => v.IsVisualizable) && Interactable;
        }   
        #endregion
    }
}