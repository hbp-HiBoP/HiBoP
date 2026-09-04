using System;
using System.Collections.Generic;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;
using CRNL.HiBoP.XR.RemoteAssets;
using CRNL.HiBoP.XR.StaticRendering;
using CRNL.HiBoP.XR.Sites;
using UnityEngine;

namespace CRNL.HiBoP.XR.BrainInstances
{
    public sealed class BrainInstanceView : MonoBehaviour, IDisposable
    {
        [SerializeField] private P05StaticSurfaceRenderer surfaceRenderer;
        [SerializeField] private P10SiteRenderer siteRenderer;
        [SerializeField] private P10SiteSelectionController siteSelection;

        private RemoteSurfaceRendererBinding m_SurfaceBinding;
        private int m_SurfaceExpectedDrawCalls;
        private bool m_SiteSelectionSubscribed;
        private event Action<Command> m_SiteSelectionRequested;

        public event Action<Command> SiteSelectionRequested
        {
            add
            {
                SubscribeSiteSelection();
                m_SiteSelectionRequested += value;
            }
            remove => m_SiteSelectionRequested -= value;
        }

        public SurfaceAsset SurfaceAsset => m_SurfaceBinding?.ActiveSurfaceAsset;

        public AssetHash SurfaceHash => m_SurfaceBinding?.ActiveHash ?? default;

        public Mesh SharedMesh => surfaceRenderer == null ? null : surfaceRenderer.SharedMesh;

        public P10SiteRenderer SiteRenderer => siteRenderer;

        public P10SiteSelectionController SiteSelection => siteSelection;

        internal int ExpectedSurfaceDrawCalls => m_SurfaceExpectedDrawCalls;

        public int ExpectedDrawCalls => m_SurfaceExpectedDrawCalls + (siteRenderer == null ? 0 : siteRenderer.ExpectedDrawCalls);

        public void Configure(P05StaticSurfaceRenderer renderer)
        {
            surfaceRenderer = renderer;
        }

        public void ConfigureSites(P10SiteRenderer renderer, P10SiteSelectionController selection)
        {
            UnsubscribeSiteSelection();
            siteRenderer = renderer;
            siteSelection = selection;
            SubscribeSiteSelection();
        }

        internal void Initialize(RemoteSurfaceAssetStore store)
        {
            if (m_SurfaceBinding != null)
                throw new InvalidOperationException("The BrainInstance view is already initialized.");
            if (surfaceRenderer == null)
                throw new InvalidOperationException("The BrainInstance prefab is missing its serialized P05 renderer.");
            if ((siteRenderer == null) != (siteSelection == null))
                throw new InvalidOperationException("The optional P10 renderer and selection controller must both be serialized.");
            m_SurfaceBinding = new RemoteSurfaceRendererBinding(store, surfaceRenderer);
            SubscribeSiteSelection();
        }

        internal bool TryActivate(ResolvedBrainBinding resolved)
        {
            if (m_SurfaceBinding == null)
                throw new InvalidOperationException("The BrainInstance view is not initialized.");
            if (!m_SurfaceBinding.TryActivate(resolved.SurfaceHash, resolved.Transparency, resolved.Representation))
                return false;
            m_SurfaceExpectedDrawCalls = resolved.Transparency == SurfaceTransparency.Transparent ? 2 : 1;
            return true;
        }

        public void BindSiteSelectionContext(SiteSelectionContext context)
        {
            EnsureSiteComponents();
            SiteSelectionContext previous = siteSelection.Context;
            if (previous.IsValid && (previous.Session != context.Session || previous.ColumnId != context.ColumnId))
                siteRenderer.Clear();
            siteSelection.BindContext(context);
        }

        public void ApplySites(SiteAsset asset, SiteRenderFrame frame, SiteSelectionContext context, IReadOnlyList<SiteDirtyRange> dirtyRanges = null)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            EnsureSiteComponents();
            BindSiteSelectionContext(context);
            if (!ReferenceEquals(siteRenderer.SiteAsset, asset))
            {
                siteSelection.ClearSelection();
                siteRenderer.SetAsset(asset);
            }

            siteRenderer.ApplyFrame(frame, dirtyRanges);
        }

        public bool ConfirmSiteHover(ContractId commandId, ContractId correlationId)
        {
            EnsureSiteComponents();
            return siteSelection.ConfirmHover(commandId, correlationId);
        }

        public bool ApplySiteSelectionOutcome(CommandOutcome outcome, SiteSelectionMetadata metadata = null)
        {
            EnsureSiteComponents();
            return siteSelection.ApplyOutcome(outcome, metadata);
        }

        public void ClearSites()
        {
            siteSelection?.ClearScope();
            siteRenderer?.Clear();
        }

        internal void ApplyLayout(BrainInstanceLayout layout)
        {
            transform.localPosition = layout.LocalPosition;
            transform.localRotation = layout.LocalRotation;
            transform.localScale = Vector3.one * layout.UniformScale;
            gameObject.SetActive(layout.Visible);
        }

        public void Dispose()
        {
            UnsubscribeSiteSelection();
            ClearSites();
            m_SurfaceBinding?.Dispose();
            m_SurfaceBinding = null;
            m_SurfaceExpectedDrawCalls = 0;
        }

        private void Awake() => SubscribeSiteSelection();

        private void OnDestroy()
        {
            Dispose();
        }

        private void ForwardSiteSelectionRequest(Command command) => m_SiteSelectionRequested?.Invoke(command);

        private void SubscribeSiteSelection()
        {
            if (siteSelection == null || m_SiteSelectionSubscribed)
                return;
            siteSelection.SelectionRequested += ForwardSiteSelectionRequest;
            m_SiteSelectionSubscribed = true;
        }

        private void UnsubscribeSiteSelection()
        {
            if (siteSelection == null || !m_SiteSelectionSubscribed)
                return;
            siteSelection.SelectionRequested -= ForwardSiteSelectionRequest;
            m_SiteSelectionSubscribed = false;
        }

        private void EnsureSiteComponents()
        {
            if (siteRenderer == null || siteSelection == null)
                throw new InvalidOperationException("The BrainInstance prefab is missing its serialized P10 site set.");
        }
    }
}
