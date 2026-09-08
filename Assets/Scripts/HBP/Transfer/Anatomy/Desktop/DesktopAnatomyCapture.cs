using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using UnityEngine;

namespace HBP.Transfer.Anatomy.Desktop
{
    /// <summary>Copies only the selected column's already prepared Unity surface. Never loads anatomy.</summary>
    public static class DesktopAnatomyCapture
    {
        /// <summary>Capture once, then retain the offer for every retry. IDs must not change after an ACK is lost.</summary>
        public static async Task<Delivery.AnatomyDelivery> CaptureDeliverySelectedAsync(string transferId, string sessionId, ulong revision, CancellationToken cancellationToken = default)
        {
            AnatomySnapshot snapshot = await CaptureSelectedAsync(transferId, sessionId, revision, cancellationToken).ConfigureAwait(false);
            return await Task.Run(() => new Delivery.AnatomyDelivery(snapshot), cancellationToken).ConfigureAwait(false);
        }

        // The native-to-Unity X reflection and winding conversion have already been
        // applied by Surface.UpdateMesh. This is the local brain frame, in mm;
        // scene spacing, parent transforms and camera framing are presentation only.
        public const string FrameId = "hibop-mni-unity-mm-v1";

        /// <summary>
        /// Call on Unity's main thread. All Unity reads finish before this method returns
        /// its task, without yielding to an edit or retaining a native/Unity handle.
        /// The caller owns delivery/session/revision identity. Validation and construction
        /// then run on private arrays in the thread pool; later Desktop edits are independent.
        /// </summary>
        public static Task<AnatomySnapshot> CaptureSelectedAsync(string transferId, string sessionId, ulong revision, CancellationToken cancellationToken = default)
        {
            if (!PlayerLoopHelper.IsMainThread) throw new InvalidOperationException("Anatomy capture must start on the Unity main thread.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!Module3DMain.IsInitialized) throw new InvalidOperationException("No Desktop visualization is open.");
            Base3DScene scene = Module3DMain.SelectedScene;
            if (scene == null || scene.SelectedColumn is not Column3DAnatomy column)
                throw new InvalidOperationException("Select an anatomical column to capture; the current selection is not supported.");
            if (!scene.SceneInformation.CompletelyLoaded || scene.SceneInformation.GeometryNeedsUpdate || scene.SceneInformation.CutsNeedUpdate || scene.SceneInformation.FunctionalSurfaceNeedsUpdate || scene.IsSurfaceRepresentationTransitioning)
                throw new InvalidOperationException("The selected visualization is still preparing its surface. Retry after the update finishes.");
            MeshManager manager = scene.MeshManager;
            if (manager == null || manager.Meshes.Count == 0 || manager.SelectedMesh.Type != MeshType.MNI || manager.MeshPartToDisplay != MeshPart.Both || manager.SelectedMesh.Representation != SurfaceRepresentation.Anatomical)
                throw new InvalidOperationException("Capture requires the selected complete anatomical MNI surface (both hemispheres, without inflation).");
            if (scene.Cuts.Count != 0 || scene.TriangleEraser.MeshHasInvisibleTriangles)
                throw new InvalidOperationException("Cuts and erased triangles are not supported by the complete anatomy snapshot.");

            Mesh mesh = column.BrainMesh != null ? column.BrainMesh.GetComponent<MeshFilter>()?.sharedMesh : null;
            Renderer renderer = column.BrainMesh != null ? column.BrainMesh.GetComponent<Renderer>() : null;
            if (mesh == null || renderer == null || !mesh.isReadable || mesh.subMeshCount != 1 || mesh.GetTopology(0) != MeshTopology.Triangles)
                throw new InvalidOperationException("The selected column has no readable prepared triangle mesh.");
            if (manager.BrainSurface == null || mesh.vertexCount != manager.BrainSurface.NumberOfVertices || mesh.GetIndexCount(0) != manager.BrainSurface.NumberOfTriangles * 3L)
                throw new InvalidOperationException("The prepared mesh does not contain the complete selected surface.");
            Material material = renderer.sharedMaterial;
            if (material == null || material != scene.BrainMaterials.BrainMaterial || renderer.HasPropertyBlock())
                throw new InvalidOperationException("The selected column's material is not supported by anatomy capture.");
            if (material.GetFloat("_Atlas") != 0 || material.GetFloat("_FMRI") != 0 || material.GetFloat("_Amount") != 0 || material.GetFloat("_InflationBlend") != 0 || material.GetFloat("_CutCount") != 0)
                throw new InvalidOperationException("Scientific coloration, clipping and shader deformation are not supported by anatomy capture.");
            Vector2[] alphaUvs = mesh.uv2;
            Vector2 alphaScale = material.GetTextureScale("_AoTex");
            Vector2 alphaOffset = material.GetTextureOffset("_AoTex");
            if (alphaUvs.Length != mesh.vertexCount || alphaUvs.Any(uv => !(uv.y * alphaScale.y + alphaOffset.y > 0.5f)))
                throw new InvalidOperationException("Projected activity is visible or its opacity buffer is unavailable; capture requires plain anatomy.");

            Texture2D texture = material.GetTexture("_MainTex") as Texture2D;
            if (texture == null || !texture.isReadable)
                throw new InvalidOperationException("The anatomy color texture is not readable.");
            Color32[] pixels = texture.GetPixels32();
            if (pixels.Length == 0 || pixels.Any(pixel => !pixel.Equals(pixels[0])))
                throw new InvalidOperationException("The anatomy snapshot supports a uniform brain color only.");
            Color baseColor = pixels[0];
            if (texture.isDataSRGB) baseColor = baseColor.linear;
            Color tint = material.GetColor("_Color");
            // Material color properties are supplied to the linear renderer in linear light.
            if (QualitySettings.activeColorSpace == ColorSpace.Linear) tint = tint.linear;
            float[] color = { baseColor.r * tint.r, baseColor.g * tint.g, baseColor.b * tint.b, tint.a };
            bool visible = column.BrainMesh.activeSelf && renderer.enabled;
            string visualizationId = scene.Visualization.ID;
            string columnId = column.ColumnData.ID;
            Vector3[] positions = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector2[] uvs = mesh.uv;
            int[] indices = mesh.triangles;

            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                AnatomyCoordinateSpace coordinates = new(FrameId, AnatomyHandedness.Left, AnatomyLengthUnit.Millimeter, 1, new float[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 });
                return AnatomySnapshot.Create(transferId, sessionId, visualizationId, columnId, revision, coordinates, AnatomyWinding.Clockwise, visible, color, Flatten(positions), Flatten(normals), Array.ConvertAll(indices, index => checked((uint)index)), Flatten(uvs));
            }, cancellationToken);
        }

        private static float[] Flatten(Vector3[] values)
        {
            float[] result = new float[values.Length * 3];
            for (int i = 0; i < values.Length; i++)
            {
                result[i * 3] = values[i].x;
                result[i * 3 + 1] = values[i].y;
                result[i * 3 + 2] = values[i].z;
            }

            return result;
        }

        private static float[] Flatten(Vector2[] values)
        {
            float[] result = new float[values.Length * 2];
            for (int i = 0; i < values.Length; i++)
            {
                result[i * 2] = values[i].x;
                result[i * 2 + 1] = values[i].y;
            }

            return result;
        }
    }
}
