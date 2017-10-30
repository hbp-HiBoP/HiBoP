namespace HBP.Data.Settings
{
    public class TrialMatrixSettings
    {
        #region Properties
        public enum SmoothingType { None, Line }
        public enum BaselineType { None, Line, Bloc, Protocol }
        public enum BlocFormatType { ConstantLine, LineRatio, BlocRatio }

        /// <summary>
        /// Type of trial matrix smoothing.
        /// </summary>
        public SmoothingType Smoothing { get; set; }
        /// <summary>
        /// Type of Baseline matrix.
        /// </summary>
        public BaselineType Baseline { get; set; }
        /// <summary>
        /// Type of bloc format.
        /// </summary>
        public BlocFormatType BlocFormat { get; set; }
        /// <summary>
        /// Constant height of a line in pixels.
        /// </summary>
        public int ConstantLineHeight { get; set; }
        /// <summary>
        /// Ratio beetween height and width of a line.
        /// </summary>
        public float LineHeightByWidth { get; set; }
        /// <summary>
        /// Ratio beetween height and width of a bloc.
        /// </summary>
        public float HeightByWidth { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Create a new trial matrix settings instance with default values.
        /// </summary>
        public TrialMatrixSettings() : this(SmoothingType.Line, BaselineType.Protocol, BlocFormatType.LineRatio, 3, 0.05f, 9.0f / 16.0f)
        {
        }
        /// <summary>
        /// Create a new trial matrix settings instance.
        /// </summary>
        /// <param name="smoothing">Smoothing type.</param>
        /// <param name="Baseline">Baseline type.</param>
        /// <param name="blocformat">Blocformat type.</param>
        /// <param name="constantLineHeight">Constant height of a line in a trial matrix.</param>
        /// <param name="lineHeightByWidth">Ratio height by width of a line in a trial matrix.</param>
        /// <param name="heightByWidth">Ratio height by width of a bloc in a trial matrix.</param>
        public TrialMatrixSettings(SmoothingType smoothing = SmoothingType.Line,BaselineType Baseline = BaselineType.Protocol,BlocFormatType blocformat = BlocFormatType.LineRatio,int constantLineHeight = 3,float lineHeightByWidth = 0.05f,float heightByWidth = 9.0f/16.0f)
        {
            Smoothing = smoothing;
            Baseline = Baseline;
            BlocFormat = blocformat;
            ConstantLineHeight = constantLineHeight;
            LineHeightByWidth = lineHeightByWidth;
            HeightByWidth = heightByWidth;
        }
        #endregion
    }
}