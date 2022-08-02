namespace HBP.Data.Preferences
{
    public class TrialMatrixSettings
    {
        #region Properties
        public enum TrialsSynchronizationType { Disable, Enable }
        public enum TrialMatrixType { Simplified, Complete }
        public enum SmoothingType { None, Trial }
        public enum NormalizationType { None, Trial, Bloc, Protocol }
        public enum BlocFormatType { ConstantTrial, TrialRatio, BlocRatio }

        /// <summary>
        /// Type of trial matrix smoothing.
        /// </summary>
        public SmoothingType Smoothing { get; set; }
        /// <summary>
        /// Type of Baseline matrix.
        /// </summary>
        public NormalizationType Normalization { get; set; }
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
        /// <summary>
        /// Synchronization of the trials beetween sites for the same bloc.
        /// </summary>
        public TrialsSynchronizationType TrialsSynchronization { get; set; }
        /// <summary>
        /// Display all blocs or just the visualized blocs.
        /// </summary>
        public TrialMatrixType Type{ get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Create a new trial matrix settings instance.
        /// </summary>
        /// <param name="smoothing">Smoothing type.</param>
        /// <param name="baseline">Baseline type.</param>
        /// <param name="blocformat">Blocformat type.</param>
        /// <param name="constantLineHeight">Constant height of a line in a trial matrix.</param>
        /// <param name="lineHeightByWidth">Ratio height by width of a line in a trial matrix.</param>
        /// <param name="heightByWidth">Ratio height by width of a bloc in a trial matrix.</param>
        public TrialMatrixSettings(SmoothingType smoothing = SmoothingType.Trial,NormalizationType baseline = NormalizationType.Protocol,BlocFormatType blocformat = BlocFormatType.TrialRatio,int constantLineHeight = 3,float lineHeightByWidth = 0.05f,float heightByWidth = 0.3f, TrialsSynchronizationType trialsSynchronization = TrialsSynchronizationType.Enable, TrialMatrixType type = TrialMatrixType.Complete)
        {
            Smoothing = smoothing;
            Normalization = baseline;
            BlocFormat = blocformat;
            ConstantLineHeight = constantLineHeight;
            LineHeightByWidth = lineHeightByWidth;
            HeightByWidth = heightByWidth;
            TrialsSynchronization = trialsSynchronization;
            Type = type;
        }
        #endregion

        #region Public Methods
        public void SetBaseline(NormalizationType type)
        {
            if (Normalization != type)
            {
                if (ApplicationState.Module3D.Scenes.Count > 0)
                {
                    ApplicationState.DialogBoxManager.Open(Tools.Unity.DialogBoxManager.AlertType.WarningMultiOptions, "Baseline settings changed", "You need to reload the open visualizations in order to apply the changes made to the baseline normalization.\n\nWould you like to reload ?", () =>
                    {
                        Normalization = type;
                        Tools.Unity.ClassLoaderSaver.SaveToJSon(ApplicationState.UserPreferences, Core.Data.Preferences.UserPreferences.PATH, true);
                        ApplicationState.Module3D.ReloadScenes();
                    });
                }
                else
                {
                    Normalization = type;
                }
            }
        }
        #endregion
    }
}