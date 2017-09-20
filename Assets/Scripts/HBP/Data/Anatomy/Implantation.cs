using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;

namespace HBP.Data.Anatomy
{
    [DataContract]
    public class Implantation : ICloneable, ICopiable
    {
        #region Properties
        const string HEADER = "ptsfile";
        public const string EXTENSION = ".pts";
        public enum Error { None, PathIsNullOrEmpty, FileNotFound, WrongExtension, CannotReadFile, WrongFormat, CannotReadAllSites };
        [DataMember] public string Name { get; set; }
        [DataMember] public string File { get; set; }
        [IgnoreDataMember] public List<Site> Sites { get; set; }
        [IgnoreDataMember] public Brain Brain { get; set; }
        public virtual bool Usable
        {
            get { return !string.IsNullOrEmpty(Name) && HasImplantation; }
        }
        public virtual bool HasImplantation
        {
            get
            {
                return !string.IsNullOrEmpty(File) && System.IO.File.Exists(File) && (new FileInfo(File).Extension == EXTENSION);
            }
        }
        #endregion

        #region Constructor
        public Implantation(string name, string path)
        {
            Name = name;
            File = path;
            Sites = new List<Site>();
        }
        public Implantation() : this("New implantation", string.Empty)
        {
        }
        #endregion

        #region Public Methods
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
            DirectoryInfo implantationDirectory = new DirectoryInfo(path + System.IO.Path.DirectorySeparatorChar + "implantation");
            if (implantationDirectory.Exists)
            {
                FileInfo patientImplantation = new FileInfo(implantationDirectory.FullName + System.IO.Path.DirectorySeparatorChar + patientDirectory.Name + EXTENSION);
                if(patientImplantation.Exists) implantations.Add(new Implantation("Patient", patientImplantation.FullName));
                FileInfo MNIImplantation = new FileInfo(implantationDirectory.FullName + System.IO.Path.DirectorySeparatorChar + patientDirectory.Name + "_MNI" + EXTENSION);
                if(MNIImplantation.Exists) implantations.Add(new Implantation("MNI", MNIImplantation.FullName));
                FileInfo ACPCImplantation = new FileInfo(implantationDirectory.FullName + System.IO.Path.DirectorySeparatorChar + patientDirectory.Name + "_ACPC" + EXTENSION);
                if (ACPCImplantation != null) implantations.Add(new Implantation("ACPC", ACPCImplantation.FullName));
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
            return new Implantation(Name, File);
        }
        public void Copy(object copy)
        {
            Implantation implantation = copy as Implantation;
            Name = implantation.Name;
            File = implantation.File;
        }
        #endregion
    }
}