using HBP.Core.Interfaces;
using Newtonsoft.Json;
using System.ComponentModel;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), DisplayName("Name"), PatientFilter]
    public class NameFilterCondition : BaseFilterCondition
    {
        #region Properties
        public override string Description => $"The name {(IsNot ? (ExactMatch ? "is not exactly" : "does not contain") : (ExactMatch ? "is exactly" : "contains"))} \"{Name}\" (case {(CaseSensitive ? "sensitive" : "insensitive")})";

        public string Name { get; set; }
        public bool ExactMatch { get; set; }
        public bool CaseSensitive { get; set; }
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
        public override bool Check(BaseData obj)
        {
            if (obj is INameable nameable)
            {
                string name = nameable.Name;
                string nameToCompare = Name;
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
            return false;
        }
        #endregion
    }
}