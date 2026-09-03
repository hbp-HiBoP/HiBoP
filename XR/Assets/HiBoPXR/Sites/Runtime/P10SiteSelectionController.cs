using System;
using CRNL.HiBoP.Contracts;
using UnityEngine;

namespace CRNL.HiBoP.XR.Sites
{
    public sealed class P10SiteSelectionController : MonoBehaviour
    {
        [SerializeField] private P10SiteRenderer siteRenderer;

        private int m_HoverIndex = -1;
        private int m_PendingIndex = -1;
        private int m_CanonicalIndex = -1;
        private SiteSelectionContext m_Context;
        private Command m_PendingCommand;

        public event Action<Command> SelectionRequested;

        public SitePickResult Hover { get; private set; } = SitePickResult.None;

        public ContractId PendingSiteId { get; private set; }

        public ContractId PendingCommandId => m_PendingCommand?.CommandId ?? default;

        public ContractId CanonicalSiteId { get; private set; }

        public SiteSelectionMetadata Metadata { get; private set; }

        public SiteSelectionContext Context => m_Context;

        public void Configure(P10SiteRenderer renderer) => siteRenderer = renderer;

        public void BindContext(SiteSelectionContext context)
        {
            if (!context.IsValid)
                throw new ArgumentException("A valid site selection context is required.", nameof(context));
            if (m_Context == context)
                return;
            ClearSelection();
            m_Context = context;
        }

        public bool UpdateRayHover(Ray ray, float maximumWorldDistanceMeters)
        {
            EnsureRenderer();
            siteRenderer.Raycast(ray, maximumWorldDistanceMeters, out SitePickResult result);
            ApplyHover(result);
            return result.Hit;
        }

        public bool UpdateProximityHover(Vector3 worldPoint)
        {
            EnsureRenderer();
            siteRenderer.FindNearest(worldPoint, out SitePickResult result);
            ApplyHover(result);
            return result.Hit;
        }

        public bool ConfirmHover(ContractId commandId, ContractId correlationId)
        {
            EnsureRenderer();
            if (!m_Context.IsValid)
                throw new InvalidOperationException("A canonical session and column context is required before selecting a site.");
            if (!commandId.IsValid)
                throw new ArgumentException("A valid command ID is required.", nameof(commandId));
            if (!correlationId.IsValid)
                throw new ArgumentException("A valid correlation ID is required.", nameof(correlationId));
            if (!Hover.Hit)
                return false;
            PendingSiteId = Hover.SiteId;
            m_PendingIndex = Hover.Index;
            m_PendingCommand = new Command(m_Context.Session, commandId, correlationId, m_Context.ColumnScope, m_Context.ScopeRevision, CommandKind.SelectSite, ContractValue.FromId(PendingSiteId));
            RefreshFeedback();
            SelectionRequested?.Invoke(m_PendingCommand);
            return true;
        }

        public bool ApplyOutcome(CommandOutcome outcome, SiteSelectionMetadata metadata = null)
        {
            if (outcome == null)
                throw new ArgumentNullException(nameof(outcome));
            if (m_PendingCommand == null || outcome.CommandId != m_PendingCommand.CommandId)
            {
                metadata?.Dispose();
                return false;
            }

            if (!outcome.Accepted)
            {
                metadata?.Dispose();
                ClearPending();
                return true;
            }

            if (!outcome.ResultingStateRevision.HasValue || !outcome.ResultingScopeRevision.HasValue)
                throw new ArgumentException("An accepted selection outcome must contain resulting revisions.", nameof(outcome));
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));
            if (metadata.Session != m_Context.Session || metadata.ColumnId != m_Context.ColumnId || metadata.SourceStateRevision != outcome.ResultingStateRevision.Value)
                throw new ArgumentException("Selection metadata must match the pending session, column and resulting state revision.", nameof(metadata));
            if (outcome.CanonicalValue.HasValue && (outcome.CanonicalValue.Value.Kind != ContractValueKind.Id || outcome.CanonicalValue.Value.Id != metadata.SiteId))
                throw new ArgumentException("The canonical outcome value must match the metadata site ID.", nameof(outcome));

            EnsureRenderer();
            ContractId siteId = metadata.SiteId;
            if (!siteId.IsValid)
                throw new ArgumentException("A valid canonical site ID is required.", nameof(siteId));
            if (!siteRenderer.TryGetIndex(siteId, out int index))
                throw new ArgumentException("The canonical site does not belong to the active SiteAsset.", nameof(siteId));

            Metadata?.Dispose();
            Metadata = metadata;
            CanonicalSiteId = siteId;
            m_CanonicalIndex = index;
            m_Context = new SiteSelectionContext(m_Context.Session, m_Context.ColumnId, m_Context.ColumnScope, outcome.ResultingStateRevision.Value, outcome.ResultingScopeRevision.Value);
            m_PendingCommand = null;
            PendingSiteId = default;
            m_PendingIndex = -1;
            RefreshFeedback();
            return true;
        }

        public void ClearSelection()
        {
            Metadata?.Dispose();
            Metadata = null;
            Hover = SitePickResult.None;
            PendingSiteId = default;
            CanonicalSiteId = default;
            m_PendingCommand = null;
            m_HoverIndex = -1;
            m_PendingIndex = -1;
            m_CanonicalIndex = -1;
            RefreshFeedback();
        }

        public void ClearScope()
        {
            ClearSelection();
            m_Context = default;
        }

        private void OnDestroy()
        {
            Metadata?.Dispose();
            Metadata = null;
        }

        private void ApplyHover(SitePickResult result)
        {
            Hover = result;
            m_HoverIndex = result.Hit ? result.Index : -1;
            RefreshFeedback();
        }

        private void RefreshFeedback() => siteRenderer?.SetFeedback(m_HoverIndex, m_PendingIndex, m_CanonicalIndex);

        private void ClearPending()
        {
            m_PendingCommand = null;
            PendingSiteId = default;
            m_PendingIndex = -1;
            RefreshFeedback();
        }

        private void EnsureRenderer()
        {
            if (siteRenderer == null)
                throw new InvalidOperationException("The P10 site renderer must be serialized in the prefab.");
        }
    }
}
