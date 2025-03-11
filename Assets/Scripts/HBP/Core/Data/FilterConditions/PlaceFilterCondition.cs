using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), DisplayName("Place"), PatientFilter]
    public class PlaceFilterCondition : BaseFilterCondition
    {
        #region Properties
        public override string Description
        {
            get
            {
                if (Places == null || Places.Count == 0)
                    return IsNot ? "Always true" : "Always false";

                List<string> displayPlaces = new List<string>();
                if (Places.Count > 5)
                {
                    displayPlaces.AddRange(Places.Take(4));
                    displayPlaces.Add("...");
                    displayPlaces.Add(Places.Last());
                }
                else
                {
                    displayPlaces.AddRange(Places);
                }

                string formattedPlaces;
                if (displayPlaces.Count == 1)
                {
                    formattedPlaces = displayPlaces[0];
                }
                else if (displayPlaces.Count == 2)
                {
                    formattedPlaces = $"{displayPlaces[0]} {(IsNot ? "nor" : "or")} {displayPlaces[1]}";
                }
                else
                {
                    var allButLast = displayPlaces.Take(displayPlaces.Count - 1);
                    var last = displayPlaces.Last();
                    formattedPlaces = $"{string.Join(", ", allButLast)} {(IsNot ? "nor" : "or")} {last}";
                }

                return displayPlaces.Count > 1
                    ? $"The place {(IsNot ? "is neither" : "is either")} {formattedPlaces}"
                    : $"The place {(IsNot ? "is not" : "is")} {formattedPlaces}";
            }
        }

        public List<string> Places { get; set; } = new();
        #endregion

        #region Constructors
        public PlaceFilterCondition() : this(new List<string>(), false)
        {
        }
        public PlaceFilterCondition(List<string> places, bool isNot) : base(isNot)
        {
            Places = places;
        }
        public PlaceFilterCondition(List<string> places, bool isNot, string ID) : base(isNot, ID)
        {
            Places = places;
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new PlaceFilterCondition(new List<string>(Places), IsNot, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is PlaceFilterCondition placeFilterCondition)
            {
                Places = new List<string>(placeFilterCondition.Places);
            }
        }
        #endregion

        #region Public Methods
        public override bool Check(BaseData obj)
        {
            if (obj is Patient patient)
            {
                return IsNot ? !Places.Contains(patient.Place) : Places.Contains(patient.Place);
            }
            return true;
        }
        #endregion
    }
}