using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), DisplayName("Date"), PatientFilter]
    public class DateFilterCondition : BaseFilterCondition
    {
        #region Properties
        override public string Description => "";
        public List<int> Dates { get; set; }
        #endregion

        #region Constructors
        public DateFilterCondition() : this(new List<int>(), false)
        {
        }
        public DateFilterCondition(IEnumerable<int> dates, bool isNot) : base(isNot)
        {
            Dates = dates.ToList();
        }
        public DateFilterCondition(IEnumerable<int> dates, bool isNot, string ID) : base(isNot, ID)
        {
            Dates = dates.ToList();
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new DateFilterCondition(Dates, IsNot, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is DateFilterCondition dateFilterCondition)
            {
                Dates = new List<int>(dateFilterCondition.Dates);
            }
        }
        #endregion
    }
}