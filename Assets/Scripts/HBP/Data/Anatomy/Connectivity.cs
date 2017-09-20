using System.Runtime.Serialization;
using System.IO;
using System;

namespace HBP.Data.Anatomy
{
    [DataContract]
    public class Connectivity : ICloneable, ICopiable
    {
        #region Properties
        const string EXTENSION = "";
        [DataMember] public string Name { get; set; }
        [DataMember] public string File { get; set; }
        public virtual bool isUsable
        {
            get { return !string.IsNullOrEmpty(Name) && HasConnectivity; }
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
        }
        public Connectivity() : this("New connectivity", string.Empty)
        {
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
    }
}
