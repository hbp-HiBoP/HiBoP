using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace HBP.UI.Visualization
{
    public class VisualizationGestion : ItemGestion<Data.Visualization.Visualization>
    {
        #region Properties
        [SerializeField] Button m_AddButton, m_RemoveButton, m_DisplayButton;
        [SerializeField] VisualizationList m_VisualizationList;
        [SerializeField] Text m_CounterText;
        #endregion

        #region Public Methods
        public override void Save()
        {
            ApplicationState.ProjectLoaded.SetVisualizations(Items.ToArray());
            base.Save();
        }
        public void Display()
        {
            ApplicationState.Module3D.LoadScenes(m_List.ObjectsSelected);
            base.Close();
        }
        #endregion

        #region Private Methods
        protected override void SetInteractable(bool interactable)
        {
            base.SetInteractable(interactable);
            m_AddButton.interactable = interactable;
            m_RemoveButton.interactable = interactable;
            m_VisualizationList.Interactable = interactable;
        }
        protected override void Initialize()
        {
            base.Initialize();
            m_List = m_VisualizationList;
            m_VisualizationList.Initialize();
            m_VisualizationList.OnAction.AddListener((visu, type) => OpenModifier(visu,true));
            m_VisualizationList.OnSelectionChanged.AddListener(() => SetDisplay());
            m_VisualizationList.OnSelectionChanged.AddListener(() => m_CounterText.text = m_VisualizationList.ObjectsSelected.Length.ToString());
            AddItem(ApplicationState.ProjectLoaded.Visualizations.ToArray());
            SetDisplay();
        }
        void SetDisplay()
        {
            Data.Visualization.Visualization[] visualizationsSelected = m_List.ObjectsSelected;
            m_DisplayButton.interactable = (visualizationsSelected.Length > 0 && visualizationsSelected.All(v => v.IsVisualizable));
        }   
        #endregion
    }
}