using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), DisplayName("Date"), SortingOrder(3), FilterCondition(typeof(Patient))]
    public class DateFilterCondition : BaseFilterCondition
    {
        #region Properties
        public override string Description
        {
            get
            {
                if (Dates == null || Dates.Count == 0)
                    return "The date is anything";

                List<string> displayDates = new List<string>();
                if (Dates.Count > 5)
                {
                    var sortedDates = Dates.OrderBy(d => d).ToList();
                    displayDates.AddRange(sortedDates.Take(4).Select(d => d.ToString()));
                    displayDates.Add("...");
                    displayDates.Add(sortedDates.Last().ToString());
                }
                else
                {
                    displayDates.AddRange(Dates.OrderBy(d => d).Select(d => d.ToString()));
                }


                string formattedDates;
                if (displayDates.Count == 1)
                {
                    formattedDates = displayDates[0];
                }
                else if (displayDates.Count == 2)
                {
                    formattedDates = $"{displayDates[0]} {(IsNot ? "nor" : "or")} {displayDates[1]}";
                }
                else
                {
                    var allButLast = displayDates.Take(displayDates.Count - 1);
                    var last = displayDates.Last();
                    formattedDates = $"{string.Join(", ", allButLast)} {(IsNot ? "nor" : "or")} {last}";
                }

                return displayDates.Count > 1
                    ? $"The date {(IsNot ? "is neither" : "is either")} {formattedDates}"
                    : $"The date {(IsNot ? "is not" : "is")} {formattedDates}";
            }
        }
        [JsonProperty("Dates")] public List<int> Dates { get; set; }
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

        #region Public Methods
        public override bool Check(object obj)
        {
            if (obj is Patient patient)
            {
                if (Dates != null && Dates.Count == 0)
                    return true;

                return IsNot ? !Dates.Contains(patient.Date) : Dates.Contains(patient.Date);
            }
            return false;
        }
        #endregion
    }
}