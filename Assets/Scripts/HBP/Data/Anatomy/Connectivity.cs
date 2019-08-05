using System.Runtime.Serialization;
using System.IO;
using System;
using Tools.Unity;

namespace HBP.Data.Anatomy
{
    [DataContract]
    public class Connectivity : ICloneable, ICopiable, IIdentifiable
    {
        #region Properties
        const string EXTENSION = ".txt";
        [DataMember] public string ID { get; set; }
        [DataMember] public string Name { get; set; }
        [DataMember] string m_File;

        public string File
        {
            get
            {
                return m_File.ConvertToFullPath();
            }
            set
            {
                m_File = value.ConvertToShortPath();
            }
        }
        public string SavedFile { get { return m_File; } }
        protected bool m_WasUsable;
        public bool WasUsable
        {
            get
            {
                return m_WasUsable;
            }
        }
        public bool Usable
        {
            get
            {
                bool usable = !string.IsNullOrEmpty(Name) && HasConnectivity;
                m_WasUsable = usable;
                return usable;
            }
        }
        public virtual bool HasConnectivity
        {
            get
            {
                return !string.IsNullOrEmpty(File) && System.IO.File.Exists(File) && (new FileInfo(File).Extension == EXTENSION);
            }
        }
        #endregion

        #region Constructor
        public Connectivity(string name, string file, string id)
        {
            Name = name;
            File = file;
            RecalculateUsable();
        }
        public Connectivity(string name, string file) : this(name, file, Guid.NewGuid().ToString())
        {

        }
        public Connectivity() : this("New connectivity", string.Empty)
        {
        }
        #endregion

        #region Public Methods
        public bool RecalculateUsable()
        {
            return Usable;
        }
        #endregion

        #region Operators
        public override bool Equals(object obj)
        {
            Connectivity connectivity = obj as Connectivity;
            if (connectivity != null && connectivity.ID == ID)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public override int GetHashCode()
        {
            return this.ID.GetHashCode();
        }
        public static bool operator ==(Connectivity a, Connectivity b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            return a.Equals(b);
        }
        public static bool operator !=(Connectivity a, Connectivity b)
        {
            return !(a == b);
        }
        public virtual void GenerateNewIDs()
        {
            ID = Guid.NewGuid().ToString();
        }
        public object Clone()
        {
            return new Connectivity(Name, File, ID);
        }
        public void Copy(object copy)
        {
            Connectivity connectivity = copy as Connectivity;
            Name = connectivity.Name;
            File = connectivity.File;
            ID = connectivity.ID;
            RecalculateUsable();
        }
        #endregion

        #region Serialization
        [OnDeserialized()]
        public void OnDeserialized(StreamingContext context)
        {
            m_File = m_File.ToPath();
            RecalculateUsable();
        }
        #endregion
    }
}
