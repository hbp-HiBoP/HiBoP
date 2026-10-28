#if DEVELOPMENT_BUILD && UNITY_ANDROID
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using HBP.Sync;
using UnityEngine;

namespace HBP.Quest
{
    /// <summary>Explicit ADB smoke check of authored windows and synthetic rendering; no scientific parity claim.</summary>
    internal static class QuestInformationDiagnostic
    {
        [Serializable]
        private sealed class Report
        {
            public bool passed;
            public string error, device;
            public long sampleBytes, releasedBytes, preparationMilliseconds, unityBytes;
            public int matrixTextures;
        }

        internal static async UniTask RunAsync(CancellationToken stop)
        {
            var report = new Report { device = SystemInfo.deviceModel };
            string directory = Path.Combine(Application.persistentDataPath, "information-windows");
            var manager = UnityEngine.Object.FindAnyObjectByType<QuestWindowsManager>();
            QuestWindow site = null, signals = null;
            QuestSignalsPanel panel = null;
            SignalPresentationBuffer buffer = null;
            try
            {
                Directory.CreateDirectory(directory);
                site = manager.Find("Site"); signals = manager.Find("Signals");
                if (site == null || signals == null || site.IsOpen || signals.IsOpen) throw new InvalidOperationException("Close Site and Signals before this diagnostic.");
                panel = signals.GetComponent<QuestSignalsPanel>(); panel.enabled = false;
                manager.Open("Site"); manager.Open("Signals");
                var timer = System.Diagnostics.Stopwatch.StartNew();
                var snapshot = new SignalSnapshot { Revision = 1, Title = "Synthetic rendering check — A1", Format = 0, TrialHeight = 10, Colormap = new byte[] { 20, 30, 180, 255, 240, 240, 240, 255, 180, 30, 20, 255 } };
                string curveKey = new('A', 64), matrixKey = new('B', 64);
                float[] curve = new float[600], matrix = new float[2400];
                for (int i = 0; i < 200; i++) { curve[i * 3] = -100 + i * 3; curve[i * 3 + 1] = Mathf.Sin(i * .09f) * 25; curve[i * 3 + 2] = 4; }
                for (int i = 0; i < matrix.Length; i++) matrix[i] = Mathf.Sin(i * .11f) * 30;
                snapshot.Graphs = new[] { new SignalGraph { XMin = -100, XMax = 500, YMin = -40, YMax = 40, Curves = new[] { new SignalCurve { Name = "A1", Resource = curveKey, Enabled = true, HasShape = true, Color = new[] { .2f, .65f, 1f, 1f } } } } };
                snapshot.Matrices = new[] { new SignalMatrixRow { Title = "A1 / Synthetic trials", Min = -40, Max = 40, Selected = Enumerable.Repeat(true, 24).ToArray(), Cells = new[] { new SignalMatrix { Title = "Response", Resource = matrixKey, Rows = 24, Columns = 100, Start = -100, End = 500, WindowStart = -100, WindowEnd = 500, Unit = "µV" } } } };
                snapshot.Matrices[0].Group = "synthetic-recording";
                snapshot.Matrices[0].GroupTitle = "Synthetic trials";
                snapshot.Matrices[0].BlocId = "synthetic-block";
                snapshot.Matrices[0].BlocTitle = "Response";
                snapshot.Matrices[0].ChannelId = "synthetic-patient|A1";
                snapshot.Matrices[0].ChannelTitle = "A1 (Synthetic patient)";
                buffer = new SignalPresentationBuffer(Guid.NewGuid());
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(QuestSignalsPanel).GetField("buffer", flags).SetValue(panel, buffer);
                buffer.Published += value => typeof(QuestSignalsPanel).GetMethod("Publish", flags).Invoke(panel, new object[] { value });
                SignalMessage Message(SignalMessageKind kind, byte[] bytes = null, string key = "") => new() { Kind = kind, Generation = buffer.Generation, Revision = 1, Body = bytes ?? Array.Empty<byte>(), Key = key };
                buffer.Receive(Message(SignalMessageKind.Begin, key: "content"));
                buffer.Receive(Message(SignalMessageKind.Manifest, SignalSnapshotCodec.Encode(snapshot)));
                foreach (var pair in new[] { (curveKey, curve), (matrixKey, matrix) })
                {
                    byte[] bytes = new byte[pair.Item2.Length * 4]; Buffer.BlockCopy(pair.Item2, 0, bytes, 0, bytes.Length);
                    var message = Message(SignalMessageKind.Resource, bytes, pair.Item1); message.Total = bytes.Length; buffer.Receive(message);
                }
                buffer.Receive(Message(SignalMessageKind.Commit));
                report.sampleBytes = buffer.ResourceBytes;
                await UniTask.DelayFrame(3, cancellationToken: stop);
                Canvas.ForceUpdateCanvases();
                report.preparationMilliseconds = timer.ElapsedMilliseconds;
                report.matrixTextures = panel.GetComponentsInChildren<UnityEngine.UI.RawImage>().Count(image => image.texture is Texture2D);
                if (report.matrixTextures == 0) throw new InvalidOperationException("No matrix texture rendered.");
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, "render.png"));
                await UniTask.Delay(1000, cancellationToken: stop);
                signals.Close(); site.Close();
                await UniTask.DelayFrame(3, cancellationToken: stop);
                report.releasedBytes = buffer.ResourceBytes;
                report.unityBytes = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
                if (report.releasedBytes != 0) throw new InvalidOperationException("Signal samples retained after closing.");
                report.passed = true;
            }
            catch (Exception exception) { report.error = exception.ToString(); }
            finally
            {
                if (signals != null) signals.Close(); if (site != null) site.Close();
                if (panel != null) panel.enabled = true;
                buffer?.ClearData();
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "result.json"), JsonUtility.ToJson(report, true));
                Debug.Log("INFORMATION_WINDOWS_CHECK " + JsonUtility.ToJson(report));
            }
        }
    }
}
#endif
