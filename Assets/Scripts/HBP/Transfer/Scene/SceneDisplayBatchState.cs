using System;
using System.IO;
using System.Linq;

namespace HBP.Transfer.Scene
{
    /// <summary>Authenticated session control; archives and scientific payloads remain unchanged.</summary>
    public sealed class SceneDisplayBatchState
    {
        public string Id;
        public string[] Visualizations;
        public float Progress;
        public bool Finished;
        public string ActiveVisualizationId;
        public float ReceivingProgress;
        public string[] IncompleteVisualizations = Array.Empty<string>();
        public string[] Names;
        public SceneLoadingStatus SourceStatus;
        public SceneLoadingStatus TransferStatus;
        public SceneLoadingStatus ReceivingStatus;

        public void Validate()
        {
            if (!Guid.TryParseExact(Id, "N", out _) || Visualizations == null || Visualizations.Length == 0 || Visualizations.Length > SceneSessionState.MaximumScenes || Visualizations.Any(string.IsNullOrEmpty) || Visualizations.Any(value => value.Length > 128) || Visualizations.Distinct(StringComparer.Ordinal).Count() != Visualizations.Length || float.IsNaN(Progress) || Progress < 0 || Progress > 1)
                throw new InvalidDataException("Invalid Display batch.");
            if (ActiveVisualizationId != null && !Visualizations.Contains(ActiveVisualizationId) || float.IsNaN(ReceivingProgress) || ReceivingProgress < 0 || ReceivingProgress > 1)
                throw new InvalidDataException("Invalid Display preparation progress.");
            if (IncompleteVisualizations == null || IncompleteVisualizations.Length > Visualizations.Length || IncompleteVisualizations.Any(value => !Visualizations.Contains(value)))
                throw new InvalidDataException("Invalid incomplete Display scenes.");
            if (Names != null && (Names.Length != Visualizations.Length || Names.Any(value => value == null || value.Length > 1024)))
                throw new InvalidDataException("Invalid Display presentation.");
            SourceStatus?.Validate(Visualizations);
            TransferStatus?.Validate(Visualizations);
            ReceivingStatus?.Validate(Visualizations);
        }
    }
}
