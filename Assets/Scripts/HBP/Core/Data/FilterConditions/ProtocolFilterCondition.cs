using HBP.Core.Tools;
using HBP.Data.Database;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TMPro;
using UnityEngine;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), DisplayName("Protocol"), SortingOrder(3), FilterCondition(typeof(Patient))]
    public class ProtocolFilterCondition : BaseFilterCondition
    {
        #region Properties
        public override string Description
        {
            get
            {
                string nameFilter = string.Empty;
                if (!string.IsNullOrEmpty(Name))
                {
                    nameFilter = $" with name {(ExactMatch ? "being exactly" : "containing")} \"{Name}\" (case {(CaseSensitive ? "sensitive" : "insensitive")})";
                }

                if (Protocols == null || Protocols.Count == 0)
                    return (IsNot ? "Has data for any protocol" : "Does not have data for any protocol") + nameFilter;

                string typeText = Logic == CheckLogic.All ? "and" : IsNot ? "nor" : "or";

                List<string> protocolNames = new List<string>();
                if (Protocols.Count > 5)
                {
                    protocolNames.AddRange(Protocols.Take(4).Select(p => p.Name));
                    protocolNames.Add("...");
                    protocolNames.Add(Protocols.Last().Name);
                }
                else
                {
                    protocolNames.AddRange(Protocols.Select(p => p.Name));
                }

                string formattedProtocols;
                if (protocolNames.Count == 1)
                {
                    formattedProtocols = protocolNames[0];
                }
                else if (protocolNames.Count == 2)
                {
                    formattedProtocols = $"{protocolNames[0]} {typeText} {protocolNames[1]}";
                }
                else
                {
                    var allButLast = protocolNames.Take(protocolNames.Count - 1);
                    var last = protocolNames.Last();
                    formattedProtocols = $"{string.Join(", ", allButLast)} {typeText} {last}";
                }

                string scopeText = Scope == CheckScope.Database ? "in database" : "in current project";
                string prefix = IsNot ? $"Does not have data for" : $"Has data for";

                return $"{prefix} {formattedProtocols} {scopeText}{nameFilter}";
            }
        }

        [JsonProperty("Protocols")] public List<Protocol> Protocols { get; set; }

        public enum CheckScope { Database, CurrentProject }
        [JsonProperty("Scope")] public CheckScope Scope { get; set; }

        public enum CheckLogic { All, Any }
        [JsonProperty("Logic")] public CheckLogic Logic { get; set; }

        [JsonProperty("Name")] public string Name { get; set; } = "";
        [JsonProperty("ExactMatch")] public bool ExactMatch { get; set; } = false;
        [JsonProperty("CaseSensitive")] public bool CaseSensitive { get; set; } = false;
        #endregion

        #region Constructors
        public ProtocolFilterCondition() : this(new List<Protocol>(), CheckScope.Database, CheckLogic.All, false, "", false, false)
        {
        }
        public ProtocolFilterCondition(IEnumerable<Protocol> protocols, CheckScope scope, CheckLogic logic, bool isNot, string name = "", bool exactMatch = false, bool caseSensitive = false) : base(isNot)
        {
            Protocols = new List<Protocol>(protocols);
            Scope = scope;
            Logic = logic;
            Name = name;
            ExactMatch = exactMatch;
            CaseSensitive = caseSensitive;
        }
        public ProtocolFilterCondition(IEnumerable<Protocol> protocols, CheckScope scope, CheckLogic logic, bool isNot, string ID, string name = "", bool exactMatch = false, bool caseSensitive = false) : base(isNot, ID)
        {
            Protocols = new List<Protocol>(protocols);
            Scope = scope;
            Logic = logic;
            Name = name;
            ExactMatch = exactMatch;
            CaseSensitive = caseSensitive;
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new ProtocolFilterCondition(Protocols, Scope, Logic, IsNot, ID, Name, ExactMatch, CaseSensitive);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is ProtocolFilterCondition protocolFilterCondition)
            {
                Protocols = new List<Protocol>(protocolFilterCondition.Protocols);
                Scope = protocolFilterCondition.Scope;
                Logic = protocolFilterCondition.Logic;
                Name = protocolFilterCondition.Name;
                ExactMatch = protocolFilterCondition.ExactMatch;
                CaseSensitive = protocolFilterCondition.CaseSensitive;
            }
        }
        #endregion

        #region Public Methods
        public override bool Check(BaseData obj)
        {
            if (obj is Patient patient)
            {
                List<PatientDataInfo> data = (Scope switch
                {
                    CheckScope.Database => DatabaseManager.Database.DataInfos.OfType<PatientDataInfo>().Where(d => d.Patient == patient),
                    CheckScope.CurrentProject => ApplicationState.LoadedProject != null ? ApplicationState.LoadedProject.Datasets.SelectMany(ds => ds.Data).OfType<PatientDataInfo>().Where(d => d.Patient == patient) : new List<PatientDataInfo>(),
                    _ => DatabaseManager.Database.DataInfos.OfType<PatientDataInfo>().Where(d => d.Patient == patient),
                }).ToList();

                if (!string.IsNullOrEmpty(Name))
                {
                    data = data.Where(d =>
                    {
                        string dataName = d.Name;
                        string filterName = Name;
                        if (!CaseSensitive)
                        {
                            dataName = dataName.ToLower();
                            filterName = filterName.ToLower();
                        }
                        if (ExactMatch)
                        {
                            return dataName == filterName;
                        }
                        else
                        {
                            return dataName.Contains(filterName);
                        }
                    }).ToList();
                }

                if (Protocols.Count == 0)
                {
                    return data.Count > 0 != IsNot;
                }
                return Logic switch
                {
                    CheckLogic.All => (data.Count > 0 && Protocols.All(p => data.Any(d => d.Protocol == p))) != IsNot,
                    CheckLogic.Any => (data.Count > 0 && Protocols.Any(p => data.Any(d => d.Protocol == p))) != IsNot,
                    _ => false,
                };
            }
            return false;
        }
        #endregion
    }
}