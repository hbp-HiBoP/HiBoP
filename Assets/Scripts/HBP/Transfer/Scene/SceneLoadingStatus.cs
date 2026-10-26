using System;
using System.IO;
using System.Globalization;
using HBP.Core.Tools;

namespace HBP.Transfer.Scene
{
    /// <summary>Small presentation snapshot; scientific data and transfer ownership remain separate.</summary>
    public sealed class SceneLoadingStatus
    {
        public string VisualizationId;
        public string Prefix = "", Message = "", Suffix = "";
        public float Progress;
        public bool Transfer;

        public static SceneLoadingStatus From(float progress, LoadingText text, string visualizationId = null, bool transfer = false) =>
            new()
            {
                VisualizationId = visualizationId,
                Prefix = text?.Prefix ?? "",
                Message = text?.Message ?? "",
                Suffix = text?.Suffix ?? "",
                // Legacy desktop loaders can overshoot; only the shared presentation is normalized.
                Progress = float.IsNaN(progress) ? 0 : Math.Max(0, Math.Min(1, progress)),
                Transfer = transfer
            };

        public LoadingText ToText(string[] visualizations, string[] names, bool receiving = false)
        {
            int index = Array.IndexOf(visualizations, VisualizationId);
            string scene = index >= 0 && names != null ? names[index] : "";
            return Transfer ? new LoadingText(receiving ? "Receiving scene " : "Sending scene to Quest ", scene, " - " + Math.Round(Progress * 100, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture) + "%") : new LoadingText(Prefix, Message, Suffix);
        }

        public void Validate(string[] visualizations)
        {
            if (VisualizationId != null && Array.IndexOf(visualizations, VisualizationId) < 0 || float.IsNaN(Progress) || float.IsInfinity(Progress) || Progress < 0 || Progress > 1 || Prefix == null || Message == null || Suffix == null || Prefix.Length > 2048 || Message.Length > 2048 || Suffix.Length > 2048)
                throw new InvalidDataException("Invalid scene loading status.");
        }
    }
}
