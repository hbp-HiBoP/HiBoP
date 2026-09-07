using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.Protocol;
using CRNL.HiBoP.RenderModel;

namespace HBP.RenderModelAdapters
{
    public sealed class DesktopCutComputation
    {
        public DesktopCutComputation(CutRenderResult result, PreloadedDynamicTimeline stablePlanTimeline = null)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
            StablePlanTimeline = stablePlanTimeline;
        }

        public CutRenderResult Result { get; }
        public PreloadedDynamicTimeline StablePlanTimeline { get; }
    }

    public sealed class DesktopCutComputationRequest
    {
        public DesktopCutComputationRequest(Command command, CutResultManifest manifest, CutResourceBudget budget, ISet<AssetHash> residentAssetHashes = null)
        {
            if (!CutCommands.TryRead(command, out _))
                throw new ArgumentException("A sequenced SetCut command is required.", nameof(command));
            Command = command;
            Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            Budget = budget;
            ResidentAssetHashes = residentAssetHashes == null ? null : new HashSet<AssetHash>(residentAssetHashes);
        }

        public Command Command { get; }
        public CutResultManifest Manifest { get; }
        public CutResourceBudget Budget { get; }
        public ISet<AssetHash> ResidentAssetHashes { get; }
    }

    /// <summary>
    /// Serializes access to the mutable Desktop scientific generators, coalesces intermediate
    /// poses per cut and returns only publications that are still latest after computation.
    /// Encoding and transfer must be performed only for a Completed outcome.
    /// </summary>
    public sealed class DesktopCutComputationScheduler : IDisposable
    {
        private readonly SemaphoreSlim m_ScientificGate = new(1, 1);
        private readonly object m_OrderGate = new();
        private readonly Func<DesktopCutComputationRequest, CancellationToken, Task<DesktopCutComputation>> m_Compute;
        private readonly LatestWinsScheduler<ScopeKey, DesktopCutComputationRequest, CutResultPublication> m_Scheduler;
        private readonly Dictionary<ScopeKey, CutOrder> m_CutOrders = new();
        private ulong m_DispatchSequence;
        private bool m_Disposed;

        public DesktopCutComputationScheduler(Func<DesktopCutComputationRequest, CancellationToken, Task<DesktopCutComputation>> compute)
        {
            m_Compute = compute ?? throw new ArgumentNullException(nameof(compute));
            m_Scheduler = new LatestWinsScheduler<ScopeKey, DesktopCutComputationRequest, CutResultPublication>(ComputeLatestAsync);
        }

        public long CompletedCount => m_Scheduler.CompletedCount;
        public long FailedCount => m_Scheduler.FailedCount;
        public long SupersededCount => m_Scheduler.SupersededCount;

        public Task<LatestWinsOutcome<CutResultPublication>> EnqueueAsync(DesktopCutComputationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(DesktopCutComputationScheduler));

            lock (m_OrderGate)
            {
                ContractId interactionId = request.Command.InteractionId.Value;
                ulong interactionSequence = request.Command.Sequence.Value.Value;
                if (!m_CutOrders.TryGetValue(request.Command.Scope, out CutOrder order))
                {
                    order = new CutOrder(interactionId, interactionSequence);
                    m_CutOrders.Add(request.Command.Scope, order);
                }
                else if (order.InteractionId == interactionId)
                {
                    if (interactionSequence <= order.InteractionSequence)
                        throw new ArgumentException("The cut command sequence must increase within its interaction.", nameof(request));
                    order.InteractionSequence = interactionSequence;
                }
                else
                {
                    if (order.PreviousInteractions.Contains(interactionId))
                        throw new ArgumentException("A command from a superseded cut interaction cannot be scheduled.", nameof(request));
                    order.PreviousInteractions.Add(order.InteractionId);
                    order.InteractionId = interactionId;
                    order.InteractionSequence = interactionSequence;
                }

                ulong dispatchSequence = checked(++m_DispatchSequence);
                return m_Scheduler.EnqueueAsync(request.Command.Scope, dispatchSequence, request);
            }
        }

        public bool ForgetCut(ScopeKey cutScope)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(DesktopCutComputationScheduler));
            lock (m_OrderGate)
            {
                if (!m_Scheduler.ForgetScope(cutScope))
                    return false;
                return m_CutOrders.Remove(cutScope);
            }
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_ScientificGate.Dispose();
        }

        private async Task<CutResultPublication> ComputeLatestAsync(DesktopCutComputationRequest request, CancellationToken cancellationToken)
        {
            await m_ScientificGate.WaitAsync(cancellationToken);
            try
            {
                DesktopCutComputation computation = await m_Compute(request, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                ValidateResult(request.Command, computation.Result);
                return new CutResultPublication(request.Command.Session, computation.Result, request.Manifest, computation.StablePlanTimeline, request.Budget, request.ResidentAssetHashes);
            }
            finally
            {
                m_ScientificGate.Release();
            }
        }

        private static void ValidateResult(Command command, CutRenderResult result)
        {
            if (result.CutId != command.Scope.Id || result.InteractionId != command.InteractionId.Value || result.Sequence != command.Sequence.Value)
                throw new InvalidOperationException("Desktop cut computation returned a result for a different cut interaction or sequence.");
            if (result.CutRevision < command.BaseScopeRevision)
                throw new InvalidOperationException("Desktop cut computation returned a regressing cut revision.");
        }

        private sealed class CutOrder
        {
            public CutOrder(ContractId interactionId, ulong interactionSequence)
            {
                InteractionId = interactionId;
                InteractionSequence = interactionSequence;
            }

            public ContractId InteractionId;
            public ulong InteractionSequence;
            public HashSet<ContractId> PreviousInteractions { get; } = new();
        }
    }
}
