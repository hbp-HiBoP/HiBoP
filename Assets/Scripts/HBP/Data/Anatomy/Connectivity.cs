using System.Runtime.Serialization;
using System.IO;
using System;

namespace HBP.Data.Anatomy
{
    [DataContract]
    public class Connectivity : ICloneable, ICopiable
    {
        #region Properties
        const string EXTENSION = ".txt";
        [DataMember] public string Name { get; set; }
        [DataMember] public string File { get; set; }
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
        }
        #endregion

        #region Serialization
        [OnDeserialized()]
        public void OnDeserialized(StreamingContext context)
        {
            RecalculateUsable();
        }
        #endregion
    }
}
