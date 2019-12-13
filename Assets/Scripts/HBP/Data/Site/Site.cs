using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Tools.CSharp;

namespace HBP.Data
{
    [DataContract]
    public class Site : BaseData, INameable, ILoadable<Site>
    {
        #region Properties
        /// <summary>
        /// Name of the site.
        /// </summary>
        [DataMember] public string Name { get; set; }
        /// <summary>
        /// Coordinates of the site.
        /// </summary>
        [DataMember] public List<Coordinate> Coordinates { get; set; }
        /// <summary>
        /// Tags of the site.
        /// </summary>
        [DataMember] public List<BaseTagValue> Tags { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the site class.
        /// </summary>
        /// <param name="name">Name of the site.</param>
        /// <param name="tags">Tags of the site.</param>
        /// <param name="ID">Unique identifier to identify the patient.</param>
        public Site(string name, IEnumerable<Coordinate> coordinates, IEnumerable<BaseTagValue> tags, string ID) : base(ID)
        {
            Name = name;
            Coordinates = coordinates.ToList();
            Tags = tags.ToList();
        }
        /// <summary>
        /// Initializes a new instance of the site class.
        /// </summary>
        /// <param name="name">Name of the site.</param>
        /// <param name="tags">Tags of the site.</param>
        public Site(string name, IEnumerable<Coordinate> coordinates, IEnumerable<BaseTagValue> tags) : base()
        {
            Name = name;
            Coordinates = coordinates.ToList();
            Tags = tags.ToList();
        }
        /// <summary>
        /// Initializes a new instance of the site class.
        /// </summary>
        public Site() : this("Unknown", new Coordinate[0], new BaseTagValue[0])
        {
        }
        #endregion

        #region Public Methods
        public string[] GetExtensions()
        {
            return new string[] { "pts", "tsv" };
        }
        public static List<Site> LoadFromIntranatDirectory(string path)
        {
            var sites = new List<Site>();
            var parent = new DirectoryInfo(path);
            var implantationDirection = new DirectoryInfo(Path.Combine(path, "implantation"));
            var ptsFiles = implantationDirection.GetFiles("*.pts", SearchOption.TopDirectoryOnly);
            var csvFiles = implantationDirection.GetFiles("*.csv", SearchOption.TopDirectoryOnly);
            foreach (var file in ptsFiles)
            {
                string referenceSystem = file.Name.Replace(parent.Name, "").Replace("_", "").Replace(".pts", "");
                if (referenceSystem == "")
                {
                    referenceSystem = "Patient";
                }
                else if (referenceSystem.Contains("T1Post"))
                {
                    referenceSystem = "Post";
                }
                var ptsSites = LoadSitesFromPTSFIle(referenceSystem, file.FullName);
                foreach (var site in ptsSites)
                {
                    var existingSite = sites.FirstOrDefault(s => s.Name == site.Name);
                    if (existingSite == null) sites.Add(site);
                    else existingSite.Coordinates.AddRange(site.Coordinates);
                }
            }
            foreach (var file in csvFiles)
            {
                var csvSites = LoadSitesFromCSVFile(file.FullName);
                foreach (var site in csvSites)
                {
                    var existingSite = sites.FirstOrDefault(s => s.Name == site.Name);
                    if (existingSite != null) existingSite.Tags.AddRange(site.Tags);
                }
            }
            return sites;
        }
        public static List<Site> LoadImplantationFromBIDSFile(string referenceSystem, string tsvFile, bool onlyCoordinates = false)
        {
            List<Site> result = new List<Site>();
            if (!string.IsNullOrEmpty(tsvFile))
            {
                using (StreamReader sr = new StreamReader(tsvFile))
                {
                    // Find which column of the tsv corresponds to which mandatory argument
                    string firstLine = sr.ReadLine();
                    string[] firstLineSplits = firstLine.Split('\t');
                    int[] indices = new int[4]
                    {
                        Array.IndexOf(firstLineSplits, "name"),
                        Array.IndexOf(firstLineSplits, "x"),
                        Array.IndexOf(firstLineSplits, "y"),
                        Array.IndexOf(firstLineSplits, "z")
                    };
                    Dictionary<int, BaseTag> tagByColumnIndex = new Dictionary<int, BaseTag>();
                    if (!onlyCoordinates)
                    {
                        for (int i = 0; i < firstLineSplits.Length; i++)
                        {
                            if (indices.Contains(i)) continue;
                            BaseTag associatedTag = ApplicationState.ProjectLoaded.Preferences.SitesTags.FirstOrDefault(t => t.Name == firstLineSplits[i]);
                            if (associatedTag == null) associatedTag = ApplicationState.ProjectLoaded.Preferences.GeneralTags.FirstOrDefault(t => t.Name == firstLineSplits[i]);
                            tagByColumnIndex.Add(i, associatedTag);
                        }
                    }
                    // Create sites
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        Site site = new Site();
                        string[] args = line.Split('\t');
                        site.Name = ApplicationState.UserPreferences.Data.Anatomic.SiteNameCorrection ? FixName(args[indices[0]]) : args[indices[0]];
                        if (!NumberExtension.TryParseFloat(args[indices[1]], out float x)) continue;
                        if (!NumberExtension.TryParseFloat(args[indices[2]], out float y)) continue;
                        if (!NumberExtension.TryParseFloat(args[indices[3]], out float z)) continue;
                        site.Coordinates.Add(new Coordinate(referenceSystem, new UnityEngine.Vector3(x, y, z)));
                        foreach (var kvTag in tagByColumnIndex)
                        {
                            site.Tags.Add(new BaseTagValue(kvTag.Value, args[kvTag.Key]));
                        }
                        result.Add(site);
                    }
                }
            }
            return result;
        }
        public static List<Site> LoadImplantationFromIntrAnatFile(string referenceSystem, string ptsFile, string csvFile)
        {
            List<Site> result = new List<Site>();
            if (!string.IsNullOrEmpty(ptsFile))
            {
                using (StreamReader ptssr = new StreamReader(ptsFile))
                {
                    string line;
                    line = ptssr.ReadLine();
                    if (!line.Contains("ptsfile")) throw new System.Exception("Invalid PTS file");
                    while ((line = ptssr.ReadLine()) != null)
                    {
                        Site site = new Site();
                        string[] splits = Regex.Split(line, "[\\s\t]+");
                        if (splits.Length < 4) continue;
                        site.Name = ApplicationState.UserPreferences.Data.Anatomic.SiteNameCorrection ? FixName(splits[0]) : splits[0];
                        if (!NumberExtension.TryParseFloat(splits[1], out float x)) continue;
                        if (!NumberExtension.TryParseFloat(splits[2], out float y)) continue;
                        if (!NumberExtension.TryParseFloat(splits[3], out float z)) continue;
                        site.Coordinates.Add(new Coordinate(referenceSystem, new UnityEngine.Vector3(x, y, z)));
                        result.Add(site);
                    }
                }
                if (!string.IsNullOrEmpty(csvFile))
                {
                    using (StreamReader csvsr = new StreamReader(csvFile))
                    {
                        csvsr.ReadLine();
                        csvsr.ReadLine();
                        // Find which column of the tsv corresponds to which mandatory argument
                        string firstLine = csvsr.ReadLine();
                        string[] firstLineSplits = firstLine.Split('\t');
                        int[] indices = new int[2]
                        {
                            System.Array.IndexOf(firstLineSplits, "contact"),
                            System.Array.IndexOf(firstLineSplits, "MNI")
                        };
                        Dictionary<int, BaseTag> tagByColumnIndex = new Dictionary<int, BaseTag>();
                        for (int i = 0; i < firstLineSplits.Length; i++)
                        {
                            if (indices.Contains(i)) continue;
                            BaseTag associatedTag = ApplicationState.ProjectLoaded.Preferences.SitesTags.FirstOrDefault(t => t.Name == firstLineSplits[i]);
                            if (associatedTag == null) associatedTag = ApplicationState.ProjectLoaded.Preferences.GeneralTags.FirstOrDefault(t => t.Name == firstLineSplits[i]);
                            tagByColumnIndex.Add(i, associatedTag);
                        }
                        // Fill tags
                        string line;
                        while ((line = csvsr.ReadLine()) != null)
                        {
                            string[] args = line.Split('\t');
                            string siteName = ApplicationState.UserPreferences.Data.Anatomic.SiteNameCorrection ? FixName(args[indices[0]]) : args[indices[0]];
                            Site site = result.FirstOrDefault(s => s.Name == siteName);
                            if (site != null)
                            {
                                foreach (var kvTag in tagByColumnIndex)
                                {
                                    site.Tags.Add(new BaseTagValue(kvTag.Value, args[kvTag.Key]));
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }
        public static List<Site> LoadSitesFromPTSFIle(string referenceSystem, string ptsFile)
        {
            var sites = new List<Site>();
            if (!string.IsNullOrEmpty(ptsFile))
            {
                using (StreamReader streamReader = new StreamReader(ptsFile))
                {
                    string line = streamReader.ReadLine();
                    if (!line.Contains("ptsfile")) throw new System.Exception("Invalid PTS file");
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        Site site = new Site();
                        string[] splits = Regex.Split(line, "[\\s\t]+");
                        if (splits.Length < 4) continue;
                        site.Name = ApplicationState.UserPreferences.Data.Anatomic.SiteNameCorrection ? FixName(splits[0]) : splits[0];
                        if (!NumberExtension.TryParseFloat(splits[1], out float x)) continue;
                        if (!NumberExtension.TryParseFloat(splits[2], out float y)) continue;
                        if (!NumberExtension.TryParseFloat(splits[3], out float z)) continue;
                        site.Coordinates.Add(new Coordinate(referenceSystem, new UnityEngine.Vector3(x, y, z)));
                        sites.Add(site);
                    }
                }
            }
            return sites;
        }
        public static List<Site> LoadSitesFromCSVFile(string csvFile)
        {
            var sites = new List<Site>();
            if (!string.IsNullOrEmpty(csvFile))
            {
                using (StreamReader streamReader = new StreamReader(csvFile))
                {
                    string file = streamReader.ReadToEnd();
                    string[] lines = file.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    int TitleLine = Array.FindIndex(lines, l => l.StartsWith("contact"));
                    if (TitleLine > -1)
                    {
                        string[] tagNames = lines[TitleLine].Split('\t');
                        BaseTag[] tags = new BaseTag[tagNames.Length];
                        for (int i = 0; i < tagNames.Length; i++)
                        {
                            string tagName = tagNames[i];
                            BaseTag tag = null;
                            if (tagName != "MNI" && tagName != "Contact" && tagName != "contact")
                            {
                                tag = ApplicationState.ProjectLoaded.Preferences.SitesTags.Concat(ApplicationState.ProjectLoaded.Preferences.GeneralTags).FirstOrDefault(t => t.Name == tagName);
                                if (tag == null)
                                {
                                    tag = new StringTag(tagNames[i]);
                                    ApplicationState.ProjectLoaded.Preferences.SitesTags.Add(tag);
                                }
                            }
                            tags[i] = tag;
                        }
                        for (int l = TitleLine + 1; l < lines.Length; l++)
                        {
                            string[] values = lines[l].Split('\t');
                            string name = ApplicationState.UserPreferences.Data.Anatomic.SiteNameCorrection ? FixName(values[0]) : values[0];
                            List<BaseTagValue> tagValues = new List<BaseTagValue>();
                            for (int i = 1; i < values.Length; i++)
                            {
                                BaseTag tag = tags[i];
                                string value = values[i];
                                if (tag != null)
                                {
                                    BaseTagValue tagValue = null;
                                    if (tag is EmptyTag emptyTag)
                                    {
                                        tagValue = new EmptyTagValue(emptyTag);
                                    }
                                    else if (tag is BoolTag boolTag)
                                    {
                                        if (bool.TryParse(value, out bool result))
                                        {
                                            tagValue = new BoolTagValue(boolTag, result);
                                        }
                                    }
                                    else if (tag is EnumTag enumTag)
                                    {
                                        int index = Array.IndexOf(enumTag.Values, value);
                                        tagValue = new EnumTagValue(enumTag, index);
                                    }
                                    else if (tag is FloatTag floatTag)
                                    {
                                        if (float.TryParse(value, out float result))
                                        {
                                            tagValue = new FloatTagValue(floatTag, result);
                                        }
                                    }
                                    else if (tag is IntTag intTag)
                                    {
                                        if (int.TryParse(value, out int result))
                                        {
                                            tagValue = new IntTagValue(intTag, result);
                                        }
                                    }
                                    else if (tag is StringTag stringTag)
                                    {
                                        tagValue = new StringTagValue(stringTag, value);
                                    }
                                    if (tagValue != null)
                                    {
                                        tagValues.Add(tagValue);
                                    }
                                }
                            }
                            sites.Add(new Site(name, new Coordinate[0], tagValues));
                        }
                    }
                }
            }
            return sites;
        }

        public static string FixName(string name)
        {
            string siteName = name.ToUpper();
            siteName = siteName.Replace("PLOT", "");
            int prime = siteName.LastIndexOf('P');
            if (prime > 0)
            {
                siteName = siteName.Remove(prime, 1).Insert(prime, "\'");
            }
            for (int i = siteName.Length - 1; i > 0; --i)
            {
                if (siteName[i] == '0' && !char.IsDigit(siteName[i - 1]))
                {
                    siteName = siteName.Remove(i, 1);
                }
            }
            return siteName;
        }
        /// <summary>
        /// Generates  ID recursively.
        /// </summary>
        public override void GenerateID()
        {
            base.GenerateID();
            foreach (var tag in Tags) tag.GenerateID();
            foreach (var coordinate in Coordinates) coordinate.GenerateID();
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone the instance.
        /// </summary>
        /// <returns>object cloned.</returns>
        public override object Clone()
        {
            return new Site(Name, Coordinates.DeepClone(), Tags.DeepClone(), ID);
        }
        /// <summary>
        /// Copy the instance.
        /// </summary>
        /// <param name="obj">instance to copy.</param>
        public override void Copy(object obj)
        {
            base.Copy(obj);
            if (obj is Site site)
            {
                Name = site.Name;
                Coordinates = site.Coordinates;
                Tags = site.Tags;
            }
        }
        #endregion

        #region Interfaces
        string[] ILoadable<Site>.GetExtensions()
        {
            return GetExtensions();
        }
        bool ILoadable<Site>.LoadFromFile(string path, out Site[] result)
        {
            result = new Site[0];
            FileInfo fileInfo = new FileInfo(path);
            if (fileInfo.Extension == ".pts")
            {
                string referenceSystem = "Unknown";
                string[] splits = fileInfo.Name.Split('_');
                if (splits.Length == 3)
                {
                    referenceSystem = "Patient";
                }
                else if (splits.Length == 4)
                {
                    referenceSystem = splits[3].Replace(fileInfo.Extension, "");
                }
                result = LoadImplantationFromIntrAnatFile(referenceSystem, path, "").ToArray();
                return true;
            }
            else if (fileInfo.Extension == ".tsv")
            {
                string name = path.Split('_').FirstOrDefault(s => s.Contains("space")).Split('-')[1];
                result = LoadImplantationFromBIDSFile(name, path, true).ToArray();
                return true;
            }
            return false;
        }
        #endregion
    }
}