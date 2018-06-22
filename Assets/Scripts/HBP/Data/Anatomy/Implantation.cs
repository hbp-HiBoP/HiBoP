using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using Tools.Unity;

namespace HBP.Data.Anatomy
{
    [DataContract]
    public class Implantation : ICloneable, ICopiable
    {
        #region Properties
        const string HEADER = "ptsfile";
        public const string EXTENSION = ".pts";
        public const string BIDS_EXTENSION = ".tsv";
        public const string MARS_ATLAS_EXTENSION = ".csv";
        public enum Error { None, PathIsNullOrEmpty, FileNotFound, WrongExtension, CannotReadFile, WrongFormat, CannotReadAllSites };
        [DataMember] public string Name { get; set; }
        [DataMember(Name = "File")] string m_File;
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
        [DataMember(Name = "MarsAtlas")] string m_MarsAtlas;
        public string MarsAtlas
        {
            get
            {
                return m_MarsAtlas.ConvertToFullPath();
            }
            set
            {
                m_MarsAtlas = value.ConvertToShortPath();
            }
        }
        [IgnoreDataMember] public List<Site> Sites { get; set; }
        [IgnoreDataMember] public Brain Brain { get; set; }
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
                bool usable = !string.IsNullOrEmpty(Name) && HasImplantation;
                m_WasUsable = usable;
                return usable;
            }
        }
        public virtual bool HasImplantation
        {
            get
            {
                return !string.IsNullOrEmpty(File) && System.IO.File.Exists(File) && (new FileInfo(File).Extension == EXTENSION || new FileInfo(File).Extension == BIDS_EXTENSION);
            }
        }
        public virtual bool HasMarsAtlas
        {
            get
            {
                return !string.IsNullOrEmpty(MarsAtlas) && System.IO.File.Exists(MarsAtlas) && (new FileInfo(MarsAtlas).Extension == MARS_ATLAS_EXTENSION);
            }
        }
        #endregion

        #region Constructor
        public Implantation(string name, string path, string marsAtlas)
        {
            Name = name;
            File = path;
            MarsAtlas = marsAtlas;
            Sites = new List<Site>();
            RecalculateUsable();
        }
        public Implantation() : this("New implantation", string.Empty, string.Empty)
        {
        }
        #endregion

        #region Public Methods
        public bool RecalculateUsable()
        {
            return Usable;
        }
        public Error Load()
        {
            if (string.IsNullOrEmpty(File)) return Error.PathIsNullOrEmpty;
            FileInfo fileInfo = new FileInfo(File);
            if (!fileInfo.Exists) return Error.FileNotFound;
            if (fileInfo.Extension != EXTENSION) return Error.WrongExtension;
            string[] lines = new string[0];
            try
            {
                lines = System.IO.File.ReadAllLines(fileInfo.FullName);
            }
            catch
            {
                return Error.CannotReadFile;
            }
            if (!IsCorrect(lines)) return Error.WrongFormat;
            IEnumerable<Site> sites;
            if (!ReadSites(lines, out sites)) return Error.CannotReadAllSites;
            Sites = sites.ToList();
            return Error.None;
        }
        public void Unload()
        {
            Sites = new List<Site>();
        }
        public static Implantation[] GetImplantations(string path)
        {
            //UnityEngine.Profiling.Profiler.BeginSample("GetImplantations");
            List<Implantation> implantations = new List<Implantation>();
            DirectoryInfo patientDirectory = new DirectoryInfo(path);
            DirectoryInfo implantationDirectory = new DirectoryInfo(path + Path.DirectorySeparatorChar + "implantation");
            if (implantationDirectory.Exists)
            {
                FileInfo marsAtlasFile = new FileInfo(implantationDirectory.FullName + Path.DirectorySeparatorChar + patientDirectory.Name + MARS_ATLAS_EXTENSION);
                string marsAtlas = marsAtlasFile.Exists ? marsAtlasFile.FullName : string.Empty;
                FileInfo patientImplantation = new FileInfo(implantationDirectory.FullName + Path.DirectorySeparatorChar + patientDirectory.Name + EXTENSION);
                if(patientImplantation.Exists) implantations.Add(new Implantation("Patient", patientImplantation.FullName, marsAtlas));
                FileInfo MNIImplantation = new FileInfo(implantationDirectory.FullName + Path.DirectorySeparatorChar + patientDirectory.Name + "_MNI" + EXTENSION);
                if(MNIImplantation.Exists) implantations.Add(new Implantation("MNI", MNIImplantation.FullName, marsAtlas));
                FileInfo ACPCImplantation = new FileInfo(implantationDirectory.FullName + Path.DirectorySeparatorChar + patientDirectory.Name + "_ACPC" + EXTENSION);
                if (ACPCImplantation != null) implantations.Add(new Implantation("ACPC", ACPCImplantation.FullName, marsAtlas));
                FileInfo postImplantation = implantationDirectory.GetFiles(patientDirectory.Name + "_T1Post*" + EXTENSION).FirstOrDefault();
                if (postImplantation != null) implantations.Add(new Implantation("Post", postImplantation.FullName, marsAtlas));
            }
            //UnityEngine.Profiling.Profiler.EndSample();
            return implantations.ToArray();
        }
        #endregion

        #region Private Methods
        bool IsCorrect(string[] lines)
        {
            int sites = 0;
            return lines[0] == HEADER && int.TryParse(lines[2],out sites) && sites == (lines.Length - 3);
        }
        bool ReadSites(IEnumerable<string> lines, out IEnumerable<Site> sites)
        {
            sites = new List<Site>();
            bool ok = true;
            foreach (var line in lines)
            {
                Site site;
                if (Site.ReadLine(line,Brain.Patient, out site))
                {
                    ok &= true;
                    (sites as List<Site>).Add(site);
                }
                else
                {
                    ok = false;
                }
            }
            return ok;
        }
        #endregion

        #region Operators
        public object Clone()
        {
            return new Implantation(Name, File, MarsAtlas);
        }
        public void Copy(object copy)
        {
            Implantation implantation = copy as Implantation;
            Name = implantation.Name;
            File = implantation.File;
            MarsAtlas = implantation.MarsAtlas;
            RecalculateUsable();
        }
        #endregion

        #region Serialization
        [OnDeserialized()]
        public void OnDeserialized(StreamingContext context)
        {
            File = File;
            MarsAtlas = MarsAtlas;
            RecalculateUsable();
        }
        #endregion
    }
}