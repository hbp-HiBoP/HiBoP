using System;
using HBP.Core.Enums;
using UnityEngine;

namespace HBP.Core.DLL
{
    public sealed class LocalizerExportGridSettings
    {
        public const int DefaultMaximumDimension = 80;
        public const int MinimumMaximumDimension = 2;
        public const int MaximumAllowedDimension = 512;
        public const long LargeExportVoxelCount = 8_000_000;
        public const VolumeInterpolation DefaultInterpolation = VolumeInterpolation.Trilinear;

        public int MaximumDimension { get; }
        public VolumeInterpolation Interpolation { get; }

        public LocalizerExportGridSettings(int maximumDimension, VolumeInterpolation interpolation = DefaultInterpolation)
        {
            if (maximumDimension < MinimumMaximumDimension || maximumDimension > MaximumAllowedDimension)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumDimension));
            }

            if (!Enum.IsDefined(typeof(VolumeInterpolation), interpolation))
            {
                throw new ArgumentOutOfRangeException(nameof(interpolation));
            }

            MaximumDimension = maximumDimension;
            Interpolation = interpolation;
        }

        public Vector3Int CalculateDimensions(Vector3Int referenceDimensions)
        {
            if (referenceDimensions.x < 2 || referenceDimensions.y < 2 || referenceDimensions.z < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(referenceDimensions));
            }

            int largestReferenceDimension = Math.Max(referenceDimensions.x, Math.Max(referenceDimensions.y, referenceDimensions.z));
            return new Vector3Int(CalculateDimension(referenceDimensions.x, largestReferenceDimension), CalculateDimension(referenceDimensions.y, largestReferenceDimension), CalculateDimension(referenceDimensions.z, largestReferenceDimension));
        }

        public long CalculateVoxelCount(Vector3Int referenceDimensions)
        {
            Vector3Int dimensions = CalculateDimensions(referenceDimensions);
            return checked((long)dimensions.x * dimensions.y * dimensions.z);
        }

        public bool RequiresLargeExportConfirmation(Vector3Int referenceDimensions)
        {
            return CalculateVoxelCount(referenceDimensions) > LargeExportVoxelCount;
        }

        private int CalculateDimension(int referenceDimension, int largestReferenceDimension)
        {
            return Math.Max(2, (int)((float)MaximumDimension * referenceDimension / largestReferenceDimension));
        }
    }
}
