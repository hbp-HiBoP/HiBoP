using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Tools.Unity.Components;

namespace HBP.UI.Visualization
{
    public class VisualizationGestion : GestionWindow<Data.Visualization.Visualization>
    {
        #region Properties
        [SerializeField] Button m_DisplayButton;
        [SerializeField] VisualizationListGestion m_ListGestion;
        public override ListGestion<Data.Visualization.Visualization> ListGestion => m_ListGestion;

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
            ApplicationState.Module3D.LoadScenes(m_ListGestion.List.ObjectsSelected);
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
            Data.Visualization.Visualization[] visualizationsSelected = m_ListGestion.List.ObjectsSelected;
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