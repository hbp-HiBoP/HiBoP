using System;
using System.IO;
using System.Threading.Tasks;
using HBP.Transfer.Anatomy;
using UnityEngine;
using UnityEngine.Profiling;

namespace HBP.Quest.Legacy
{
    /// <summary>Opt-in development probe of the renderer on the device, using a Desktop HBNA export.
    /// This local injection is never evidence of a network transfer or a user's controller gestures.</summary>
    public static class QuestContactDiagnostic
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
#if DEVELOPMENT_BUILD && UNITY_ANDROID
            string marker = Path.Combine(Application.persistentDataPath, "quest014-render-probe");
            if (!File.Exists(marker)) return;
            File.Delete(marker);
            _ = RunAsync(); // RunAsync observes failures and reports them.
#endif
        }

        private static async Task RunAsync()
        {
            QuestAnatomyView view = null;
            try
            {
                var token = Application.exitCancellationToken;
                string fixture = Path.Combine(Application.persistentDataPath, "quest014-contacts.hbna");
                var snapshot = await Task.Run(() => AnatomySnapshotCodec.Decode(File.ReadAllBytes(fixture)), token);
                if (snapshot.VertexCount != 69104 || snapshot.Contacts.Sites.Count != 8) throw new InvalidDataException("Expected the MNI Contacts fixture.");
                view = UnityEngine.Object.FindFirstObjectByType<QuestAnatomyView>();
                // Local injection does not pair: hide credentials from the diagnostic screenshots.
                var panel = UnityEngine.Object.FindFirstObjectByType<QuestConnectionPanel>();
                if (panel != null) panel.gameObject.SetActive(false);
                var renderer = view.GetComponentInChildren<QuestContactRenderer>();
                Record("baseline", view, renderer);
                for (int cycle = 0; cycle < 3; cycle++)
                {
                    view.ApplySnapshot(snapshot);
                    await Task.Delay(100, token);
                    Require(renderer.BufferBytes == 256 && renderer.VisibleSiteCount == 8, "Eight visible contact instances");
                    Record("loaded-" + cycle, view, renderer);
                    Mesh previous = view.SharedMesh;
                    view.ApplySnapshot(snapshot);
                    await Task.Delay(100, token);
                    Require(previous == null && renderer.BufferBytes == 256, "Replacement releases the old mesh and keeps one site buffer");
                    Record("replaced-" + cycle, view, renderer);
                    view.Clear();
                    await Task.Delay(100, token);
                    Require(view.BufferBytes == 0 && renderer.SiteCount == 0, "Clear releases the contact buffer");
                    Record("cleared-" + cycle, view, renderer);
                }

                view.ApplySnapshot(snapshot);
                await Task.Delay(2000, token); // Allow initial tracked placement to settle.
                Mesh mesh = view.SharedMesh;
                int uploads = view.UploadCount;
                Vector3 scale = view.transform.localScale;
                await Observe("surface-visible", view, renderer);
                view.ToggleSurface();
                Require(!view.SurfaceVisible && renderer.VisibleSiteCount == 8 && view.Contacts == snapshot.Contacts, "Visual mask keeps prepared contacts");
                await Observe("surface-hidden", view, renderer);
                Bounds bounds = renderer.WorldBounds;
                view.transform.localScale = scale * 2;
                Require(Vector3.Distance(renderer.WorldBounds.size, bounds.size * 2) < 0.00001f, "Contact bounds follow group scale");
                await Observe("surface-hidden-scale-2", view, renderer);
                view.transform.localScale = scale;
                view.ToggleSurface();
                Require(view.SurfaceVisible && view.SharedMesh == mesh && view.UploadCount == uploads, "Visibility and scale never reupload resources");
                view.Clear();
                await Task.Delay(100, token);
                Require(mesh == null && renderer.BufferBytes == 0, "Final release");
                Record("complete", view, renderer);
                Debug.Log("QUEST014_PROBE_PASS local-render-only");
            }
            catch (Exception exception)
            {
                Debug.LogError("QUEST014_PROBE_FAIL " + exception.GetType().Name + ": " + exception.Message);
            }
            finally
            {
                if (view != null) view.Clear();
            }
        }

        private static async Task Observe(string stage, QuestAnatomyView view, QuestContactRenderer renderer)
        {
            Debug.Log("QUEST014_STAGE " + stage);
            await Task.Delay(12000, Application.exitCancellationToken);
            Record(stage, view, renderer);
            // Android appends the persistent data path to this filename itself.
            ScreenCapture.CaptureScreenshot("quest014-" + stage + ".png");
            await Task.Delay(1000, Application.exitCancellationToken);
        }

        private static void Record(string stage, QuestAnatomyView view, QuestContactRenderer renderer)
        {
            Debug.Log($"QUEST014_RESOURCES stage={stage}; contacts={renderer.SiteCount}; visible={renderer.VisibleSiteCount}; contactBytes={renderer.BufferBytes}; allBufferBytes={view.BufferBytes}; unityAllocatedBytes={Profiler.GetTotalAllocatedMemoryLong()}; managedBytes={GC.GetTotalMemory(false)}; uploads={view.UploadCount}");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
