namespace HBP.Module3D
{
    /// <summary>
    /// Class containing information about how to render the MRI
    /// </summary>
    public struct MRICalValues
    {
        /// <summary>
        /// Minimum value of a voxel of the MRI
        /// </summary>
        public float Min { get; set; }
        /// <summary>
        /// Maximum value of a voxel of the MRI
        /// </summary>
        public float Max { get; set; }
        /// <summary>
        /// Loaded minimum calibration value
        /// </summary>
        public float LoadedCalMin { get; set; }
        /// <summary>
        /// Loaded maximum calibration value
        /// </summary>
        public float LoadedCalMax { get; set; }
        /// <summary>
        /// Recomputed minimum calibration value (used if value could not be loaded properly)
        /// </summary>
        public float ComputedCalMin { get; set; }
        /// <summary>
        /// Recomputed maximum calibration value (used if value could not be loaded properly)
        /// </summary>
        public float ComputedCalMax { get; set; }
    }
}