using System;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;
using UnityEngine;

namespace CRNL.HiBoP.XR.BrainInstances
{
    public sealed class BrainInstance
    {
        internal BrainInstance(ContractId instanceId, BrainInstanceBinding binding, BrainInstanceLayout layout, BrainInstanceView view, ResolvedBrainBinding resolved)
        {
            if (!instanceId.IsValid)
                throw new ArgumentException("A valid instance ID is required.", nameof(instanceId));
            InstanceId = instanceId;
            View = view ?? throw new ArgumentNullException(nameof(view));
            Binding = binding;
            Layout = layout;
            ApplyResolved(resolved);
            LocalRevision = 1;
        }

        public ContractId InstanceId { get; }

        public BrainInstanceBinding Binding { get; private set; }

        public BrainInstanceLayout Layout { get; private set; }

        public ulong LocalRevision { get; private set; }

        public ContractId ActiveColumnId { get; private set; }

        public AssetHash SurfaceHash { get; private set; }

        public SurfaceRepresentation Representation { get; private set; }

        public BrainInstanceView View { get; }

        internal void ApplyBinding(BrainInstanceBinding binding, ResolvedBrainBinding resolved)
        {
            Binding = binding;
            ApplyResolved(resolved);
            LocalRevision++;
        }

        internal void ApplyCanonical(ResolvedBrainBinding resolved)
        {
            ApplyResolved(resolved);
        }

        internal void ApplyLayout(BrainInstanceLayout layout)
        {
            Layout = layout;
            View.ApplyLayout(layout);
            LocalRevision++;
        }

        internal void DisposeView()
        {
            View.Dispose();
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(View.gameObject);
            else
                UnityEngine.Object.DestroyImmediate(View.gameObject);
        }

        private void ApplyResolved(ResolvedBrainBinding resolved)
        {
            SurfaceHash = resolved.SurfaceHash;
            Representation = resolved.Representation;
            ActiveColumnId = resolved.ActiveColumnId;
        }
    }
}
