using Cysharp.Threading.Tasks;
using HBP.Core.Tools;
using HBP.UI.Tools;
using System;
using System.Linq;
using System.Threading;

namespace HBP.UI.Module3D
{
    public class SiteFiltersWindow : ListFilter
    {
        #region Public Methods

        public override void ResetFilters()
        {
            // Update the filtering objects to the current selected sites in the scene to prevent issues when changing the selected scene
            FilteringObjects = Data.Module3D.Module3DMain.SelectedScene.Columns.SelectMany(c => c.Sites).Where(s => !s.State.IsMasked).Select(s => (object)s).ToList();
            base.ResetFilters();
        }

        #endregion

        #region Private Methods

        protected override void Initialize()
        {
            base.Initialize();

            Data.Module3D.Module3DMain.OnSelectScene.AddSafeListener(s => SetButtonsState(), gameObject);
            Data.Module3D.Module3DMain.OnDeselectScene.AddSafeListener(s => SetButtonsState(), gameObject);
        }

        protected override async UniTask ApplyFiltersAsync(Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            // Update the filtering objects to the current selected sites in the scene to prevent issues when changing the selected scene
            FilteringObjects = Data.Module3D.Module3DMain.SelectedScene.Columns.SelectMany(c => c.Sites).Where(s => !s.State.IsMasked).Select(s => (object)s).ToList();
            await base.ApplyFiltersAsync(updateProgress, token);
        }

        protected override void SetButtonsState()
        {
            m_ApplyButton.interactable = m_ListGestion.List.ObjectsSelected.Length > 0 && Data.Module3D.Module3DMain.SelectedScene != null;
            m_ResetButton.interactable = Data.Module3D.Module3DMain.SelectedScene != null;
        }

        #endregion
    }
}
