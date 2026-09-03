using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.Protocol;
using CRNL.HiBoP.RenderModel;
using CRNL.HiBoP.XR.RemoteAssets;
using CRNL.HiBoP.XR.StaticRendering;
using CRNL.HiBoP.XR.Sites;
using UnityEngine;
using UnityEngine.Profiling;

namespace CRNL.HiBoP.XR.BrainInstances
{
    public sealed class BrainInstanceRegistry : IDisposable
    {
        private readonly InMemoryRemoteAssetCache m_Cache;
        private readonly Dictionary<ContractId, BrainInstance> m_Instances = new();
        private readonly Dictionary<ContractId, Action<Command>> m_SiteSelectionHandlers = new();
        private readonly BrainInstanceView m_Prefab;
        private readonly RemoteSurfaceAssetStore m_SurfaceStore;
        private readonly Transform m_ViewParent;
        private BrainCanonicalState m_CanonicalState;
        private bool m_Closed;

        public BrainInstanceRegistry(BrainInstanceView prefab, Transform viewParent, InMemoryRemoteAssetCache cache, RemoteSurfaceAssetStore surfaceStore)
        {
            m_Prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));
            m_ViewParent = viewParent;
            m_Cache = cache ?? throw new ArgumentNullException(nameof(cache));
            m_SurfaceStore = surfaceStore ?? throw new ArgumentNullException(nameof(surfaceStore));
        }

        public int Count => m_Instances.Count;

        public event Action<ContractId, Command> SiteSelectionRequested;

        public IReadOnlyCollection<BrainInstance> Instances => new ReadOnlyCollection<BrainInstance>(new List<BrainInstance>(m_Instances.Values));

        public BrainReconciliationResult Reconcile(SessionSnapshot snapshot)
        {
            EnsureOpen();
            BrainCanonicalState next = BrainCanonicalState.FromSnapshot(snapshot);
            var closed = new List<ClosedBrainInstance>();
            var awaitingAssets = new List<ContractId>();
            bool epochChanged = m_CanonicalState != null && m_CanonicalState.Session != next.Session;
            if (epochChanged)
            {
                m_Cache.ApplyLifecycle(AssetCacheLifecycleEvent.NewEpoch);
                CloseAll(BrainInstanceCloseReason.NewEpoch, closed);
                m_CanonicalState = next;
                return new BrainReconciliationResult(closed, awaitingAssets, true);
            }

            m_CanonicalState = next;
            foreach (BrainInstance instance in new List<BrainInstance>(m_Instances.Values))
            {
                if (!next.TryResolve(instance.Binding, out ResolvedBrainBinding resolved))
                {
                    BrainInstanceCloseReason reason = next.GetInvalidationReason(instance.Binding);
                    Close(instance.InstanceId, reason, closed);
                    continue;
                }

                bool renderingChanged = instance.SurfaceHash != resolved.SurfaceHash || instance.Representation != resolved.Representation || instance.View.ExpectedSurfaceDrawCalls != ExpectedDrawCalls(resolved);
                if (renderingChanged && !instance.View.TryActivate(resolved))
                {
                    awaitingAssets.Add(instance.InstanceId);
                    continue;
                }

                instance.ApplyCanonical(resolved);
                BindSiteSelectionContext(instance.View, resolved);
                instance.View.ApplyLayout(instance.Layout);
            }

            return new BrainReconciliationResult(closed, awaitingAssets, false);
        }

        public bool TryCreate(ContractId instanceId, BrainInstanceBinding binding, BrainInstanceLayout layout, out BrainInstance instance)
        {
            EnsureOpen();
            if (!instanceId.IsValid)
                throw new ArgumentException("A valid instance ID is required.", nameof(instanceId));
            if (m_Instances.ContainsKey(instanceId))
                throw new ArgumentException("The instance ID is already registered.", nameof(instanceId));
            if (m_CanonicalState == null)
                throw new InvalidOperationException("A canonical snapshot is required before creating an instance.");
            if (!m_CanonicalState.TryResolve(binding, out ResolvedBrainBinding resolved))
            {
                instance = null;
                return false;
            }

            BrainInstanceView view = UnityEngine.Object.Instantiate(m_Prefab, m_ViewParent, false);
            view.name = $"BrainInstance-{instanceId}";
            try
            {
                view.Initialize(m_SurfaceStore);
                if (!view.TryActivate(resolved))
                {
                    DestroyView(view);
                    instance = null;
                    return false;
                }

                view.ApplyLayout(layout);
                BindSiteSelectionContext(view, resolved);
                instance = new BrainInstance(instanceId, binding, layout, view, resolved);
                m_Instances.Add(instanceId, instance);
                SubscribeSiteSelection(instance);
                return true;
            }
            catch
            {
                DestroyView(view);
                throw;
            }
        }

        public bool TryRebind(ContractId instanceId, BrainInstanceBinding binding)
        {
            EnsureOpen();
            if (!m_Instances.TryGetValue(instanceId, out BrainInstance instance))
                return false;
            if (!m_CanonicalState.TryResolve(binding, out ResolvedBrainBinding resolved))
                return false;
            if (!instance.View.TryActivate(resolved))
                return false;
            BindSiteSelectionContext(instance.View, resolved);
            instance.ApplyBinding(binding, resolved);
            return true;
        }

        public bool TrySetLayout(ContractId instanceId, BrainInstanceLayout layout)
        {
            EnsureOpen();
            if (!m_Instances.TryGetValue(instanceId, out BrainInstance instance))
                return false;
            instance.ApplyLayout(layout);
            return true;
        }

        public bool TryApplySiteFrame(ContractId instanceId, SiteAsset asset, SiteRenderFrame frame, IReadOnlyList<SiteDirtyRange> dirtyRanges = null)
        {
            EnsureOpen();
            if (!m_Instances.TryGetValue(instanceId, out BrainInstance instance))
                return false;
            if (!m_CanonicalState.TryGetSiteSelectionContext(instance.ActiveColumnId, out SiteSelectionContext context))
                return false;
            instance.View.ApplySites(asset, frame, context, dirtyRanges);
            return true;
        }

        public bool TryUpdateSiteRayHover(ContractId instanceId, Ray ray, float maximumWorldDistanceMeters, out SitePickResult result)
        {
            EnsureOpen();
            if (!m_Instances.TryGetValue(instanceId, out BrainInstance instance) || instance.View.SiteSelection == null)
            {
                result = SitePickResult.None;
                return false;
            }

            instance.View.SiteSelection.UpdateRayHover(ray, maximumWorldDistanceMeters);
            result = instance.View.SiteSelection.Hover;
            return result.Hit;
        }

        public bool TryUpdateSiteProximityHover(ContractId instanceId, Vector3 worldPoint, out SitePickResult result)
        {
            EnsureOpen();
            if (!m_Instances.TryGetValue(instanceId, out BrainInstance instance) || instance.View.SiteSelection == null)
            {
                result = SitePickResult.None;
                return false;
            }

            instance.View.SiteSelection.UpdateProximityHover(worldPoint);
            result = instance.View.SiteSelection.Hover;
            return result.Hit;
        }

        public bool TryConfirmSiteHover(ContractId instanceId, ContractId commandId, ContractId correlationId)
        {
            EnsureOpen();
            return m_Instances.TryGetValue(instanceId, out BrainInstance instance) && instance.View.SiteSelection != null && instance.View.ConfirmSiteHover(commandId, correlationId);
        }

        public bool TryApplySiteSelectionOutcome(ContractId instanceId, CommandOutcome outcome, SiteSelectionMetadata metadata = null)
        {
            EnsureOpen();
            if (!m_Instances.TryGetValue(instanceId, out BrainInstance instance) || instance.View.SiteSelection == null)
            {
                metadata?.Dispose();
                return false;
            }

            return instance.View.ApplySiteSelectionOutcome(outcome, metadata);
        }

        public bool TrySetPose(ContractId instanceId, Vector3 localPosition, Quaternion localRotation)
        {
            if (!m_Instances.TryGetValue(instanceId, out BrainInstance instance))
                return false;
            BrainInstanceLayout current = instance.Layout;
            return TrySetLayout(instanceId, new BrainInstanceLayout(localPosition, localRotation, current.UniformScale, current.Visible));
        }

        public bool TrySetScale(ContractId instanceId, float uniformScale)
        {
            if (!m_Instances.TryGetValue(instanceId, out BrainInstance instance))
                return false;
            BrainInstanceLayout current = instance.Layout;
            return TrySetLayout(instanceId, new BrainInstanceLayout(current.LocalPosition, current.LocalRotation, uniformScale, current.Visible));
        }

        public bool TryRecenter(ContractId instanceId, Vector3 localPosition, Quaternion localRotation)
        {
            return TrySetPose(instanceId, localPosition, localRotation);
        }

        public bool TryClose(ContractId instanceId, out ClosedBrainInstance closed)
        {
            EnsureOpen();
            if (!m_Instances.ContainsKey(instanceId))
            {
                closed = null;
                return false;
            }

            var results = new List<ClosedBrainInstance>();
            Close(instanceId, BrainInstanceCloseReason.Requested, results);
            closed = results[0];
            return true;
        }

        public BrainInstanceMetrics CaptureMetrics()
        {
            HashSet<AssetHash> hashes = new();
            HashSet<Mesh> meshes = new();
            int rendererCount = 0;
            int drawCalls = 0;
            foreach (BrainInstance instance in m_Instances.Values)
            {
                if (instance.View.SurfaceAsset != null)
                {
                    rendererCount++;
                    hashes.Add(instance.View.SurfaceHash);
                }

                if (instance.View.SharedMesh != null)
                    meshes.Add(instance.View.SharedMesh);
                drawCalls += instance.View.ExpectedDrawCalls;
            }

            long meshBytes = 0;
            foreach (Mesh mesh in meshes)
                meshBytes += Profiler.GetRuntimeMemorySizeLong(mesh);
            return new BrainInstanceMetrics(m_Instances.Count, rendererCount, hashes.Count, meshes.Count, drawCalls, m_Cache.ResidentBytes, meshBytes);
        }

        public IReadOnlyList<ClosedBrainInstance> CloseSession()
        {
            if (m_Closed)
                return Array.Empty<ClosedBrainInstance>();
            m_Cache.ApplyLifecycle(AssetCacheLifecycleEvent.Closed);
            var closed = new List<ClosedBrainInstance>();
            CloseAll(BrainInstanceCloseReason.SessionClosed, closed);
            m_CanonicalState = null;
            m_Closed = true;
            SiteSelectionRequested = null;
            return new ReadOnlyCollection<ClosedBrainInstance>(closed);
        }

        public void Dispose()
        {
            CloseSession();
        }

        private static int ExpectedDrawCalls(ResolvedBrainBinding resolved)
        {
            return resolved.Transparency == SurfaceTransparency.Transparent ? 2 : 1;
        }

        private void BindSiteSelectionContext(BrainInstanceView view, ResolvedBrainBinding resolved)
        {
            if (view.SiteSelection == null)
                return;
            if (m_CanonicalState.TryGetSiteSelectionContext(resolved.ActiveColumnId, out SiteSelectionContext context))
                view.BindSiteSelectionContext(context);
            else
                view.ClearSites();
        }

        private void SubscribeSiteSelection(BrainInstance instance)
        {
            if (instance.View.SiteSelection == null)
                return;
            Action<Command> handler = command => SiteSelectionRequested?.Invoke(instance.InstanceId, command);
            m_SiteSelectionHandlers.Add(instance.InstanceId, handler);
            instance.View.SiteSelectionRequested += handler;
        }

        private void UnsubscribeSiteSelection(BrainInstance instance)
        {
            if (!m_SiteSelectionHandlers.Remove(instance.InstanceId, out Action<Command> handler))
                return;
            instance.View.SiteSelectionRequested -= handler;
        }

        private void CloseAll(BrainInstanceCloseReason reason, ICollection<ClosedBrainInstance> closed)
        {
            foreach (ContractId instanceId in new List<ContractId>(m_Instances.Keys))
                Close(instanceId, reason, closed);
        }

        private void Close(ContractId instanceId, BrainInstanceCloseReason reason, ICollection<ClosedBrainInstance> closed)
        {
            BrainInstance instance = m_Instances[instanceId];
            m_Instances.Remove(instanceId);
            UnsubscribeSiteSelection(instance);
            instance.DisposeView();
            closed.Add(new ClosedBrainInstance(instanceId, reason));
        }

        private static void DestroyView(BrainInstanceView view)
        {
            if (view == null)
                return;
            view.Dispose();
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(view.gameObject);
            else
                UnityEngine.Object.DestroyImmediate(view.gameObject);
        }

        private void EnsureOpen()
        {
            if (m_Closed)
                throw new ObjectDisposedException(nameof(BrainInstanceRegistry));
        }
    }
}
