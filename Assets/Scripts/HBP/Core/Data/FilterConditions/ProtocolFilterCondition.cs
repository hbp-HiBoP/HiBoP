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
    [JsonObject(MemberSerialization.OptIn), DisplayName("Protocol"), SortingOrder(3), PatientFilter]
    public class ProtocolFilterCondition : BaseFilterCondition
    {
        #region Properties
        public override string Description
        {
            get
            {
                if (Protocols == null || Protocols.Count == 0)
                    return IsNot ? "Has data for any protocol" : "Does not have data for any protocol";

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

                return $"{prefix} {formattedProtocols} {scopeText}";
            }
        }
        public List<Protocol> Protocols { get; set; }

        public enum CheckScope { Database, CurrentProject }
        public CheckScope Scope { get; set; }

        public enum CheckLogic { All, Any }
        public CheckLogic Logic { get; set; }
        #endregion

        #region Constructors
        public ProtocolFilterCondition() : this(new List<Protocol>(), CheckScope.Database, CheckLogic.All, false)
        {
        }
        public ProtocolFilterCondition(IEnumerable<Protocol> protocols, CheckScope scope, CheckLogic logic, bool isNot) : base(isNot)
        {
            Protocols = new List<Protocol>(protocols);
            Scope = scope;
            Logic = logic;
        }
        public ProtocolFilterCondition(IEnumerable<Protocol> protocols, CheckScope scope, CheckLogic logic, bool isNot, string ID) : base(isNot, ID)
        {
            Protocols = new List<Protocol>(protocols);
            Scope = scope;
            Logic = logic;
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new ProtocolFilterCondition(Protocols, Scope, Logic, IsNot, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is ProtocolFilterCondition protocolFilterCondition)
            {
                Protocols = new List<Protocol>(protocolFilterCondition.Protocols);
                Scope = protocolFilterCondition.Scope;
                Logic = protocolFilterCondition.Logic;
            }
        }
        #endregion

        #region Public Methods
        public override bool Check(BaseData obj)
        {
            if (obj is Patient patient)
            {
                IEnumerable<PatientDataInfo> data = Scope switch
                {
                    CheckScope.Database => DatabaseManager.Database.DataInfos.OfType<PatientDataInfo>().Where(d => d.Patient == patient),
                    CheckScope.CurrentProject => ApplicationState.LoadedProject.Datasets.SelectMany(ds => ds.Data).OfType<PatientDataInfo>().Where(d => d.Patient == patient),
                    _ => DatabaseManager.Database.DataInfos.OfType<PatientDataInfo>().Where(d => d.Patient == patient),
                };
                if (Protocols.Count == 0)
                {
                    return data.Count() > 0 != IsNot;
                }
                return Logic switch
                {
                    CheckLogic.All => data.All(d => Protocols.Contains(d.Protocol)) != IsNot,
                    CheckLogic.Any => data.Any(d => Protocols.Contains(d.Protocol)) != IsNot,
                    _ => false,
                };
            }
            return false;
        }
        #endregion
    }
}