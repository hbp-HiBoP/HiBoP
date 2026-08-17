using System;
using HBP.Dev.Rendering;
using UnityEditor;
using UnityEngine;

namespace HBP.Dev.Rendering.EditorTools
{
    public static class RenderingBaselineMenu
    {
        private const string FullMenuPath = "Tools/HiBoP/Rendering/Capture Current Pipeline Validation";
        private const string FastMenuPath = "Tools/HiBoP/Rendering/Capture Current Pipeline Validation without 30k sites";

        [MenuItem(FullMenuPath)]
        private static async void CaptureFullBaseline()
        {
            await CaptureAsync(true);
        }

        [MenuItem(FullMenuPath, true)]
        private static bool ValidateCaptureFullBaseline()
        {
            return Application.isPlaying && !RenderingBaselineCapture.IsRunning;
        }

        [MenuItem(FastMenuPath)]
        private static async void CaptureFastBaseline()
        {
            await CaptureAsync(false);
        }

        [MenuItem(FastMenuPath, true)]
        private static bool ValidateCaptureFastBaseline()
        {
            return Application.isPlaying && !RenderingBaselineCapture.IsRunning;
        }

        private static async System.Threading.Tasks.Task CaptureAsync(bool includeSiteStress)
        {
            try
            {
                string directory = await RenderingBaselineCapture.RunAsync(includeSiteStress: includeSiteStress);
                Debug.Log($"Rendering validation artifacts were written to: {directory}");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
