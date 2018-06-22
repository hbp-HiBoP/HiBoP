using System.Runtime.Serialization;
using System.IO;
using System;
using Tools.Unity;

namespace HBP.Data.Anatomy
{
    [DataContract]
    public class Connectivity : ICloneable, ICopiable
    {
        #region Properties
        const string EXTENSION = ".txt";
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
        public Connectivity(string name, string file)
        {
            Name = name;
            File = file;
            RecalculateUsable();
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
        public object Clone()
        {
            return new Connectivity(Name, File);
        }
        public void Copy(object copy)
        {
            Connectivity connectivity = copy as Connectivity;
            Name = connectivity.Name;
            File = connectivity.File;
            RecalculateUsable();
        }
        #endregion

        #region Serialization
        [OnDeserialized()]
        public void OnDeserialized(StreamingContext context)
        {
            File = File;
            RecalculateUsable();
        }
        #endregion
    }
}
