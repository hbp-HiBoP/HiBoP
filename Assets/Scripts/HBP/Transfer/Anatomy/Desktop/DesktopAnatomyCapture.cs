using System;
using System.IO;
using System.Security.Cryptography;
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
    /// <summary>Copies the selected column's already prepared surface and contacts. Never loads anatomy.</summary>
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
        public const string FrameId = AnatomyContacts.FrameId;

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
            ValidateSelection(out Base3DScene scene, out Column3D column, out Mesh mesh, out Material material, out Texture2D texture, out Color32[] pixels, out bool visible);
            Color baseColor = pixels[0];
            if (texture.isDataSRGB) baseColor = baseColor.linear;
            Color tint = material.GetColor("_Color");
            // Material color properties are supplied to the linear renderer in linear light.
            if (QualitySettings.activeColorSpace == ColorSpace.Linear) tint = tint.linear;
            float[] color = { baseColor.r * tint.r, baseColor.g * tint.g, baseColor.b * tint.b, tint.a };
            string visualizationId = scene.Visualization.ID;
            string columnId = column.ColumnData.ID;
            Vector3[] positions = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector2[] uvs = mesh.uv;
            int[] indices = mesh.triangles;
            AnatomyContacts contacts = DesktopContactsCapture.Capture(scene, column);
            var mriManager = scene.MRIManager;
            var mri = mriManager != null && mriManager.SelectedMRIID >= 0 && mriManager.SelectedMRIID < mriManager.MRIs.Count ? mriManager.SelectedMRI : null;
            if (mri == null || !mri.IsLoaded) throw new InvalidOperationException("The selected projection reference volume is not loaded.");
            var volume = mri.Volume;
            string volumePath = volume.SourceFilePath, volumeHash = volume.SourceFileSha256;
            if (volumePath == null || volumeHash == null) throw new InvalidOperationException("Reference volume provenance is unavailable. Reload a supported single-file .nii volume before transfer.");
            int grid = Core.DLL.ActivityProjectionSettings.VolumeGridDimension;
            int interpolation = (int)Core.DLL.ActivityProjectionSettings.VolumeInterpolation;
            IEEGInstant ieeg = column is Column3DIEEG dynamicColumn ? DesktopIEEGCapture.Capture(dynamicColumn) : null;
            float influence = column is Column3DDynamic dynamic ? dynamic.DynamicParameters.InfluenceDistance : ((Column3DAnatomy)column).AnatomyParameters.InfluenceDistance, alpha = column.ActivityAlpha;
            // HBNA v3 has no boundary-smoothing field: never substitute a different scientific setting.
            if (!Core.Preferences.PersistentDataManager.UserPreferences.Visualization._3D.SmoothActivityBoundaries)
                throw new InvalidOperationException("Quest density currently requires Smooth activity boundaries enabled; HBNA v3 cannot carry the disabled setting.");
            int rule = (int)Core.Preferences.PersistentDataManager.UserPreferences.Visualization._3D.SiteInfluenceByDistance;

            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var source = new FileStream(volumePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (source.Length > AnatomySnapshotCodec.MaximumEncodedBytes) throw new InvalidOperationException($"Reference volume alone requires {source.Length} bytes, exceeding the {AnatomySnapshotCodec.MaximumEncodedBytes}-byte codec limit. Keep all scientific inputs; select a smaller source dataset or qualify a larger transport budget.");
                byte[] volumeBytes = new byte[checked((int)source.Length)];
                using (var reader = new BinaryReader(source, System.Text.Encoding.UTF8, true)) volumeBytes = reader.ReadBytes(volumeBytes.Length);
                using var sha = SHA256.Create();
                if (BitConverter.ToString(sha.ComputeHash(volumeBytes)) != volumeHash) throw new InvalidOperationException("The reference volume file changed since native loading. Reload the visualization before capture.");
                var projection = new AnatomyProjection(volumeBytes, grid, interpolation, influence, rule, alpha);
                AnatomyCoordinateSpace coordinates = new(FrameId, AnatomyHandedness.Left, AnatomyLengthUnit.Millimeter, 1, new float[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 });
                return AnatomySnapshot.Create(transferId, sessionId, visualizationId, columnId, revision, coordinates, AnatomyWinding.Clockwise, visible, color, Flatten(positions), Flatten(normals), Array.ConvertAll(indices, index => checked((uint)index)), Flatten(uvs), contacts, projection, ieeg);
            }, cancellationToken);
        }

        public static string GetSelectionError()
        {
            try
            {
                ValidateSelection(out _, out _, out _, out _, out _, out _, out _);
                return null;
            }
            catch (InvalidOperationException exception)
            {
                return exception.Message;
            }
        }

        private static void ValidateSelection(out Base3DScene scene, out Column3D column, out Mesh mesh, out Material material, out Texture2D texture, out Color32[] pixels, out bool visible)
        {
            if (!Module3DMain.IsInitialized) throw new InvalidOperationException("No Desktop visualization is open.");
            scene = Module3DMain.SelectedScene;
            if (scene == null || (scene.SelectedColumn is not Column3DAnatomy && scene.SelectedColumn is not Column3DIEEG))
                throw new InvalidOperationException("Select an anatomical or iEEG column to capture; the current selection is not supported.");
            column = scene.SelectedColumn;
            if (!scene.SceneInformation.CompletelyLoaded || scene.SceneInformation.GeometryNeedsUpdate || scene.SceneInformation.ProjectionGridNeedsUpdate || scene.SceneInformation.SurfaceProjectionNeedsUpdate || scene.SceneInformation.SitesNeedUpdate || scene.SceneInformation.CutsNeedUpdate || scene.SceneInformation.FunctionalSurfaceNeedsUpdate || scene.IsSurfaceRepresentationTransitioning)
                throw new InvalidOperationException("The selected visualization is still preparing its surface. Retry after the update finishes.");
            if (column is Column3DIEEG && (!scene.IsGeneratorUpToDate || scene.SceneInformation.GeneratorNeedsUpdate))
                throw new InvalidOperationException("The selected iEEG preparation is not up to date.");
            MeshManager manager = scene.MeshManager;
            if (manager == null || manager.Meshes.Count == 0 || manager.SelectedMesh.Type != MeshType.MNI || manager.MeshPartToDisplay != MeshPart.Both || manager.SelectedMesh.Representation != SurfaceRepresentation.Anatomical)
                throw new InvalidOperationException("Capture requires the selected complete anatomical MNI surface (both hemispheres, without inflation).");
            if (scene.Cuts.Count != 0 || scene.TriangleEraser.MeshHasInvisibleTriangles)
                throw new InvalidOperationException("Cuts and erased triangles are not supported by the complete anatomy snapshot.");

            mesh = column.BrainMesh != null ? column.BrainMesh.GetComponent<MeshFilter>()?.sharedMesh : null;
            Renderer renderer = column.BrainMesh != null ? column.BrainMesh.GetComponent<Renderer>() : null;
            if (mesh == null || renderer == null || !mesh.isReadable || mesh.subMeshCount != 1 || mesh.GetTopology(0) != MeshTopology.Triangles)
                throw new InvalidOperationException("The selected column has no readable prepared triangle mesh.");
            if (manager.BrainSurface == null || mesh.vertexCount != manager.BrainSurface.NumberOfVertices || mesh.GetIndexCount(0) != manager.BrainSurface.NumberOfTriangles * 3L)
                throw new InvalidOperationException("The prepared mesh does not contain the complete selected surface.");
            material = renderer.sharedMaterial;
            if (material == null || material != scene.BrainMaterials.BrainMaterial || renderer.HasPropertyBlock())
                throw new InvalidOperationException("The selected column's material is not supported by anatomy capture.");
            if (material.GetFloat("_Atlas") != 0 || material.GetFloat("_FMRI") != 0 || material.GetFloat("_Amount") != 0 || material.GetFloat("_InflationBlend") != 0 || material.GetFloat("_CutCount") != 0)
                throw new InvalidOperationException("Scientific coloration, clipping and shader deformation are not supported by anatomy capture.");
            Vector2[] alphaUvs = mesh.uv2;
            Vector2 alphaScale = material.GetTextureScale("_AoTex");
            Vector2 alphaOffset = material.GetTextureOffset("_AoTex");
            if (column is not Column3DIEEG && (alphaUvs.Length != mesh.vertexCount || alphaUvs.Any(uv => !(uv.y * alphaScale.y + alphaOffset.y > 0.5f))))
                throw new InvalidOperationException("Projected activity is visible or its opacity buffer is unavailable; capture requires plain anatomy.");

            texture = material.GetTexture("_MainTex") as Texture2D;
            if (texture == null || !texture.isReadable)
                throw new InvalidOperationException("The anatomy color texture is not readable.");
            pixels = texture.GetPixels32();
            Color32 firstPixel = pixels.Length == 0 ? default : pixels[0];
            if (pixels.Length == 0 || pixels.Any(pixel => !pixel.Equals(firstPixel)))
                throw new InvalidOperationException("The anatomy snapshot supports a uniform brain color only.");
            visible = column.BrainMesh.activeSelf && renderer.enabled;
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
