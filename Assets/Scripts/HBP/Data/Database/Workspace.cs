using HBP.Core.Data;
using HBP.Core.Interfaces;
using HBP.Core.Tools;
using Newtonsoft.Json;

namespace HBP.Data.Database
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Workspace : BaseData, INameable
    {
        #region Properties
        [JsonProperty] public string Name { get; set; }
        public string Path => System.IO.Path.Join(ApplicationState.DatabasePath, "Workspaces", ID);
        #endregion

        #region Constructors
        public Workspace(string name, string ID) : base(ID)
        {
            Name = name;
        }
        public Workspace(string name) : base()
        {
            Name = name;
        }
        public Workspace() : this("")
        {
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new Workspace(Name, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is Workspace workspace)
            {
                Name = workspace.Name;
            }
        }
        #endregion
    }
}