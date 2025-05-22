using Newtonsoft.Json;
using System.ComponentModel;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), DisplayName("Patient name"), SortingOrder(1), FilterCondition(typeof(Object3D.Site))]
    public class PatientNameFilterCondition : BaseFilterCondition
    {
        #region Properties
        public override string Description => $"The name of the patient {(IsNot ? (ExactMatch ? "is not exactly" : "does not contain") : (ExactMatch ? "is exactly" : "contains"))} \"{Name}\" (case {(CaseSensitive ? "sensitive" : "insensitive")})";

        [JsonProperty("Name")] public string Name { get; set; }
        [JsonProperty("ExactMatch")] public bool ExactMatch { get; set; }
        [JsonProperty("CaseSensitive")] public bool CaseSensitive { get; set; }
        #endregion

        #region Constructors
        public PatientNameFilterCondition() : this("", false, false, false)
        {
        }
        public PatientNameFilterCondition(string name, bool exactMatch, bool caseSensitive, bool isNot) : base(isNot)
        {
            Name = name;
            ExactMatch = exactMatch;
            CaseSensitive = caseSensitive;
        }
        public PatientNameFilterCondition(string name, bool exactMatch, bool caseSensitive, bool isNot, string ID) : base(isNot, ID)
        {
            Name = name;
            ExactMatch = exactMatch;
            CaseSensitive = caseSensitive;
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new PatientNameFilterCondition(Name, ExactMatch, CaseSensitive, IsNot, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is PatientNameFilterCondition nameFilterCondition)
            {
                Name = nameFilterCondition.Name;
                ExactMatch = nameFilterCondition.ExactMatch;
                CaseSensitive = nameFilterCondition.CaseSensitive;
            }
        }
        #endregion

        #region Public Methods
        public override bool Check(object obj)
        {
            string name = "";
            if (obj is Object3D.Site site)
            {
                name = site.Information.Patient.Name;
            }

            string nameToCompare = Name;

            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(nameToCompare))
                return false;

            if (!CaseSensitive)
            {
                name = name.ToLower();
                nameToCompare = nameToCompare.ToLower();
            }
            if (ExactMatch)
            {
                return IsNot ? name != nameToCompare : name == nameToCompare;
            }
            else
            {
                return IsNot ? !name.Contains(nameToCompare) : name.Contains(nameToCompare);
            }
        }
        #endregion
    }
}