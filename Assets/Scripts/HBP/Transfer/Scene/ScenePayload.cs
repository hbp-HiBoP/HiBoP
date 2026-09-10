using System.Collections.Generic;
using HBP.Core.Data;
using HBP.Core.Object3D;

namespace HBP.Transfer.Scene
{
    public sealed class ScenePayload
    {
        public const int FormatVersion = 2;
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
        public SceneState State = new();
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

    public sealed class ColumnState
    {
        public string Id, SelectedSite, SourceSite;
        public int ResourceIndex, TimeIndex, TimeStep, SourceMode, SourceLabel;
        public bool Looping, Playing;
        public float AnatomyInfluence;
        public Dictionary<string, Dictionary<string, float>> Correlations, CorrelationMeans;
        public List<FunctionalResource> Functional = new();
        public Dictionary<string, SiteDisplayState> Sites = new();
    }

    public sealed class SiteDisplayState
    {
        public float[] Position;
        public bool Masked;
        public bool Filtered;
    }

    public sealed class SceneState
    {
        public int SelectedColumn, SelectedROI = -1;
        public bool MarsAtlas, JuBrain;
        public float AtlasAlpha;
        public bool IBC, DiFuMo, Localizers;
        public int IBCIndex, DiFuMoArea, LocalizerTime;
        public string DiFuMoAtlas, LocalizerProtocol, LocalizerData, LocalizerBloc;
        public float FMRIAlpha, NegativeMin, NegativeMax, PositiveMin, PositiveMax, LocalizerMin, LocalizerMiddle, LocalizerMax;
        public int[] ErasedTriangles, ErasedSimplifiedTriangles;
        public bool ROICreationMode, DisplayCorrelations;
        public int SelectedSphere = -1;
    }
}
