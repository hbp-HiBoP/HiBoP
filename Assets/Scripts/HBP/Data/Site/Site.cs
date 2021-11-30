using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Tools.CSharp;

namespace HBP.Data
{
    /// <summary>
    /// Class which contains all the data about a electrode contact point also known as site.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>ID</b></term>
    /// <description>Unique identifier.</description>
    /// </item>
    /// <item>
    /// <term><b>Name</b></term> 
    /// <description>Name of the site.</description>
    /// </item>
    /// <item>
    /// <term><b>Coordinates</b></term> 
    /// <description>Coordinates of the site in specific reference systems.</description>
    /// </item>
    /// <item>
    /// <term><b>Tags</b></term> 
    /// <description>Tags of the site.</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract]
    public class Site : BaseData, INameable, ILoadable<Site>
    {
        #region Properties
        /// <summary>
        /// Name of the site.
        /// </summary>
        [DataMember] public string Name { get; set; }
        /// <summary>
        /// Coordinates of the site in specific reference systems.
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
        /// <summary>
        /// Get all the possible extensions for site files.
        /// </summary>
        /// <returns></returns>
        public string[] GetExtensions()
        {
            return new string[] { "pts", "tsv", "csv" };
        }
        /// <summary>
        /// Load all sites from a intranat directory.
        /// </summary>
        /// <param name="path">Path to intranat directory</param>
        /// <returns>Sites in the directory</returns>
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
                var ptsSites = LoadSitesFromPTSFile(referenceSystem, file.FullName);
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
        /// <summary>
        /// Load all sites from a BIDS directory.
        /// </summary>
        /// <param name="referenceSystem">reference system</param>
        /// <param name="tsvFile">tvs file</param>
        /// <param name="loadTags">True to load tags, False otherwise</param>
        /// <returns>Sites in the directory</returns>
        public static List<Site> LoadImplantationFromBIDSFile(string referenceSystem, string tsvFile, bool loadTags = true)
        {
            List<Site> sites = new List<Site>();
            if (!string.IsNullOrEmpty(tsvFile))
            {
                using (StreamReader streamReader = new StreamReader(tsvFile))
                {
                    string file = streamReader.ReadToEnd();
                    string[] lines = file.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    // Split the lines before handling them
                    List<List<string>> splittedLines = new List<List<string>>(lines.Length);
                    splittedLines.Add(lines[0].Split('\t').ToList());
                    for (int i = 1; i < lines.Length; ++i)
                    {
                        List<string> splittedLine = lines[i].Split('\t').ToList();
                        if (splittedLine.Count == splittedLines[0].Count)
                        {
                            splittedLines.Add(splittedLine);
                        }
                    }
                    // Look for Mars Atlas specific case and add more information
                    if (ApplicationState.Module3D.MarsAtlas.Loaded)
                    {
                        int marsAtlasIndex = splittedLines[0].IndexOf("MarsAtlas");
                        if (marsAtlasIndex != -1)
                        {
                            splittedLines[0].Insert(marsAtlasIndex + 1, "Hemisphere (MarsAtlas)");
                            splittedLines[0].Insert(marsAtlasIndex + 2, "Lobe (MarsAtlas)");
                            splittedLines[0].Insert(marsAtlasIndex + 3, "Name_FS (MarsAtlas)");
                            splittedLines[0].Insert(marsAtlasIndex + 4, "Full name (MarsAtlas)");
                            splittedLines[0].Insert(marsAtlasIndex + 5, "Brodmann Area (MarsAtlas)");
                            for (int i = 1; i < splittedLines.Count; ++i)
                            {
                                int marsAtlasLabel = ApplicationState.Module3D.MarsAtlas.Label(splittedLines[i][marsAtlasIndex]);
                                splittedLines[i].Insert(marsAtlasIndex + 1, ApplicationState.Module3D.MarsAtlas.Hemisphere(marsAtlasLabel));
                                splittedLines[i].Insert(marsAtlasIndex + 2, ApplicationState.Module3D.MarsAtlas.Lobe(marsAtlasLabel));
                                splittedLines[i].Insert(marsAtlasIndex + 3, ApplicationState.Module3D.MarsAtlas.NameFS(marsAtlasLabel));
                                splittedLines[i].Insert(marsAtlasIndex + 4, ApplicationState.Module3D.MarsAtlas.FullName(marsAtlasLabel));
                                splittedLines[i].Insert(marsAtlasIndex + 5, ApplicationState.Module3D.MarsAtlas.BrodmannArea(marsAtlasLabel));
                            }
                        }
                    }

                    // Add site tags to the project.
                    List<string> columns = splittedLines[0];
                    if (loadTags)
                    {
                        IEnumerable<BaseTag> tags = ApplicationState.ProjectLoaded.Preferences.SitesTags.Concat(ApplicationState.ProjectLoaded.Preferences.GeneralTags);
                        foreach (var column in columns)
                        {
                            if (column != "name" && column != "x" && column != "y" && column != "z" && !tags.Any(t => t.Name == column))
                            {
                                ApplicationState.ProjectLoaded.Preferences.SitesTags.Add(new StringTag(column));
                            }
                        }
                    }

                    // Create sites.
                    IEnumerable<BaseTag> projectTags = ApplicationState.ProjectLoaded.Preferences.SitesTags.Concat(ApplicationState.ProjectLoaded.Preferences.GeneralTags);
                    for (int l = 1; l < splittedLines.Count; l++)
                    {
                        Site site = new Site("", new Coordinate[] { new Coordinate(referenceSystem, new UnityEngine.Vector3()) }, new BaseTagValue[0]);
                        List<string> values = splittedLines[l];
                        for (int v = 0; v < values.Count && v < columns.Count; v++)
                        {
                            string column = columns[v];
                            string value = values[v];
                            if (column == "name") site.Name = value;
                            else if (column == "x" && NumberExtension.TryParseFloat(value, out float x)) site.Coordinates[0].Position = new SerializableVector3(x, site.Coordinates[0].Position.y, site.Coordinates[0].Position.z);
                            else if (column == "y" && NumberExtension.TryParseFloat(value, out float y)) site.Coordinates[0].Position = new SerializableVector3(site.Coordinates[0].Position.x, y, site.Coordinates[0].Position.z);
                            else if (column == "z" && NumberExtension.TryParseFloat(value, out float z)) site.Coordinates[0].Position = new SerializableVector3(site.Coordinates[0].Position.x, site.Coordinates[0].Position.y, z);
                            else if(loadTags)
                            {
                                BaseTag tag = projectTags.FirstOrDefault(t => t.Name == column);
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
                                        tagValue = new EnumTagValue(enumTag, value);
                                    }
                                    else if (tag is FloatTag floatTag)
                                    {
                                        if (NumberExtension.TryParseFloat(value, out float result))
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
                                        if (!string.IsNullOrEmpty(value))
                                        {
                                            tagValue = new StringTagValue(stringTag, value);
                                        }
                                    }
                                    if (tagValue != null)
                                    {
                                        site.Tags.Add(tagValue);
                                    }
                                }
                            }
                        }
                        sites.Add(site);
                    }
                }
            }
            return sites;
        }
        /// <summary>
        /// Load all sites from a intranat directory.
        /// </summary>
        /// <param name="path">Path to intranat directory</param>
        /// <returns>Sites in the directory</returns>
        public static List<Site> LoadImplantationFromIntrAnatFile(string referenceSystem, string ptsFile, string csvFile)
        {
            List<Site> result = new List<Site>();
            if (!string.IsNullOrEmpty(ptsFile))
            {
                using (StreamReader ptssr = new StreamReader(ptsFile))
                {
                    string line;
                    line = ptssr.ReadLine();
                    if (!line.Contains("ptsfile")) return result;
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
                            Array.IndexOf(firstLineSplits, "contact"),
                            Array.IndexOf(firstLineSplits, "MNI")
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
        /// <summary>
        /// Load all sites from PTS file.
        /// </summary>
        /// <param name="referenceSystem">reference system</param>
        /// <param name="ptsFile">pts file path</param>
        /// <returns>All sites in the pts file</returns>
        public static List<Site> LoadSitesFromPTSFile(string referenceSystem, string ptsFile)
        {
            var sites = new List<Site>();
            if (!string.IsNullOrEmpty(ptsFile))
            {
                using (StreamReader streamReader = new StreamReader(ptsFile))
                {
                    string line = streamReader.ReadLine();
                    if (!line.Contains("ptsfile")) return sites;
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
        /// <summary>
        /// Load all sites from csv file.
        /// </summary>
        /// <param name="csvFile">CSV file path</param>
        /// <returns>All sites in the csv file</returns>
        public static List<Site> LoadSitesFromCSVFile(string csvFile)
        {
            var sites = new List<Site>();
            if (!string.IsNullOrEmpty(csvFile))
            {
                using (StreamReader streamReader = new StreamReader(csvFile))
                {
                    string file = streamReader.ReadToEnd();
                    string[] lines = file.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    int titleLine = Array.FindIndex(lines, l => l.StartsWith("contact"));
                    if (titleLine > -1)
                    {
                        // Split the lines before handling them
                        List<List<string>> splittedLines = new List<List<string>>(lines.Length - titleLine);
                        splittedLines.Add(lines[titleLine].Split('\t').ToList());
                        for (int i = titleLine + 1; i < lines.Length; ++i)
                        {
                            List<string> splittedLine = lines[i].Split('\t').ToList();
                            if (splittedLine.Count == splittedLines[0].Count)
                            {
                                splittedLines.Add(splittedLine);
                            }
                        }
                        // Look for Mars Atlas specific case and add more information
                        if (ApplicationState.Module3D.MarsAtlas.Loaded)
                        {
                            int marsAtlasIndex = splittedLines[0].IndexOf("MarsAtlas");
                            if (marsAtlasIndex != -1)
                            {
                                splittedLines[0].Insert(marsAtlasIndex + 1, "Hemisphere (MarsAtlas)");
                                splittedLines[0].Insert(marsAtlasIndex + 2, "Lobe (MarsAtlas)");
                                splittedLines[0].Insert(marsAtlasIndex + 3, "Name_FS (MarsAtlas)");
                                splittedLines[0].Insert(marsAtlasIndex + 4, "Full name (MarsAtlas)");
                                splittedLines[0].Insert(marsAtlasIndex + 5, "Brodmann Area (MarsAtlas)");
                                for (int i = 1; i < splittedLines.Count; ++i)
                                {
                                    int marsAtlasLabel = ApplicationState.Module3D.MarsAtlas.Label(splittedLines[i][marsAtlasIndex]);
                                    splittedLines[i].Insert(marsAtlasIndex + 1, ApplicationState.Module3D.MarsAtlas.Hemisphere(marsAtlasLabel));
                                    splittedLines[i].Insert(marsAtlasIndex + 2, ApplicationState.Module3D.MarsAtlas.Lobe(marsAtlasLabel));
                                    splittedLines[i].Insert(marsAtlasIndex + 3, ApplicationState.Module3D.MarsAtlas.NameFS(marsAtlasLabel));
                                    splittedLines[i].Insert(marsAtlasIndex + 4, ApplicationState.Module3D.MarsAtlas.FullName(marsAtlasLabel));
                                    splittedLines[i].Insert(marsAtlasIndex + 5, ApplicationState.Module3D.MarsAtlas.BrodmannArea(marsAtlasLabel));
                                }
                            }
                        }
                        // Create tags and tagValues
                        List<string> tagNames = splittedLines[0];
                        BaseTag[] tags = new BaseTag[tagNames.Count];
                        for (int i = 0; i < tagNames.Count; i++)
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
                        for (int l = 1; l < splittedLines.Count; l++)
                        {
                            List<string> values = splittedLines[l];
                            string name = ApplicationState.UserPreferences.Data.Anatomic.SiteNameCorrection ? FixName(values[0]) : values[0];
                            List<BaseTagValue> tagValues = new List<BaseTagValue>();
                            for (int i = 1; i < values.Count; i++)
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
                                        tagValue = new EnumTagValue(enumTag, value);
                                    }
                                    else if (tag is FloatTag floatTag)
                                    {
                                        if (NumberExtension.TryParseFloat(value, out float result))
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
                                        if (!string.IsNullOrEmpty(value))
                                        {
                                            tagValue = new StringTagValue(stringTag, value);
                                        }
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

        /// <summary>
        /// Fix the name (P,'p).
        /// </summary>
        /// <param name="name">Name</param>
        /// <returns>Fixed name</returns>
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
        public override List<BaseData> GetAllIdentifiable()
        {
            List<BaseData> IDs = base.GetAllIdentifiable();
            foreach (var tag in Tags) IDs.AddRange(tag.GetAllIdentifiable());
            foreach (var coordinate in Coordinates) IDs.AddRange(coordinate.GetAllIdentifiable());
            return IDs;
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
        /// <summary>
        /// Get all the possible extensions for site files.
        /// </summary>
        /// <returns></returns>
        string[] ILoadable<Site>.GetExtensions()
        {
            return GetExtensions();
        }
        /// <summary>
        /// Load all sites from file.
        /// </summary>
        /// <param name="path">file path</param>
        /// <param name="result">All sites in the file</param>
        /// <returns>True if isOk, False otherwise</returns>
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
                result = LoadImplantationFromBIDSFile(name, path).ToArray();
                return true;
            }
            else if (fileInfo.Extension == ".csv")
            {
                result = LoadSitesFromCSVFile(path).ToArray();
                return true;
            }
            return false;
        }
        #endregion
    }
}