using HBP.Core.Interfaces;
using HBP.Core.Object3D;
using HBP.Core.Tools;
using HBP.Core.Preferences;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.Scripting;

namespace HBP.Core.Data
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
    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class Site : BaseData, INameable, ILoadable<Site>
    {
        #region Properties

        /// <summary>
        /// Name of the site.
        /// </summary>
        [JsonProperty] public string Name { get; set; }

        /// <summary>
        /// Coordinates of the site in specific reference systems.
        /// </summary>
        [JsonProperty] public List<Coordinate> Coordinates { get; set; }

        /// <summary>
        /// Tags of the site.
        /// </summary>
        [JsonProperty] public List<BaseTagValue> Tags { get; set; }


        /// <summary>
        /// Do we need to fix site names ?
        /// </summary>
        [JsonIgnore] public static bool SiteNameCorrection = true;

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
        public static List<Site> LoadFromIntranatDirectory(string path, TagParsingPolicy policy = null, bool createMissingTags = true, TagImportContext importContext = null)
        {
            policy = importContext?.Policy ?? policy ?? TagParsingPolicy.Default;
            var sites = new List<Site>();
            var parent = new DirectoryInfo(path);
            var implantationDirection = new DirectoryInfo(Path.Combine(path, "implantation"));
            if (implantationDirection.Exists)
            {
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
                    else if (referenceSystem.Contains("CTPost"))
                    {
                        referenceSystem = "CT";
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
                    var csvSites = LoadSitesFromCSVFile(file.FullName, policy, createMissingTags, importContext);
                    foreach (var site in csvSites)
                    {
                        var existingSite = sites.FirstOrDefault(s => s.Name == site.Name);
                        existingSite?.Tags.AddRange(site.Tags);
                    }
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
        public static List<Site> LoadImplantationFromBIDSFile(string referenceSystem, string tsvFile, bool loadTags = true, TagParsingPolicy policy = null, bool createMissingTags = true, TagImportContext importContext = null)
        {
            policy = importContext?.Policy ?? policy ?? TagParsingPolicy.Default;
            TagCollection tagCollection = importContext?.Tags ?? PersistentDataManager.Tags;
            List<Site> sites = new();
            if (!string.IsNullOrEmpty(tsvFile))
            {
                using (StreamReader streamReader = new(tsvFile))
                {
                    string file = streamReader.ReadToEnd();
                    string[] lines = file.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    // Split the lines before handling them
                    List<List<string>> splittedLines = new(lines.Length);
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
                    if (Object3DManager.MarsAtlas.Loaded)
                    {
                        int marsAtlasIndex = splittedLines[0].IndexOf("MarsAtlas");
                        if (marsAtlasIndex != -1)
                        {
                            splittedLines[0].Insert(marsAtlasIndex + 1, "Hemisphere-MarsAtlas");
                            splittedLines[0].Insert(marsAtlasIndex + 2, "Lobe-MarsAtlas");
                            splittedLines[0].Insert(marsAtlasIndex + 3, "NameFS-MarsAtlas");
                            splittedLines[0].Insert(marsAtlasIndex + 4, "Fullname-MarsAtlas");
                            splittedLines[0].Insert(marsAtlasIndex + 5, "BrodmannArea-MarsAtlas");
                            for (int i = 1; i < splittedLines.Count; ++i)
                            {
                                int marsAtlasLabel = Object3DManager.MarsAtlas.Label(splittedLines[i][marsAtlasIndex]);
                                splittedLines[i].Insert(marsAtlasIndex + 1, Object3DManager.MarsAtlas.Hemisphere(marsAtlasLabel));
                                splittedLines[i].Insert(marsAtlasIndex + 2, Object3DManager.MarsAtlas.Lobe(marsAtlasLabel));
                                splittedLines[i].Insert(marsAtlasIndex + 3, Object3DManager.MarsAtlas.NameFS(marsAtlasLabel));
                                splittedLines[i].Insert(marsAtlasIndex + 4, Object3DManager.MarsAtlas.FullName(marsAtlasLabel));
                                splittedLines[i].Insert(marsAtlasIndex + 5, Object3DManager.MarsAtlas.BrodmannArea(marsAtlasLabel));
                            }
                        }
                    }

                    // Add site tags to the project.
                    List<string> columns = splittedLines[0];
                    if (loadTags)
                    {
                        TagImportObservations observations = new();
                        for (int row = 1; row < splittedLines.Count; row++)
                        {
                            for (int column = 0; column < columns.Count && column < splittedLines[row].Count; column++)
                            {
                                string tagName = columns[column];
                                if (!tagName.Equals("name", StringComparison.OrdinalIgnoreCase) && !tagName.Equals("x", StringComparison.OrdinalIgnoreCase) && !tagName.Equals("y", StringComparison.OrdinalIgnoreCase) && !tagName.Equals("z", StringComparison.OrdinalIgnoreCase))
                                {
                                    observations.AddSiteValue(tagName, splittedLines[row][column]);
                                }
                            }
                        }

                        if (createMissingTags) observations.CreateMissingTags(tagCollection, policy);
                    }

                    // Create sites.
                    IEnumerable<BaseTag> projectTags = tagCollection.SitesTags.Concat(tagCollection.GeneralTags);
                    int nameColumnIndex = columns.FindIndex(column => column.Equals("name", StringComparison.OrdinalIgnoreCase));
                    for (int l = 1; l < splittedLines.Count; l++)
                    {
                        List<string> values = splittedLines[l];
                        string siteName = nameColumnIndex >= 0 && nameColumnIndex < values.Count ? values[nameColumnIndex] : string.Empty;
                        Site site = new(siteName, new Coordinate[] { new(referenceSystem, new UnityEngine.Vector3()) }, new BaseTagValue[0]);
                        for (int v = 0; v < values.Count && v < columns.Count; v++)
                        {
                            string column = columns[v];
                            string value = values[v];
                            if (column.Equals("name", StringComparison.OrdinalIgnoreCase)) site.Name = value;
                            else if (column.Equals("x", StringComparison.OrdinalIgnoreCase) && NumberExtension.TryParseFloat(value, out float x)) site.Coordinates[0].Position = new SerializableVector3(x, site.Coordinates[0].Position.y, site.Coordinates[0].Position.z);
                            else if (column.Equals("y", StringComparison.OrdinalIgnoreCase) && NumberExtension.TryParseFloat(value, out float y)) site.Coordinates[0].Position = new SerializableVector3(site.Coordinates[0].Position.x, y, site.Coordinates[0].Position.z);
                            else if (column.Equals("z", StringComparison.OrdinalIgnoreCase) && NumberExtension.TryParseFloat(value, out float z)) site.Coordinates[0].Position = new SerializableVector3(site.Coordinates[0].Position.x, site.Coordinates[0].Position.y, z);
                            else if (loadTags)
                            {
                                BaseTag tag = projectTags.FirstOrDefault(t => string.Equals(t.Name?.Trim(), column.Trim(), StringComparison.OrdinalIgnoreCase));
                                if (tag != null)
                                {
                                    RawTagValueResult creation = importContext?.TryCreate(TagCategory.Site, tag, value, tsvFile, site.Name) ?? RawTagValueFactory.TryCreate(tag, value, policy);
                                    if (creation.Status == RawTagValueStatus.Success)
                                    {
                                        site.Tags.Add(creation.Value);
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
        /// Checks if a tag value is invalid (n/a, nan, empty, etc.)
        /// This method provides consistent handling between Intranat and BIDS databases.
        /// </summary>
        /// <param name="value">The string value to check</param>
        /// <returns>True if the value is invalid and should be ignored, false otherwise</returns>
        private static bool IsInvalidTagValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true;

            string lowerValue = value.Trim().ToLower();
            return lowerValue == "n/a" || lowerValue == "na" || lowerValue == "nan" || lowerValue == "null" || lowerValue == "none" || lowerValue == "" || lowerValue == "-";
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
                using (StreamReader streamReader = new(ptsFile))
                {
                    string line = streamReader.ReadLine();
                    if (!line.Contains("ptsfile")) return sites;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        Site site = new();
                        string[] splits = Regex.Split(line, "[\\s\t]+");
                        if (splits.Length < 4) continue;
                        site.Name = SiteNameCorrection ? SiteTools.FixName(splits[0]) : splits[0];
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
        /// Save sites to the historical PTS text format.
        /// </summary>
        /// <param name="sites">Sites to save.</param>
        /// <param name="referenceSystem">Reference system of the coordinates to save.</param>
        /// <param name="ptsFile">Destination PTS file path.</param>
        public static void SaveSitesToPTSFile(IEnumerable<Site> sites, string referenceSystem, string ptsFile)
        {
            if (sites == null) throw new ArgumentNullException(nameof(sites));
            if (string.IsNullOrWhiteSpace(referenceSystem)) throw new ArgumentException("The reference system is empty.", nameof(referenceSystem));
            if (string.IsNullOrWhiteSpace(ptsFile)) throw new ArgumentException("The PTS file path is empty.", nameof(ptsFile));

            List<(Site Site, Coordinate Coordinate)> entries = sites.Select(site =>
            {
                if (site == null) throw new ArgumentException("The site collection contains a null entry.", nameof(sites));
                Coordinate coordinate = site.Coordinates.FirstOrDefault(value => value.ReferenceSystem == referenceSystem);
                if (coordinate == null)
                {
                    throw new InvalidDataException($"Site '{site.Name}' has no coordinate in reference system '{referenceSystem}'.");
                }

                return (site, coordinate);
            }).ToList();

            string directory = Path.GetDirectoryName(ptsFile);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using StreamWriter writer = new(ptsFile, false);
            writer.WriteLine("ptsfile");
            writer.WriteLine("1\t1\t1");
            writer.WriteLine(entries.Count.ToString(CultureInfo.InvariantCulture));
            foreach ((Site site, Coordinate coordinate) in entries)
            {
                UnityEngine.Vector3 position = coordinate.Position.ToVector3();
                writer.Write(site.Name);
                writer.Write('\t');
                writer.Write(position.x.ToString("F6", CultureInfo.InvariantCulture));
                writer.Write('\t');
                writer.Write(position.y.ToString("F6", CultureInfo.InvariantCulture));
                writer.Write('\t');
                writer.WriteLine(position.z.ToString("F6", CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Load all sites from csv file.
        /// </summary>
        /// <param name="csvFile">CSV file path</param>
        /// <returns>All sites in the csv file</returns>
        public static List<Site> LoadSitesFromCSVFile(string csvFile, TagParsingPolicy policy = null, bool createMissingTags = true, TagImportContext importContext = null)
        {
            policy = importContext?.Policy ?? policy ?? TagParsingPolicy.Default;
            TagCollection tagCollection = importContext?.Tags ?? PersistentDataManager.Tags;
            var sites = new List<Site>();
            if (!string.IsNullOrEmpty(csvFile))
            {
                using StreamReader streamReader = new(csvFile);
                string file = streamReader.ReadToEnd();
                string[] lines = file.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                int titleLine = Array.FindIndex(lines, line => line.Contains('\t') && string.Equals(line.Split('\t')[0].Trim(), "contact", StringComparison.OrdinalIgnoreCase));
                if (titleLine > -1)
                {
                    // Split the lines before handling them
                    List<List<string>> splittedLines = new(lines.Length - titleLine)
                    {
                        lines[titleLine].Split('\t').ToList()
                    };
                    for (int i = titleLine + 1; i < lines.Length; ++i)
                    {
                        List<string> splittedLine = lines[i].Split('\t').ToList();
                        if (splittedLine.Count == splittedLines[0].Count)
                        {
                            splittedLines.Add(splittedLine);
                        }
                    }

                    // Look for Mars Atlas specific case and add more information
                    if (Object3DManager.MarsAtlas.Loaded)
                    {
                        int marsAtlasIndex = splittedLines[0].IndexOf("MarsAtlas");
                        int intrAnatMarsAtlasIndex = splittedLines[0].IndexOf("IntrAnat-MarsAtlas");
                        int mniMarsAtlasIndex = splittedLines[0].IndexOf("MNI-MarsAtlas");

                        // Determine which column to use as the base for inserting additional fields
                        int baseIndex = -1;
                        if (marsAtlasIndex != -1) baseIndex = marsAtlasIndex;
                        else if (intrAnatMarsAtlasIndex != -1) baseIndex = intrAnatMarsAtlasIndex;
                        else if (mniMarsAtlasIndex != -1) baseIndex = mniMarsAtlasIndex;

                        if (baseIndex != -1)
                        {
                            splittedLines[0].Insert(baseIndex + 1, "Hemisphere-MarsAtlas");
                            splittedLines[0].Insert(baseIndex + 2, "Lobe-MarsAtlas");
                            splittedLines[0].Insert(baseIndex + 3, "NameFS-MarsAtlas");
                            splittedLines[0].Insert(baseIndex + 4, "Fullname-MarsAtlas");
                            splittedLines[0].Insert(baseIndex + 5, "Brodmann-MarsAtlas");

                            for (int i = 1; i < splittedLines.Count; ++i)
                            {
                                int marsAtlasLabel = -1;

                                // Check MarsAtlas first, then fallback to IntrAnat-MarsAtlas, then MNI-MarsAtlas
                                if (marsAtlasIndex != -1 && splittedLines[i][marsAtlasIndex].ToLower() != "n/a")
                                    marsAtlasLabel = Object3DManager.MarsAtlas.Label(splittedLines[i][marsAtlasIndex]);
                                else if (intrAnatMarsAtlasIndex != -1 && splittedLines[i][intrAnatMarsAtlasIndex].ToLower() != "n/a")
                                    marsAtlasLabel = Object3DManager.MarsAtlas.Label(splittedLines[i][intrAnatMarsAtlasIndex]);
                                else if (mniMarsAtlasIndex != -1 && splittedLines[i][mniMarsAtlasIndex].ToLower() != "n/a")
                                    marsAtlasLabel = Object3DManager.MarsAtlas.Label(splittedLines[i][mniMarsAtlasIndex]);

                                // If all available values are N/A, set all derived fields to N/A
                                if (marsAtlasLabel == -1)
                                {
                                    splittedLines[i].Insert(baseIndex + 1, "N/A");
                                    splittedLines[i].Insert(baseIndex + 2, "N/A");
                                    splittedLines[i].Insert(baseIndex + 3, "N/A");
                                    splittedLines[i].Insert(baseIndex + 4, "N/A");
                                    splittedLines[i].Insert(baseIndex + 5, "N/A");
                                }
                                else
                                {
                                    splittedLines[i].Insert(baseIndex + 1, Object3DManager.MarsAtlas.Hemisphere(marsAtlasLabel));
                                    splittedLines[i].Insert(baseIndex + 2, Object3DManager.MarsAtlas.Lobe(marsAtlasLabel));
                                    splittedLines[i].Insert(baseIndex + 3, Object3DManager.MarsAtlas.NameFS(marsAtlasLabel));
                                    splittedLines[i].Insert(baseIndex + 4, Object3DManager.MarsAtlas.FullName(marsAtlasLabel));
                                    splittedLines[i].Insert(baseIndex + 5, Object3DManager.MarsAtlas.BrodmannArea(marsAtlasLabel));
                                }
                            }
                        }
                    }

                    // Create tags and tagValues
                    List<string> tagNames = splittedLines[0];
                    TagImportObservations observations = new();
                    for (int row = 1; row < splittedLines.Count; row++)
                    {
                        for (int column = 0; column < tagNames.Count && column < splittedLines[row].Count; column++)
                        {
                            string normalizedName = tagNames[column].Trim();
                            if (!normalizedName.Equals("mni", StringComparison.OrdinalIgnoreCase) && !normalizedName.Equals("contact", StringComparison.OrdinalIgnoreCase) && !normalizedName.Equals("t1pre scanner based", StringComparison.OrdinalIgnoreCase))
                            {
                                observations.AddSiteValue(normalizedName, splittedLines[row][column]);
                            }
                        }
                    }

                    if (createMissingTags) observations.CreateMissingTags(tagCollection, policy);
                    BaseTag[] tags = new BaseTag[tagNames.Count];
                    for (int i = 0; i < tagNames.Count; i++)
                    {
                        string tagName = tagNames[i].Trim();
                        BaseTag tag = null;
                        if (!tagName.Equals("mni", StringComparison.OrdinalIgnoreCase) && !tagName.Equals("contact", StringComparison.OrdinalIgnoreCase) && !tagName.Equals("t1pre scanner based", StringComparison.OrdinalIgnoreCase))
                        {
                            tag = tagCollection.SitesTags.Concat(tagCollection.GeneralTags).FirstOrDefault(t => string.Equals(t.Name?.Trim(), tagName, StringComparison.OrdinalIgnoreCase));
                        }

                        tags[i] = tag;
                    }

                    for (int l = 1; l < splittedLines.Count; l++)
                    {
                        List<string> values = splittedLines[l];
                        string name = SiteNameCorrection ? SiteTools.FixName(values[0]) : values[0];
                        List<BaseTagValue> tagValues = new();
                        for (int i = 1; i < values.Count; i++)
                        {
                            BaseTag tag = tags[i];
                            string value = values[i];
                            if (tag != null)
                            {
                                RawTagValueResult creation = importContext?.TryCreate(TagCategory.Site, tag, value, csvFile, name) ?? RawTagValueFactory.TryCreate(tag, value, policy);
                                if (creation.Status == RawTagValueStatus.Success)
                                {
                                    tagValues.Add(creation.Value);
                                }
                            }
                        }

                        sites.Add(new Site(name, new Coordinate[0], tagValues));
                    }
                }
            }

            return sites;
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
            Site clone = new(Name, Coordinates.DeepClone(), Tags.DeepClone(), ID)
            {
            };
            return clone;
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
            FileInfo fileInfo = new(path);
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

                result = LoadSitesFromPTSFile(referenceSystem, path).ToArray();
                return true;
            }
            else if (fileInfo.Extension == ".tsv")
            {
                string name = path.Split('_').FirstOrDefault(s => s.Contains("space"))?.Split('-')[1];
                if (string.IsNullOrEmpty(name))
                {
                    name = "scanner";
                }

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
