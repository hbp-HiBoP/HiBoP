using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace HBP.Transfer.Anatomy
{
    /// <summary>Prepared scientific inputs. NIfTI bytes preserve the native affine; no sender path travels.</summary>
    public sealed class AnatomyProjection
    {
        public AnatomyBuffer<byte> VolumeBytes { get; }
        public AnatomyBuffer<byte> VolumeHash { get; }
        public int GridDimension { get; }
        public int Interpolation { get; }
        public float InfluenceDistance { get; }
        public int InfluenceByDistance { get; }
        public float ActivityAlpha { get; }
        public int[] Dimensions => new[] { VolumeBytes[42] | VolumeBytes[43] << 8, VolumeBytes[44] | VolumeBytes[45] << 8, VolumeBytes[46] | VolumeBytes[47] << 8 };

        public AnatomyProjection(byte[] volumeBytes, int gridDimension, int interpolation, float influenceDistance, int influenceByDistance, float activityAlpha)
        {
            if (volumeBytes == null) throw new ArgumentNullException(nameof(volumeBytes), "A projection requires its reference volume.");
            ValidateNifti(volumeBytes);
            if (gridDimension < 2 || interpolation < 0 || interpolation > 1 || influenceByDistance < 0 || influenceByDistance > 2 || float.IsNaN(influenceDistance) || float.IsInfinity(influenceDistance) || influenceDistance < 0 || influenceDistance > 50 || float.IsNaN(activityAlpha) || activityAlpha < 0 || activityAlpha > 1)
                throw new ArgumentException("Invalid projection grid, influence or opacity settings.");
            VolumeBytes = new AnatomyBuffer<byte>((byte[])volumeBytes.Clone());
            using var sha = SHA256.Create();
            VolumeHash = new AnatomyBuffer<byte>(sha.ComputeHash(volumeBytes));
            GridDimension = gridDimension;
            Interpolation = interpolation;
            InfluenceDistance = influenceDistance;
            InfluenceByDistance = influenceByDistance;
            ActivityAlpha = activityAlpha;
        }

        internal void Validate(AnatomyCoordinateSpace coordinates, AnatomyWinding winding, AnatomyContacts contacts)
        {
            float[] identity = { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };
            if (coordinates.FrameId != AnatomyContacts.FrameId || coordinates.MappingVersion != 1 || coordinates.Unit != AnatomyLengthUnit.Millimeter || coordinates.Handedness != AnatomyHandedness.Left || !coordinates.AssetToBrain.AsReadOnlySpan().SequenceEqual(identity) || winding != AnatomyWinding.Clockwise || contacts.Implantation != "MNI")
                throw new ArgumentException("Projection requires the prepared MNI Unity millimeter frame and clockwise surface.");
            foreach (var site in contacts.Sites)
            {
                var flags = site.Flags;
                bool expected = (flags & (AnatomySiteFlags.Masked | AnatomySiteFlags.Blacklisted)) != 0 || (contacts.RoiActive && (flags & AnatomySiteFlags.OutOfRoi) != 0) || (flags & AnatomySiteFlags.Filtered) == 0;
                if (site.EffectiveMasked != expected) throw new ArgumentException("Effective site mask disagrees with the prepared source flags.");
            }
        }

        // This first projection contract admits single-file, little-endian NIfTI-1 scalar 3D volumes.
        // Unsupported formats fail explicitly; no decompression, resampling or scientific conversion.
        private static void ValidateNifti(byte[] bytes)
        {
            if (bytes.Length < 352 || bytes.Length > AnatomySnapshotCodec.MaximumEncodedBytes || BitConverter.ToInt32(bytes, 0) != 348 || bytes[344] != 'n' || bytes[345] != '+' || bytes[346] != '1' || bytes[347] != 0 || BitConverter.ToInt16(bytes, 40) != 3)
                throw new ArgumentException("Projection requires a complete uncompressed little-endian NIfTI-1 3D .nii volume; reload a supported reference volume.");
            int type = BitConverter.ToInt16(bytes, 70);
            int bits = type switch { 2 or 256 => 8, 4 or 512 => 16, 8 or 16 or 768 => 32, 64 or 1024 or 1280 => 64, _ => 0 };
            long voxels = 1;
            for (int axis = 0; axis < 3; axis++)
            {
                int size = BitConverter.ToInt16(bytes, 42 + 2 * axis);
                float spacing = BitConverter.ToSingle(bytes, 80 + 4 * axis);
                if (size < 1 || float.IsNaN(spacing) || float.IsInfinity(spacing) || spacing <= 0) throw new ArgumentException("Invalid volume dimensions or spacing.");
                voxels *= size;
            }

            float offset = BitConverter.ToSingle(bytes, 108);
            if (bits == 0 || bits != BitConverter.ToInt16(bytes, 72) || float.IsNaN(offset) || offset < 352 || offset > bytes.Length || offset != Math.Floor(offset) || (long)offset + voxels * (bits / 8) != bytes.Length)
                throw new ArgumentException("NIfTI voxel type, dimensions and exact byte length disagree.");
            foreach (int field in new[] { 112, 116, 256, 260, 264, 268, 272, 276, 280, 284, 288, 292, 296, 300, 304, 308, 312, 316, 320, 324 })
            {
                float value = BitConverter.ToSingle(bytes, field);
                if (float.IsNaN(value) || float.IsInfinity(value)) throw new ArgumentException("Non-finite NIfTI spatial/scaling metadata.");
            }
        }
    }

    internal static class AnatomyProjectionCodec
    {
        internal static long Length(AnatomyProjection projection) => 56L + projection.VolumeBytes.Count;

        internal static void Write(BinaryWriter writer, AnatomyProjection projection)
        {
            writer.Write(projection.GridDimension);
            writer.Write(projection.Interpolation);
            writer.Write(projection.InfluenceDistance);
            writer.Write(projection.InfluenceByDistance);
            writer.Write(projection.ActivityAlpha);
            writer.Write(projection.VolumeBytes.Count);
            writer.Write(projection.VolumeHash.ToArray());
            writer.Write(projection.VolumeBytes.ToArray());
        }

        internal static AnatomyProjection Read(BinaryReader reader, long end)
        {
            if (end - reader.BaseStream.Position < 56) throw new InvalidDataException("Missing projection volume.");
            int grid = reader.ReadInt32(), interpolation = reader.ReadInt32();
            float influence = reader.ReadSingle();
            int rule = reader.ReadInt32();
            float alpha = reader.ReadSingle();
            int length = reader.ReadInt32();
            if (length < 352 || reader.BaseStream.Position + 32L + length != end) throw new InvalidDataException("Invalid projection volume length.");
            byte[] hash = reader.ReadBytes(32), volume = reader.ReadBytes(length);
            using var sha = SHA256.Create();
            if (!sha.ComputeHash(volume).SequenceEqual(hash)) throw new InvalidDataException("Reference volume SHA-256 mismatch.");
            return new AnatomyProjection(volume, grid, interpolation, influence, rule, alpha);
        }
    }
}
