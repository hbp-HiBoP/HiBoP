namespace HBP.Data.Informations.TrialMatrix
{
    public class SubBloc
    {
        #region Properties
        public bool IsFiller
        {
            get
            {
                return SubBlocProtocol == null || SubTrials.Length == 0 || Window == default(Core.Tools.TimeWindow);
            }
        }
        public Core.Data.SubBloc SubBlocProtocol { get; set; }
        public SubTrial[] SubTrials { get; set; }
        public Core.Tools.TimeWindow Window { get; set; }
        #endregion

        #region Constructor
        public SubBloc(Core.Data.SubBloc subBlocProtocol, SubTrial[] subTrials, Core.Tools.TimeWindow window)
        {
            SubBlocProtocol = subBlocProtocol;
            SubTrials = subTrials;
            Window = window;
        }
        public SubBloc(Core.Data.SubBloc subBlocProtocol, SubTrial[] subTrials) : this(subBlocProtocol, subTrials, default(Core.Tools.TimeWindow))
        {
        }
        public SubBloc(Core.Tools.TimeWindow window) : this(null, new SubTrial[0], window)
        {
        }
        public SubBloc() : this(null, new SubTrial[0])
        {

        }
        #endregion
    }
}