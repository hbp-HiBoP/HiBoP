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
            while (!Module3DMain.IsInitialized || Module3DMain.SelectedScene == null || !Module3DMain.SelectedScene.SceneInformation.CompletelyLoaded || Module3DMain.SelectedScene.SceneInformation.FunctionalSurfaceNeedsUpdate || Module3DMain.SelectedScene.SceneInformation.CutsNeedUpdate)
            {
                if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("Anatomy diagnostic: no selected visualization became ready within 120 seconds.");
                await UniTask.NextFrame(cancellationToken: token);
            }

            while (!token.IsCancellationRequested)
            {
                try
                {
                    string result = await ExportSelectedAsync(directory, token);
                    Debug.Log("Anatomy capture exported: " + result + ". Press F8 to capture the current selection again.");
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
                    ["Task"] = "QUEST-006", ["CreatedUtc"] = DateTime.UtcNow.ToString("O"), ["Unity"] = unityVersion, ["GraphicsDevice"] = graphicsDevice,
                    ["VisualizationName"] = visualizationName, ["ColumnName"] = columnName,
                    ["VisualizationId"] = first.VisualizationId, ["ColumnId"] = first.ColumnId,
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

        private static bool EqualBits<T, U>(T[] left, U[] right) where T : unmanaged where U : unmanaged
        {
            return MemoryMarshal.AsBytes(left.AsSpan()).SequenceEqual(MemoryMarshal.AsBytes(right.AsSpan()));
        }
    }
}
