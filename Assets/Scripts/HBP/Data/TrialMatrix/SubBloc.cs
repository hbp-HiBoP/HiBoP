namespace HBP.Data.TrialMatrix
{
    public class SubBloc
    {
        #region Properties
        public Experience.Protocol.SubBloc SubBlocProtocol { get; set; }
        public SubTrial[] SubTrials { get; set; }
        public int SpacesBefore { get; set; }
        public int SpacesAfter { get; set; }
        #endregion

        #region Constructor
        public SubBloc(Experience.Protocol.SubBloc subBlocProtocol, SubTrial[] subTrials)
        {
            SubBlocProtocol = subBlocProtocol;
            SubTrials = subTrials;
        }
        #endregion

        #region Public Methods
        public SubTrial[] GetSubTrials(int[] subTrials)
        {
            SubTrial[] result = new SubTrial[subTrials.Length];
            for (int i = 0; i < subTrials.Length; i++)
            {
                result[i] = SubTrials[subTrials[i]];
            }
            return result;
        }
        #endregion
    }
}