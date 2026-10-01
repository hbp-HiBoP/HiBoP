using Cysharp.Threading.Tasks;
using HBP.Core.Tools;
using HBP.Core.Data;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using HBP.Sync.Scene;
using HBP.UI.Tools;
using System;
using System.Linq;
using System.Threading;

namespace HBP.UI.Module3D
{
    public class SiteFiltersWindow : ListFilter
    {
        private Base3DScene m_Scene;
        private bool m_RoutedRequestRunning;

        public Base3DScene Scene
        {
            get => m_Scene;
            set
            {
                m_Scene = value;
                UpdateFilteringObjects();
                SetButtonsState();
            }
        }

        #region Public Methods

        public override void ApplyFilters()
        {
            UpdateFilteringObjects();
            BaseFilterCondition[] conditions = m_ListGestion.List.ObjectsSelected;
            if (conditions.Length == 0 || !TryGetRequestHandler(out Func<V2SiteFilterRequest, CancellationToken, System.Threading.Tasks.Task<bool>> handler))
            {
                base.ApplyFilters();
                return;
            }

            RunRoutedRequestAsync(V2SiteFilterRequest.FromConditions(conditions, externalLoadingIndicator: true), handler, ApplyFiltersLocallyAsync).Forget();
        }

        public override void ResetFilters()
        {
            UpdateFilteringObjects();
            if (TryGetRequestHandler(out Func<V2SiteFilterRequest, CancellationToken, System.Threading.Tasks.Task<bool>> handler))
            {
                RunRoutedRequestAsync(V2SiteFilterRequest.ResetUnmasked(externalLoadingIndicator: true), handler, (_, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    ResetFiltersLocally();
                    return UniTask.CompletedTask;
                }).Forget();
                return;
            }

            ResetFiltersLocally();
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
            UpdateFilteringObjects();
            await base.ApplyFiltersAsync(updateProgress, token);
        }

        protected override void SetButtonsState()
        {
            m_ApplyButton.interactable = !m_RoutedRequestRunning && m_ListGestion.List.ObjectsSelected.Length > 0 && m_Scene != null;
            m_ResetButton.interactable = !m_RoutedRequestRunning && m_Scene != null;
        }

        private void UpdateFilteringObjects()
        {
            if (m_Scene == null) return;
            FilteringObjects = m_Scene.Columns.SelectMany(column => column.Sites).Where(site => !site.State.IsMasked).Select(site => (object)site).ToList();
        }

        private bool TryGetRequestHandler(out Func<V2SiteFilterRequest, CancellationToken, System.Threading.Tasks.Task<bool>> handler)
        {
            handler = null;
            return m_Scene != null && V2SiteFilterRequestRouter.TryGetHandler(m_Scene, out handler);
        }

        private UniTask ApplyFiltersLocallyAsync(Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            return base.ApplyFiltersAsync(updateProgress, token);
        }

        private void ResetFiltersLocally()
        {
            base.ResetFilters();
        }

        private async UniTask RunRoutedRequestAsync(V2SiteFilterRequest request, Func<V2SiteFilterRequest, CancellationToken, System.Threading.Tasks.Task<bool>> handler, Func<Action<float, float, LoadingText>, CancellationToken, UniTask> fallback)
        {
            m_RoutedRequestRunning = true;
            SetButtonsState();
            try
            {
                await LoadingManager.LoadDelayedAsync(async (update, token) =>
                {
                    bool handled = await handler(request, token);
                    if (!handled) await fallback(update, token);
                }, CancellationToken.None, showInformations: false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
                // LoadingManager already displayed the failure dialog.
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                m_RoutedRequestRunning = false;
                SetButtonsState();
            }
        }

        #endregion
    }
}
