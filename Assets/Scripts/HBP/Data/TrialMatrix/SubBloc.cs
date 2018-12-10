namespace HBP.Data.TrialMatrix
{
    public class SubBloc
    {
        #region Properties
        public Experience.Protocol.SubBloc SubBlocProtocol { get; set; }
        public SubTrial[] SubTrials { get; set; }
        public Tools.CSharp.Window Window { get; set; }
        #endregion

        #region Constructor
        public SubBloc(Experience.Protocol.SubBloc subBlocProtocol, SubTrial[] subTrials)
        {
            SubBlocProtocol = subBlocProtocol;
            SubTrials = subTrials;
        }
        #endregion
    }
}