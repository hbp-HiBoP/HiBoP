using System.IO;
using System.Reflection;
using HBP.UI.Module3D;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Rendering
{
    public class Phase5PerformanceValidationTests
    {
        [Test]
        public void BenchmarkRenderTextureOverride_IsExactAndDoesNotChangeTheDefaultPolicy()
        {
            GameObject viewObject = new("phase 5 benchmark view", typeof(RectTransform));
            MethodInfo method = typeof(View3DUI).GetMethod("GetRenderTextureSize", BindingFlags.Static | BindingFlags.NonPublic);

            try
            {
                RectTransform rectTransform = viewObject.GetComponent<RectTransform>();
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 100.0f);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 200.0f);
                View3DUI.RenderTextureSizeOverride = new Vector2Int(348, 516);

                Vector2Int size = (Vector2Int)method.Invoke(null, new object[] { rectTransform });

                Assert.That(size, Is.EqualTo(new Vector2Int(348, 516)));
            }
            finally
            {
                View3DUI.RenderTextureSizeOverride = null;
                Object.DestroyImmediate(viewObject);
            }

            Assert.That(View3DUI.RenderTextureSizeOverride, Is.Null);
        }

        [Test]
        public void Phase5Capture_ContainsTheRequiredPerformanceMatrixAndDiagnostics()
        {
            string capture = File.ReadAllText("Assets/Scripts/HBP/Dev/Rendering/RenderingBaselineCapture.cs");
            string report = File.ReadAllText("Assets/Scripts/HBP/Dev/Rendering/RenderingBaselineReport.cs");

            StringAssert.Contains("small_static_opaque_edges_off", capture);
            StringAssert.Contains("small_static_transparent_edges_on", capture);
            StringAssert.Contains("small_camera_rotation", capture);
            StringAssert.Contains("small_activity_time_update", capture);
            StringAssert.Contains("small_atlas_hover", capture);
            StringAssert.Contains("small_cut_move", capture);
            StringAssert.Contains("sites_30000_1x1", capture);
            StringAssert.Contains("multi_view_9x3_static", capture);
            StringAssert.Contains("small_static_3d_cameras_disabled_control", capture);
            StringAssert.Contains("GC Allocated In Frame", capture);
            StringAssert.Contains("private const int RealSampleFrames = 900", capture);
            StringAssert.Contains("private const int SiteStressSampleFrames = 900", capture);
            StringAssert.Contains("private const int HighViewSampleFrames = 900", capture);
            StringAssert.Contains("HighViewReferenceViewSize = new(112, 200)", capture);
            StringAssert.Contains("urp-phase5", capture);
            StringAssert.Contains("public const int CurrentSchemaVersion = 3", report);
            StringAssert.Contains("GcAllocatedBytesPerFrame", report);
            StringAssert.Contains("List<MemorySnapshot>", report);
            StringAssert.Contains("SceneTargetRenderTextureCount", report);
            StringAssert.Contains("SceneTargetRenderTexturePixelCount", report);
            StringAssert.Contains("HbpViewRenderTextureCount", report);
            StringAssert.Contains("HbpViewRenderTexturePixelCount", report);
        }
    }
}
