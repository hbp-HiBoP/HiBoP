using System;
using System.Runtime.InteropServices;
using HBP.Transfer.Anatomy;
using UnityEngine;
using UnityEngine.Rendering;

namespace HBP.Quest.Legacy
{
    /// <summary>Prepared appearance only. One instanced draw in the surface's millimeter frame.</summary>
    // QuestAnatomyInput updates the group in default-order LateUpdate. Capture its final pose.
    [DefaultExecutionOrder(100)]
    public sealed class QuestContactRenderer : MonoBehaviour
    {
        private static readonly int SitesId = Shader.PropertyToID("_Contacts");
        private static readonly int LocalToWorldId = Shader.PropertyToID("_ContactLocalToWorld");
        [SerializeField] private Mesh siteMesh;
        [SerializeField] private Material siteMaterial;
        private Frame current;

        public int SiteCount => current?.Count ?? 0;
        public int VisibleSiteCount => current?.VisibleCount ?? 0;
        public long BufferBytes => (long)SiteCount * Site.Stride;
        public Bounds LocalBounds => current?.Bounds ?? new Bounds();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal Site[] ReadDiagnosticSites()
        {
            var data = new Site[SiteCount];
            current?.Buffer?.GetData(data);
            return data;
        }
#endif

        [StructLayout(LayoutKind.Sequential)]
        internal struct Site
        {
            public const int Stride = 32;
            public Vector4 PositionRadius;
            public Vector4 Color;
        }

        internal sealed class Frame : IDisposable
        {
            public GraphicsBuffer Buffer;
            public MaterialPropertyBlock Properties;
            public Bounds Bounds;
            public int Count, VisibleCount;

            public void Dispose()
            {
                Buffer?.Dispose();
                Buffer = null;
            }
        }

        internal Frame Prepare(AnatomyContacts contacts)
        {
            if (siteMesh == null || siteMaterial == null || !siteMaterial.enableInstancing)
                throw new InvalidOperationException("Contact mesh and instanced material must be serialized in QuestAnatomy.prefab.");
            var next = new Frame { Count = contacts.Sites.Count };
            if (next.Count == 0) return next;
            try
            {
                var sites = new Site[next.Count];
                for (int i = 0; i < sites.Length; i++)
                {
                    AnatomySite source = contacts.Sites[i];
                    var position = new Vector3(source.Position[0], source.Position[1], source.Position[2]);
                    // Zero radius discards invisible instances; the immutable snapshot keeps their diameter.
                    float radius = source.Visible ? source.Diameter * 0.5f : 0;
                    sites[i] = new Site
                    {
                        PositionRadius = new Vector4(position.x, position.y, position.z, radius),
                        Color = new Vector4(source.Color[0], source.Color[1], source.Color[2], source.Color[3])
                    };
                    if (source.Visible) next.VisibleCount++;
                    var bounds = new Bounds(position, Vector3.one * source.Diameter);
                    if (i == 0) next.Bounds = bounds;
                    else next.Bounds.Encapsulate(bounds);
                }

                next.Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, next.Count, Site.Stride);
                next.Buffer.SetData(sites);
                next.Properties = new MaterialPropertyBlock();
                next.Properties.SetBuffer(SitesId, next.Buffer);
                return next;
            }
            catch
            {
                next.Dispose();
                throw;
            }
        }

        // Called only after surface staging succeeds, without yielding a frame.
        internal void Commit(Frame next)
        {
            Frame previous = current;
            current = next;
            previous?.Dispose();
        }

        public Bounds WorldBounds
        {
            get
            {
                Bounds local = LocalBounds;
                var result = new Bounds(transform.TransformPoint(local.center), Vector3.zero);
                for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                    result.Encapsulate(transform.TransformPoint(local.center + Vector3.Scale(local.extents, new Vector3(x, y, z))));
                return result;
            }
        }

        private void LateUpdate()
        {
            if (current == null || current.VisibleCount == 0) return;
            current.Properties.SetMatrix(LocalToWorldId, transform.localToWorldMatrix);
            var parameters = new RenderParams(siteMaterial)
            {
                layer = gameObject.layer, matProps = current.Properties, worldBounds = WorldBounds,
                motionVectorMode = MotionVectorGenerationMode.ForceNoMotion,
                receiveShadows = false, shadowCastingMode = ShadowCastingMode.Off
            };
            Graphics.RenderMeshPrimitives(parameters, siteMesh, 0, current.Count);
        }

        public void Clear() => Commit(null);
        private void OnDestroy() => Clear();
    }
}
