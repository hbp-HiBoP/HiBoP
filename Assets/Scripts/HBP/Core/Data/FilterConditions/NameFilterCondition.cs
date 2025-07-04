using HBP.Core.Interfaces;
using Newtonsoft.Json;
using System.ComponentModel;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), DisplayName("Name"), SortingOrder(0), FilterCondition(typeof(INameable), typeof(Object3D.Site))]
    public class NameFilterCondition : BaseFilterCondition
    {
        #region Properties
        public override string Description => $"The name {(IsNot ? (ExactMatch ? "is not exactly" : "does not contain") : (ExactMatch ? "is exactly" : "contains"))} \"{Name}\" (case {(CaseSensitive ? "sensitive" : "insensitive")})";

        [JsonProperty("Name")] public string Name { get; set; }
        [JsonProperty("ExactMatch")] public bool ExactMatch { get; set; }
        [JsonProperty("CaseSensitive")] public bool CaseSensitive { get; set; }
        #endregion

        #region Constructors
        public NameFilterCondition() : this("", false, false, false)
        {
        }
        public NameFilterCondition(string name, bool exactMatch, bool caseSensitive, bool isNot) : base(isNot)
        {
            Name = name;
            ExactMatch = exactMatch;
            CaseSensitive = caseSensitive;
        }
        public NameFilterCondition(string name, bool exactMatch, bool caseSensitive, bool isNot, string ID) : base(isNot, ID)
        {
            Name = name;
            ExactMatch = exactMatch;
            CaseSensitive = caseSensitive;
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new NameFilterCondition(Name, ExactMatch, CaseSensitive, IsNot, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is NameFilterCondition nameFilterCondition)
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
            if (obj is INameable nameable)
            {
                name = nameable.Name;
            }
            if (obj is Object3D.Site site)
            {
                name = site.Information.Name;
            }

            string nameToCompare = Name;

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(nameToCompare))
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