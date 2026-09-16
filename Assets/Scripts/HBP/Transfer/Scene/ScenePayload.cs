using System.Collections.Generic;
using HBP.Core.Data;
using HBP.Core.Object3D;

namespace HBP.Transfer.Scene
{
    public sealed class ScenePayload
    {
        public const int FormatVersion = 4;
        public const int LegacyFormatVersion = 3;
        public int Version = FormatVersion;
        public string TransferId;
        public string GlobalContextId;
        public string SessionId;
        public ulong Revision;
        public Visualization Visualization;
        public Dictionary<string, string> StandardFiles = new();
        public List<MeshResource> Meshes = new();
        public List<VolumeResource> MRIs = new();
        public List<ColumnState> Columns = new();
    }

    public sealed class MeshResource
    {
        public string Name, PatientId, Standard, SourceMRI;
        public HBP.Core.DLL.PreviewSurfaceReport GenerationReport;
        public SurfaceInflationPreset InflationPreset;
        public HBP.Core.DLL.SurfaceInflationOptions InflationOptions;
        public HBP.Core.DLL.SurfaceInflationCoordinateSpace InflatedCoordinates;
        public HBP.Core.Enums.MeshType Type;
        public SurfaceRepresentation Representation;
        public string Both, Left, Right, InflatedBoth, InflatedLeft, InflatedRight;
        public string SimplifiedBoth, SimplifiedLeft, SimplifiedRight, InflatedSimplifiedBoth, InflatedSimplifiedLeft, InflatedSimplifiedRight;
        public int[] StandardBothMask, StandardLeftMask, StandardRightMask;
    }

    public sealed class VolumeResource
    {
        public string Name, PatientId, File, Standard;
    }

    public sealed class FunctionalResource
    {
        public string Name, PatientId, File, Mask;
        public Dictionary<string, float[]> Values;
        public Dictionary<string, string> Units;
        public float Frequency;
    }

    /// <summary>Prepared resources only; persistent choices belong to column configurations.</summary>
    public sealed class ColumnState
    {
        public string Id;
        public List<FunctionalResource> Functional = new();
    }
}
