using Newtonsoft.Json;
using System.ComponentModel;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), DisplayName("State"), SortingOrder(3), FilterCondition(typeof(DataInfo))]
    public class DataStateFilterCondition : BaseFilterCondition
    {
        #region Properties
        [JsonProperty("State")] public DataInfo.DataState State { get; set; }

        public override string Description
        {
            get
            {
                return State switch
                {
                    DataInfo.DataState.Ok => $"Data is{(IsNot ? " not " : " ")}OK",
                    DataInfo.DataState.Warning => $"Data is{(IsNot ? " not " : " ")}in warning state",
                    DataInfo.DataState.Error => $"Data is{(IsNot ? " not " : " ")}in error state",
                    _ => $"Data is{(IsNot ? " not " : " ")}in unknown state",
                };
            }
        }
        #endregion

        #region Constructors
        public DataStateFilterCondition() : this(DataInfo.DataState.Ok, false) { }
        public DataStateFilterCondition(DataInfo.DataState state, bool isNot) : base(isNot)
        {
            State = state;
        }
        public DataStateFilterCondition(DataInfo.DataState state, bool isNot, string ID) : base(isNot, ID)
        {
            State = state;
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new DataStateFilterCondition(State, IsNot, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is DataStateFilterCondition dataStateFilterCondition)
            {
                State = dataStateFilterCondition.State;
            }
        }
        #endregion

        #region Public Methods
        public override bool Check(object obj)
        {
            if (obj is DataInfo dataInfo)
            {
                return (dataInfo.State == State) != IsNot;
            }
            return false;
        }
        #endregion
    }
}