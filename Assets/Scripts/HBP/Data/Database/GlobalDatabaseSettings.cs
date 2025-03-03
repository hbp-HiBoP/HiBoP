using HBP.Core.Data;
using HBP.Core.Tools;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace HBP.Data.Database
{
    [JsonObject(MemberSerialization.OptIn)]
    public class GlobalDatabaseSettings : BaseData
    {
        #region Properties
        public static string PATH = Path.Combine(ApplicationState.DatabasePath, "Settings.json");

        [JsonProperty] public bool IsFirstUse { get; set; }

        [JsonProperty] private List<Workspace> m_Workspaces = new();
        public List<Workspace> Workspaces => m_Workspaces;

        [JsonProperty] private string m_SelectedWorkspaceID;
        private Workspace m_SelectedWorkspace;
        public Workspace SelectedWorkspace
        {
            get
            {
                return m_SelectedWorkspace;
            }
            set
            {
                m_SelectedWorkspace = value;
            }
        }
        #endregion

        #region Constructors
        public GlobalDatabaseSettings(bool isFirstUse, IEnumerable<Workspace> workspaces, Workspace selectedWorkspace, string ID) : base(ID)
        {
            IsFirstUse = isFirstUse;
            m_Workspaces = workspaces.ToList();
            SelectedWorkspace = selectedWorkspace;
        }
        public GlobalDatabaseSettings(bool isFirstUse, IEnumerable<Workspace> workspaces, Workspace selectedWorkspace) : base()
        {
            IsFirstUse = isFirstUse;
            m_Workspaces = workspaces.ToList();
            SelectedWorkspace = selectedWorkspace;
        }
        public GlobalDatabaseSettings() : this(true, new List<Workspace>(), null)
        {
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new GlobalDatabaseSettings(IsFirstUse, m_Workspaces, SelectedWorkspace, ID);
        }
        public override void Copy(object obj)
        {
            base.Copy(obj);
            if (obj is GlobalDatabaseSettings globalDatabaseSettings)
            {
                IsFirstUse = globalDatabaseSettings.IsFirstUse;
                m_Workspaces = globalDatabaseSettings.m_Workspaces;
                SelectedWorkspace = globalDatabaseSettings.SelectedWorkspace;
            }
        }
        #endregion

        #region Public Methods
        public void SetDefaultWorkspace()
        {
            Workspace workspace = new Workspace("Default");
            m_Workspaces.Add(workspace);
            SelectedWorkspace = workspace;
        }
        public void SetWorkspaces(IEnumerable<Workspace> workspaces)
        {
            m_Workspaces = workspaces.ToList();
        }
        #endregion

        #region Serialization
        protected override void OnSerializing()
        {
            base.OnSerializing();
            m_SelectedWorkspaceID = m_SelectedWorkspace?.ID;
        }
        protected override void OnDeserialized()
        {
            base.OnDeserialized();
            SelectedWorkspace = m_Workspaces.FirstOrDefault(w => w.ID == m_SelectedWorkspaceID);
        }
        #endregion
    }
}