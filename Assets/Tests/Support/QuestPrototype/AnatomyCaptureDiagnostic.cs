using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Data.Module3D;
using HBP.Input;
using HBP.Transfer.Anatomy;
using HBP.Transfer.Anatomy.Desktop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

namespace HBP.Dev
{
    /// <summary>Opt-in diagnostic: -captureAnatomy directory exports once when ready, then on F8.</summary>
    public static class AnatomyCaptureDiagnostic
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-captureAnatomy");
            if (index >= 0 && index + 1 < args.Length)
                RunAsync(Path.GetFullPath(args[index + 1]), Application.exitCancellationToken).Forget(Debug.LogException);
#endif
        }

        private static async UniTask RunAsync(string directory, CancellationToken token)
        {
            float deadline = Time.realtimeSinceStartup + 120;
            while (!Module3DMain.IsInitialized || Module3DMain.SelectedScene == null || !Module3DMain.SelectedScene.SceneInformation.CompletelyLoaded || Module3DMain.SelectedScene.SceneInformation.FunctionalSurfaceNeedsUpdate || Module3DMain.SelectedScene.SceneInformation.CutsNeedUpdate || Module3DMain.SelectedScene.SceneInformation.SitesNeedUpdate)
            {
                if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("Anatomy diagnostic: no selected visualization became ready within 120 seconds.");
                await UniTask.NextFrame(cancellationToken: token);
            }

            string[] arguments = Environment.GetCommandLineArgs();
            int indexOption = Array.IndexOf(arguments, "-captureIEEGIndex");
            if (indexOption >= 0)
            {
                var selectedScene = Module3DMain.SelectedScene;
                if (selectedScene.SelectedColumn is not Column3DIEEG) throw new ArgumentException("-captureIEEGIndex requires an iEEG column.");
                while (selectedScene.SceneInformation.GeometryNeedsUpdate || selectedScene.SceneInformation.ProjectionGridNeedsUpdate || selectedScene.SceneInformation.SurfaceProjectionNeedsUpdate)
                {
                    if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("iEEG diagnostic projection resources did not become ready.");
                    await UniTask.NextFrame(cancellationToken: token);
                }

                if (!selectedScene.IsGeneratorUpToDate)
                {
                    if (selectedScene.TryGetSurfaceProjectionWarning(out _, out string title, out string message)) throw new InvalidOperationException(title + ": " + message);
                    selectedScene.UpdateGenerator();
                }

                while (DesktopAnatomyCapture.GetSelectionError() != null)
                {
                    if (Time.realtimeSinceStartup > deadline) throw new TimeoutException(DesktopAnatomyCapture.GetSelectionError());
                    await UniTask.NextFrame(cancellationToken: token);
                }

                if (indexOption + 1 >= arguments.Length || !int.TryParse(arguments[indexOption + 1], out int selected) || Module3DMain.SelectedScene.SelectedColumn is not Column3DIEEG ieeg || selected < 0 || selected >= ieeg.Timeline.Length)
                    throw new ArgumentException("-captureIEEGIndex requires an exact valid navigation index in an iEEG column.");
                ieeg.Timeline.IsPlaying = false;
                ieeg.Timeline.CurrentIndex = selected;
                await UniTask.NextFrame(cancellationToken: token);
                while (DesktopAnatomyCapture.GetSelectionError() != null)
                {
                    if (Time.realtimeSinceStartup > deadline) throw new TimeoutException(DesktopAnatomyCapture.GetSelectionError());
                    await UniTask.NextFrame(cancellationToken: token);
                }
            }

            while (!token.IsCancellationRequested)
            {
                try
                {
                    string result = await ExportSelectedAsync(directory, token);
                    Debug.Log("Anatomy capture exported: " + result + ". Press F8 to capture the current selection again.");
                    if (Array.IndexOf(arguments, "-captureOnce") >= 0)
                    {
                        Application.Quit(0);
                        return;
                    }
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    Debug.LogWarning("Anatomy capture refused: " + exception.Message);
                }

                do
                {
                    await UniTask.NextFrame(cancellationToken: token);
                } while (!DesktopInput.WasPressedThisFrame(Key.F8));
            }
        }

        /// <summary>Exports two captures plus source/buffer/camera checks. Does not select or open a visualization.</summary>
        public static async Task<string> ExportSelectedAsync(string directory, CancellationToken token = default)
        {
            // Capture validates selection synchronously, before any diagnostic reads.
            string transferId = Guid.NewGuid().ToString();
            Stopwatch copyWatch = Stopwatch.StartNew();
            Task<AnatomySnapshot> firstTask = DesktopAnatomyCapture.CaptureSelectedAsync(transferId, "anatomy-diagnostic", 1, token);
            double copyMilliseconds = copyWatch.Elapsed.TotalMilliseconds;
            Base3DScene scene = Module3DMain.SelectedScene;
            Column3D column = scene.SelectedColumn;
            string visualizationName = scene.Name;
            string columnName = column.Name;
            Mesh source = column.BrainMesh.GetComponent<MeshFilter>().sharedMesh;
            Vector3[] positions = source.vertices;
            Vector3[] normals = source.normals;
            Vector2[] uvs = source.uv;
            int[] indices = source.triangles;
            Bounds bounds = source.bounds;
            Camera[] cameras = scene.Columns.SelectMany(value => value.Views).Select(view => view.Camera).ToArray();
            Matrix4x4[] cameraViews = cameras.Select(camera => camera.worldToCameraMatrix).ToArray();
            Matrix4x4[] cameraProjections = cameras.Select(camera => camera.projectionMatrix).ToArray();
            Matrix4x4 brainTransform = column.BrainMesh.transform.localToWorldMatrix;
            int initialFrame = Time.frameCount;
            string unityVersion = Application.unityVersion;
            string graphicsDevice = SystemInfo.graphicsDeviceName;

            AnatomySnapshot first = await firstTask;
            AnatomySnapshot second = await DesktopAnatomyCapture.CaptureSelectedAsync(transferId, "anatomy-diagnostic", 1, token);
            bool cameraUnchanged = cameras.Select((camera, i) => camera != null && camera.worldToCameraMatrix.Equals(cameraViews[i]) && camera.projectionMatrix.Equals(cameraProjections[i])).All(value => value);
            bool presentationUnchanged = column != null && column.BrainMesh.transform.localToWorldMatrix.Equals(brainTransform);
            int framesDuringCapture = Time.frameCount - initialFrame;
            int cameraCount = cameras.Length;

            return await Task.Run(() =>
            {
                Stopwatch encodeWatch = Stopwatch.StartNew();
                byte[] bytes = AnatomySnapshotCodec.Encode(first);
                byte[] repeated = AnatomySnapshotCodec.Encode(second);
                AnatomySnapshot decoded = AnatomySnapshotCodec.Decode(bytes);
                bool roundTrip = bytes.SequenceEqual(AnatomySnapshotCodec.Encode(decoded));
                double encodeMilliseconds = encodeWatch.Elapsed.TotalMilliseconds;
                bool sourceMatches = EqualBits<Vector3, float>(positions, first.Positions.ToArray()) && EqualBits<Vector3, float>(normals, first.Normals.ToArray()) && EqualBits<Vector2, float>(uvs, first.Uvs.ToArray()) && EqualBits<int, uint>(indices, first.Indices.ToArray());
                string output = Path.Combine(directory, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + transferId);
                Directory.CreateDirectory(output);
                File.WriteAllBytes(Path.Combine(output, "anatomy.hbna"), bytes);
                File.WriteAllBytes(Path.Combine(output, "anatomy-repeat.hbna"), repeated);
                using SHA256 sha = SHA256.Create();
                // Explicit tokens survive IL2CPP stripping; anonymous reflected properties do not.
                JObject report = new()
                {
                    ["IEEG"] = first.IEEG == null ? null : IEEGReport(first.IEEG),
                    ["Task"] = first.IEEG != null ? "QUEST-020" : first.Projection != null ? "QUEST-017" : first.Contacts.Sites.Count == 0 ? "QUEST-006" : "QUEST-013", ["CreatedUtc"] = DateTime.UtcNow.ToString("O"), ["Unity"] = unityVersion, ["GraphicsDevice"] = graphicsDevice,
                    ["Projection"] = first.Projection == null ? null : new JObject
                    {
                        ["VolumeBytes"] = first.Projection.VolumeBytes.Count, ["VolumeSha256"] = BitConverter.ToString(first.Projection.VolumeHash.ToArray()).Replace("-", "").ToLowerInvariant(),
                        ["Dimensions"] = new JArray(first.Projection.Dimensions), ["GridDimension"] = first.Projection.GridDimension,
                        ["Interpolation"] = first.Projection.Interpolation, ["InfluenceDistance"] = first.Projection.InfluenceDistance,
                        ["InfluenceByDistance"] = first.Projection.InfluenceByDistance, ["ActivityAlpha"] = first.Projection.ActivityAlpha
                    },
                    ["VisualizationName"] = visualizationName, ["ColumnName"] = columnName,
                    ["VisualizationId"] = first.VisualizationId, ["ColumnId"] = first.ColumnId,
                    ["SchemaVersion"] = first.SchemaVersion, ["Implantation"] = first.Contacts.Implantation, ["RoiActive"] = first.Contacts.RoiActive,
                    ["PatientIds"] = new JArray(first.Contacts.PatientIds),
                    ["Sites"] = new JArray(first.Contacts.Sites.Select(site => new JObject
                    {
                        ["Id"] = site.Id, ["Name"] = site.Name, ["Electrode"] = site.Electrode, ["Order"] = site.Order,
                        ["PatientIndex"] = site.PatientIndex, ["SourceIndex"] = site.SourceIndex,
                        ["Position"] = new JArray(site.Position.ToArray()), ["Color"] = new JArray(site.Color.ToArray()),
                        ["DiameterMillimeters"] = site.Diameter, ["Visible"] = site.Visible,
                        ["Flags"] = (byte)site.Flags, ["EffectiveMasked"] = site.EffectiveMasked
                    })),
                    ["TransferId"] = first.TransferId, ["SessionId"] = first.SessionId, ["ContentRevision"] = first.ContentRevision,
                    ["FrameId"] = first.Coordinates.FrameId, ["Unit"] = first.Coordinates.Unit.ToString(), ["AssetToBrain"] = new JArray(first.Coordinates.AssetToBrain.ToArray()),
                    ["VertexCount"] = first.VertexCount, ["IndexCount"] = first.Indices.Count, ["TriangleCount"] = first.Indices.Count / 3, ["UvCount"] = first.Uvs.Count / 2,
                    ["BoundsMin"] = new JArray(bounds.min.x, bounds.min.y, bounds.min.z), ["BoundsMax"] = new JArray(bounds.max.x, bounds.max.y, bounds.max.z),
                    ["Color"] = new JArray(first.Color.ToArray()), ["Visible"] = first.Visible, ["SurfaceByteLength"] = first.SurfaceByteLength, ["EncodedBytes"] = bytes.Length,
                    ["Sha256"] = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(),
                    ["SourceBuffersBitExact"] = sourceMatches, ["RepeatedCaptureBitExact"] = bytes.SequenceEqual(repeated), ["RoundTripBitExact"] = roundTrip,
                    ["CameraCount"] = cameraCount, ["CameraUnchanged"] = cameraUnchanged, ["PresentationUnchanged"] = presentationUnchanged,
                    ["MainThreadCopyMilliseconds"] = copyMilliseconds, ["EncodeTwiceAndRoundTripMilliseconds"] = encodeMilliseconds, ["FramesDuringCapture"] = framesDuringCapture
                };
                File.WriteAllText(Path.Combine(output, "capture.json"), report.ToString(Formatting.Indented));
                return output;
            }, token);
        }

        private static JObject IEEGReport(IEEGInstant x) =>
            new()
            {
                ["DatasetId"] = x.DatasetId,
                ["DataName"] = x.DataName,
                ["BlocId"] = x.BlocId,
                ["SubBlocId"] = x.SubBlocId,
                ["PreparedSha256"] = x.PreparedSha256,
                ["NavigationIndex"] = x.NavigationIndex,
                ["NavigationLength"] = x.NavigationLength,
                ["NavigationHz"] = x.NavigationHz,
                ["LocalTimeMilliseconds"] = x.LocalTimeMilliseconds,
                ["ProjectionIndex"] = x.ProjectionIndex,
                ["ProjectionLength"] = x.ProjectionLength,
                ["ProjectionHz"] = x.ProjectionHz,
                ["SamplingPolicy"] = x.SamplingPolicy,
                ["Alpha"] = x.Alpha,
                ["SpanMin"] = x.SpanMin,
                ["Middle"] = x.Middle,
                ["SpanMax"] = x.SpanMax,
                ["Summary"] = x.Summary,
                ["ChannelIds"] = new JArray(x.ChannelIds.ToArray()),
                ["Units"] = new JArray(x.Units.ToArray()),
                ["Availability"] = new JArray(x.Availability.ToArray().Select(value => (int)value)),
                ["SurfaceValues"] = new JArray(x.SurfaceValues.ToArray()),
                ["SiteValues"] = new JArray(x.SiteValues.ToArray())
            };

        private static bool EqualBits<T, U>(T[] left, U[] right) where T : unmanaged where U : unmanaged
        {
            return MemoryMarshal.AsBytes(left.AsSpan()).SequenceEqual(MemoryMarshal.AsBytes(right.AsSpan()));
        }
    }
}
