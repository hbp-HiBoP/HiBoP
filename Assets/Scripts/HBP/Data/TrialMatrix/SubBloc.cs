namespace HBP.Data.TrialMatrix
{
    public class SubBloc
    {
        #region Properties
        public bool IsFiller
        {
            get
            {
                return SubBlocProtocol == null || SubTrials.Length == 0 || Window == default(Tools.CSharp.Window);
            }
        }
        public Experience.Protocol.SubBloc SubBlocProtocol { get; set; }
        public SubTrial[] SubTrials { get; set; }
        public Tools.CSharp.Window Window { get; set; }
        #endregion

        #region Constructor
        public SubBloc(Experience.Protocol.SubBloc subBlocProtocol, SubTrial[] subTrials, Tools.CSharp.Window window)
        {
            SubBlocProtocol = subBlocProtocol;
            SubTrials = subTrials;
            Window = window;
        }
        public SubBloc(Experience.Protocol.SubBloc subBlocProtocol, SubTrial[] subTrials) : this(subBlocProtocol, subTrials, default(Tools.CSharp.Window))
        {
            SubBlocProtocol = subBlocProtocol;
            SubTrials = subTrials;
        }
        public SubBloc(Tools.CSharp.Window window) : this(null, new SubTrial[0], window)
        {
        }
        public SubBloc() : this(null, new SubTrial[0])
        {

        }
        #endregion
    }
}