using System;
using System.Collections.Generic;

namespace HBP.Sync
{
    public enum EntityKind : byte
    {
        Scene = 1,
        Column = 2,
        Site = 3,
        Cut = 4,
        Roi = 5,
        Sphere = 6
    }

    public enum ValueKind : byte
    {
        Bool = 1,
        Int = 2,
        Float = 3,
        Text = 4,
        Id = 5,
        Resource = 6,
        Vector3 = 7,
        Color = 8,
        TextList = 9,
        Mask = 10
    }

    public sealed class FieldDefinition
    {
        public EntityKind Entity { get; }
        public ushort Id { get; }
        public ushort Group { get; }
        public ValueKind Value { get; }
        public string Name { get; }

        internal FieldDefinition(EntityKind entity, ushort id, ushort group, ValueKind value, string name)
        {
            Entity = entity;
            Id = id;
            Group = group;
            Value = value;
            Name = name;
        }
    }

    /// <summary>Frozen field numbers for schema 1. Group numbers are scoped to an entity.</summary>
    public static class SharedStateSchema
    {
        public const ushort Version = 1;
        public const int MaxFields = 100000;
        public const int MaxStateBytes = 16 * 1024 * 1024;
        private static readonly Dictionary<int, FieldDefinition> Fields = Build();
        public static IEnumerable<FieldDefinition> Definitions => Fields.Values;

        public static bool TryGet(EntityKind entity, ushort id, out FieldDefinition definition) => Fields.TryGetValue(((int)entity << 16) | id, out definition);

        private static Dictionary<int, FieldDefinition> Build()
        {
            var fields = new Dictionary<int, FieldDefinition>();
            void Add(EntityKind e, ushort id, ushort group, ValueKind kind, string name) => fields.Add(((int)e << 16) | id, new FieldDefinition(e, id, group, kind, name));

            // Scene fields. IDs 1-4 are session-scoped scene choices, not transport metadata.
            Add(EntityKind.Scene, 1, 1, ValueKind.Id, "selectedColumnId");
            Add(EntityKind.Scene, 2, 2, ValueKind.Id, "activeRoiId");
            Add(EntityKind.Scene, 3, 3, ValueKind.Bool, "strongCuts");
            Add(EntityKind.Scene, 4, 30, ValueKind.Bool, "automaticCutAroundSelectedSite");
            Add(EntityKind.Scene, 5, 4, ValueKind.Bool, "hideBlacklistedSites");
            Add(EntityKind.Scene, 6, 5, ValueKind.Bool, "showAllSites");
            Add(EntityKind.Scene, 7, 6, ValueKind.Float, "siteGain");
            Add(EntityKind.Scene, 8, 7, ValueKind.Id, "comparisonSiteId");
            Add(EntityKind.Scene, 9, 7, ValueKind.Resource, "correlationResult");
            Add(EntityKind.Scene, 10, 8, ValueKind.Bool, "displayCorrelations");
            Add(EntityKind.Scene, 11, 9, ValueKind.Resource, "mesh");
            Add(EntityKind.Scene, 12, 9, ValueKind.Resource, "previewMri");
            Add(EntityKind.Scene, 13, 9, ValueKind.Int, "meshPart");
            Add(EntityKind.Scene, 14, 9, ValueKind.Int, "representation");
            Add(EntityKind.Scene, 15, 9, ValueKind.Mask, "erasedTriangles");
            Add(EntityKind.Scene, 16, 9, ValueKind.Mask, "erasedSimplifiedTriangles");
            Add(EntityKind.Scene, 17, 10, ValueKind.Resource, "mri");
            Add(EntityKind.Scene, 18, 11, ValueKind.Float, "mriCalibrationMin");
            Add(EntityKind.Scene, 19, 11, ValueKind.Float, "mriCalibrationMax");
            Add(EntityKind.Scene, 20, 12, ValueKind.Resource, "implantation");
            Add(EntityKind.Scene, 21, 13, ValueKind.Color, "brainColor");
            Add(EntityKind.Scene, 22, 14, ValueKind.Color, "cutColor");
            Add(EntityKind.Scene, 23, 15, ValueKind.Int, "colormap");
            Add(EntityKind.Scene, 24, 16, ValueKind.Bool, "showEdges");
            Add(EntityKind.Scene, 25, 17, ValueKind.Bool, "transparentBrain");
            Add(EntityKind.Scene, 26, 18, ValueKind.Float, "brainAlpha");
            Add(EntityKind.Scene, 27, 19, ValueKind.Bool, "projectionEnabled");
            Add(EntityKind.Scene, 28, 20, ValueKind.Bool, "marsAtlas");
            Add(EntityKind.Scene, 29, 21, ValueKind.Bool, "juBrainAtlas");
            Add(EntityKind.Scene, 30, 22, ValueKind.Float, "atlasAlpha");
            Add(EntityKind.Scene, 31, 23, ValueKind.Bool, "ibcEnabled");
            Add(EntityKind.Scene, 32, 23, ValueKind.Resource, "ibcContrast");
            Add(EntityKind.Scene, 33, 24, ValueKind.Bool, "difumoEnabled");
            Add(EntityKind.Scene, 34, 24, ValueKind.Resource, "difumoAtlas");
            Add(EntityKind.Scene, 35, 24, ValueKind.Id, "difumoArea");
            Add(EntityKind.Scene, 36, 25, ValueKind.Bool, "localizersEnabled");
            Add(EntityKind.Scene, 37, 25, ValueKind.Resource, "localizerProtocol");
            Add(EntityKind.Scene, 38, 25, ValueKind.Resource, "localizerData");
            Add(EntityKind.Scene, 39, 25, ValueKind.Id, "localizerBloc");
            Add(EntityKind.Scene, 40, 26, ValueKind.Float, "localizerTime");
            Add(EntityKind.Scene, 41, 27, ValueKind.Float, "localizerMin");
            Add(EntityKind.Scene, 42, 27, ValueKind.Float, "localizerMiddle");
            Add(EntityKind.Scene, 43, 27, ValueKind.Float, "localizerMax");
            Add(EntityKind.Scene, 44, 28, ValueKind.Float, "fmriAtlasAlpha");
            Add(EntityKind.Scene, 45, 29, ValueKind.Float, "fmriAtlasNegativeMin");
            Add(EntityKind.Scene, 46, 29, ValueKind.Float, "fmriAtlasNegativeMax");
            Add(EntityKind.Scene, 47, 29, ValueKind.Float, "fmriAtlasPositiveMin");
            Add(EntityKind.Scene, 48, 29, ValueKind.Float, "fmriAtlasPositiveMax");

            Add(EntityKind.Column, 1, 1, ValueKind.Bool, "exists");
            Add(EntityKind.Column, 2, 1, ValueKind.Int, "order");
            Add(EntityKind.Column, 3, 1, ValueKind.Int, "modality");
            Add(EntityKind.Column, 4, 2, ValueKind.Id, "selectedSiteId");
            Add(EntityKind.Column, 5, 3, ValueKind.Float, "activityAlpha");
            Add(EntityKind.Column, 6, 4, ValueKind.Float, "anatomyInfluenceDistance");
            Add(EntityKind.Column, 7, 5, ValueKind.Resource, "staticLabel");
            Add(EntityKind.Column, 8, 6, ValueKind.Float, "staticSpanMin");
            Add(EntityKind.Column, 9, 6, ValueKind.Float, "staticSpanMiddle");
            Add(EntityKind.Column, 10, 6, ValueKind.Float, "staticSpanMax");
            Add(EntityKind.Column, 11, 7, ValueKind.Float, "staticInfluenceDistance");
            Add(EntityKind.Column, 12, 8, ValueKind.Float, "dynamicSpanMin");
            Add(EntityKind.Column, 13, 8, ValueKind.Float, "dynamicSpanMiddle");
            Add(EntityKind.Column, 14, 8, ValueKind.Float, "dynamicSpanMax");
            Add(EntityKind.Column, 15, 9, ValueKind.Float, "dynamicInfluenceDistance");
            Add(EntityKind.Column, 16, 10, ValueKind.Int, "ccepSourceMode");
            Add(EntityKind.Column, 17, 10, ValueKind.Id, "ccepSourceSiteId");
            Add(EntityKind.Column, 18, 10, ValueKind.Id, "ccepMarsAtlasLabel");
            Add(EntityKind.Column, 19, 11, ValueKind.Resource, "functionalResource");
            Add(EntityKind.Column, 20, 12, ValueKind.Float, "negativeMin");
            Add(EntityKind.Column, 21, 12, ValueKind.Float, "negativeMax");
            Add(EntityKind.Column, 22, 12, ValueKind.Float, "positiveMin");
            Add(EntityKind.Column, 23, 12, ValueKind.Float, "positiveMax");
            Add(EntityKind.Column, 24, 13, ValueKind.Bool, "hideLower");
            Add(EntityKind.Column, 25, 13, ValueKind.Bool, "hideMiddle");
            Add(EntityKind.Column, 26, 13, ValueKind.Bool, "hideHigher");
            Add(EntityKind.Column, 27, 14, ValueKind.Int, "timelineIndex");
            Add(EntityKind.Column, 28, 14, ValueKind.Bool, "timelinePlaying");
            Add(EntityKind.Column, 29, 14, ValueKind.Bool, "timelineLooping");
            Add(EntityKind.Column, 30, 14, ValueKind.Int, "timelineStep");
            Add(EntityKind.Column, 31, 14, ValueKind.Float, "timelineSampling");
            Add(EntityKind.Column, 32, 14, ValueKind.Float, "timelineAnchorTime");

            Add(EntityKind.Site, 1, 1, ValueKind.Bool, "exists");
            Add(EntityKind.Site, 2, 2, ValueKind.Bool, "isFiltered");
            Add(EntityKind.Site, 3, 3, ValueKind.Bool, "blacklisted");
            Add(EntityKind.Site, 4, 4, ValueKind.Bool, "highlighted");
            Add(EntityKind.Site, 5, 5, ValueKind.Color, "color");
            Add(EntityKind.Site, 6, 6, ValueKind.TextList, "labels");
            Add(EntityKind.Site, 7, 7, ValueKind.Vector3, "scientificPosition");

            Add(EntityKind.Cut, 1, 1, ValueKind.Bool, "exists");
            Add(EntityKind.Cut, 2, 1, ValueKind.Int, "order");
            Add(EntityKind.Cut, 3, 2, ValueKind.Int, "orientation");
            Add(EntityKind.Cut, 4, 2, ValueKind.Vector3, "normal");
            Add(EntityKind.Cut, 5, 2, ValueKind.Bool, "flip");
            Add(EntityKind.Cut, 6, 2, ValueKind.Vector3, "position");

            Add(EntityKind.Roi, 1, 1, ValueKind.Bool, "exists");
            Add(EntityKind.Roi, 2, 1, ValueKind.Int, "order");
            Add(EntityKind.Roi, 3, 2, ValueKind.Text, "name");
            Add(EntityKind.Sphere, 1, 1, ValueKind.Bool, "exists");
            Add(EntityKind.Sphere, 2, 1, ValueKind.Int, "order");
            Add(EntityKind.Sphere, 3, 2, ValueKind.Vector3, "position");
            Add(EntityKind.Sphere, 4, 2, ValueKind.Float, "influenceRadius");
            return fields;
        }
    }
}
