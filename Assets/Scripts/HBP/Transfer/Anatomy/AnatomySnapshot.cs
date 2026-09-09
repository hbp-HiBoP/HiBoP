using System;

namespace HBP.Transfer.Anatomy
{
    /// <summary>
    /// A complete prepared anatomical surface for one existing visualization/column.
    /// Public construction copies buffers; the producer must keep its sources stable
    /// until Create returns. Thereafter encoding needs no producer lock. No pool/native
    /// handle, authority policy, Unity instance ID or presentation state is retained.
    /// </summary>
    public sealed class AnatomySnapshot
    {
        public string TransferId { get; }
        public string SessionId { get; }
        public string VisualizationId { get; }
        public string ColumnId { get; }
        public ulong ContentRevision { get; }
        public ushort SchemaVersion => Contacts.Sites.Count == 0 && Contacts.PatientIds.Count == 0 && !Contacts.RoiActive && Contacts.Implantation == "MNI" ? (ushort)1 : AnatomySnapshotCodec.SchemaVersion;
        public AnatomyContacts Contacts { get; }
        public AnatomyCoordinateSpace Coordinates { get; }

        /// <summary>Front-face winding in asset coordinates, before AssetToBrain.</summary>
        public AnatomyWinding Winding { get; }

        public bool Visible { get; }

        /// <summary>Uniform linear-light RGB and linear opacity, each in [0,1].</summary>
        public AnatomyBuffer<float> Color { get; }

        /// <summary>Interleaved XYZ float32, without conversion or quantization.</summary>
        public AnatomyBuffer<float> Positions { get; }

        public AnatomyBuffer<float> Normals { get; }
        public AnatomyBuffer<uint> Indices { get; }

        /// <summary>Optional interleaved UV float32, empty or one pair per vertex.</summary>
        public AnatomyBuffer<float> Uvs { get; }

        public int VertexCount => Positions.Count / 3;
        public long SurfaceByteLength => 4L * (Positions.Count + Normals.Count + Indices.Count + Uvs.Count);

        public static AnatomySnapshot Create(string transferId, string sessionId, string visualizationId, string columnId, ulong contentRevision, AnatomyCoordinateSpace coordinates, AnatomyWinding winding, bool visible, float[] color, float[] positions, float[] normals, uint[] indices, float[] uvs, AnatomyContacts contacts = null)
        {
            ValidateDimensions(positions, normals, indices, uvs);
            ValidateMetadata(transferId, sessionId, visualizationId, columnId, contentRevision, coordinates, winding, color);
            ValidateBuffers(positions, normals, indices, uvs);
            return new AnatomySnapshot(transferId, sessionId, visualizationId, columnId, contentRevision, coordinates, winding, visible, (float[])color.Clone(), (float[])positions.Clone(), (float[])normals.Clone(), (uint[])indices.Clone(), (float[])uvs.Clone(), contacts);
        }

        // Only the decoder may transfer its fresh, private arrays without copying.
        internal AnatomySnapshot(string transferId, string sessionId, string visualizationId, string columnId, ulong contentRevision, AnatomyCoordinateSpace coordinates, AnatomyWinding winding, bool visible, float[] color, float[] positions, float[] normals, uint[] indices, float[] uvs, AnatomyContacts contacts = null)
        {
            ValidateMetadata(transferId, sessionId, visualizationId, columnId, contentRevision, coordinates, winding, color);
            ValidateDimensions(positions, normals, indices, uvs);
            ValidateBuffers(positions, normals, indices, uvs);
            Contacts = contacts ?? AnatomyContacts.Empty;
            Contacts.ValidateCoordinates(coordinates);
            TransferId = transferId;
            SessionId = sessionId;
            VisualizationId = visualizationId;
            ColumnId = columnId;
            ContentRevision = contentRevision;
            Coordinates = coordinates;
            Winding = winding;
            Visible = visible;
            Color = new AnatomyBuffer<float>(color);
            Positions = new AnatomyBuffer<float>(positions);
            Normals = new AnatomyBuffer<float>(normals);
            Indices = new AnatomyBuffer<uint>(indices);
            Uvs = new AnatomyBuffer<float>(uvs);
        }

        internal static void ValidateMetadata(string transferId, string sessionId, string visualizationId, string columnId, ulong contentRevision, AnatomyCoordinateSpace coordinates, AnatomyWinding winding, float[] color)
        {
            AnatomySnapshotCodec.ValidateText(transferId);
            AnatomySnapshotCodec.ValidateText(sessionId);
            AnatomySnapshotCodec.ValidateText(visualizationId);
            AnatomySnapshotCodec.ValidateText(columnId);
            if (contentRevision == 0) throw new ArgumentOutOfRangeException(nameof(contentRevision));
            if (coordinates == null) throw new ArgumentNullException(nameof(coordinates));
            if (winding != AnatomyWinding.Clockwise && winding != AnatomyWinding.CounterClockwise)
                throw new ArgumentOutOfRangeException(nameof(winding));
            if (color == null || color.Length != 4) throw new ArgumentException("RGBA requires four components.", nameof(color));
            ValidateFinite(color);
            foreach (float component in color)
                if (component < 0 || component > 1)
                    throw new ArgumentException("RGBA must be in [0,1].");
        }

        private static void ValidateBuffers(float[] positions, float[] normals, uint[] indices, float[] uvs)
        {
            ValidateFinite(positions);
            ValidateFinite(normals);
            ValidateFinite(uvs);
            foreach (uint index in indices)
                if (index >= positions.Length / 3)
                    throw new ArgumentException("Triangle index outside the vertex buffer.");
        }

        private static void ValidateDimensions(float[] positions, float[] normals, uint[] indices, float[] uvs)
        {
            if (positions == null || normals == null || indices == null || uvs == null)
                throw new ArgumentNullException("Surface buffers cannot be null; use an empty UV array when absent.");
            if (positions.Length % 3 != 0 || normals.Length != positions.Length || uvs.Length % 2 != 0)
                throw new ArgumentException("Surface component counts are inconsistent.");
            AnatomySnapshotCodec.ValidateCounts(positions.Length / 3, indices.Length, uvs.Length / 2);
        }

        internal static void ValidateFinite(float[] values)
        {
            foreach (float value in values)
                if (float.IsNaN(value) || float.IsInfinity(value))
                    throw new ArgumentException("Anatomical values must be finite.");
        }
    }
}
