using UnityEngine;
using System.Runtime.Serialization;
using HBP.Core.Interfaces;
using HBP.Core.Tools;

namespace HBP.Core.Data
{
    /// <summary>
    /// Class which contains all the data about a Icon.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>ID</b></term>
    /// <description>Unique identifier</description>
    /// </item>
    /// <item>
    /// <term><b>Name</b></term> 
    /// <description>Name of the icon</description>
    /// </item>
    /// <item>
    /// <term><b>Image path</b></term> 
    /// <description>Image path of the icon</description>
    /// </item>
    /// <item>
    /// <term><b>Image</b></term> 
    /// <description>Image of the icon</description>
    /// </item>
    /// <item>
    /// <term><b>Window</b></term> 
    /// <description>Window of the icon</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract]
    public class Icon : BaseData, INameable
    {
        #region Properties
        /// <summary>
        /// Name of the icon.
        /// </summary>
        [DataMember]
        public string Name { get; set; }
        /// <summary>
        /// Path to the image icon with Aliases.
        /// </summary>
        [DataMember(Name = "ImagePath")]
        private string m_ImagePath = "";
        /// <summary>
        /// Path of the image icon without Aliases.
        /// </summary>
        [IgnoreDataMember]
        public string ImagePath
        {
            get
            {
                return m_ImagePath.ConvertToFullPath();
            }
            set
            {
                m_ImagePath = value.ConvertToShortPath();
            }
        }

        Sprite m_Image;
        /// <summary>
        /// Image of the icon.
        /// </summary>
        public Sprite Image
        {
            get
            {
                if (!m_Image)
                {
                    Sprite sprite;
                    if (SpriteExtension.LoadSpriteFromFile(out sprite, ImagePath)) m_Image = sprite;
                }
                return m_Image;
            }
        }

        /// <summary>
        /// Temporal window when the icon is displayed.
        /// </summary>
        [DataMember]
        public TimeWindow Window { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new Icon instance.
        /// </summary>
        /// <param name="name">Name of the icon</param>
        /// <param name="path">Path of the image icon</param>
        /// <param name="window">Window when the icon is displayed</param>
        public Icon(string name, string path, TimeWindow window) : base()
        {
            Name = name;
            ImagePath = path;
            Window = window;
        }
        /// <summary>
        /// Create a new Icon instance.
        /// </summary>
        /// <param name="name">Name of the icon</param>
        /// <param name="path">Path of the image icon</param>
        /// <param name="window">Window when the icon is displayed</param>
        /// <param name="ID">Unique identifier</param>
        public Icon(string name, string path, TimeWindow window, string ID) : base(ID)
        {
            Name = name;
            ImagePath = path;
            Window = window;
        }
        /// <summary>
        /// Create a new Icon instance.
        /// </summary>
        /// <param name="name">Name of the icon</param>
        /// <param name="path">Path of the image icon</param>
        /// <param name="window">Window when the icon is displayed</param>
        /// <param name="ID">Unique identifier</param>
        public Icon(string name, string path, Vector2Int window, string ID) : this(name, path, new TimeWindow(window), ID)
        {
        }
        /// <summary>
        /// Create a new Icon instance with default value.
        /// </summary>
        public Icon() : this("New Icon", string.Empty, new TimeWindow(-300, 300))
        {
        }
        /// <summary>
        /// Create a new Icon instance with a specified window.
        /// </summary>
        /// <param name="window">Window when the icon is displayed</param>
        public Icon(TimeWindow window): this("New Icon", string.Empty, window)
        {
            
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone the icon instance.
        /// </summary>
        /// <returns>Icon clone.</returns>
        public override object Clone()
        {
            return new Icon(Name, ImagePath, Window, ID);
        }
        public override void Copy(object obj)
        {
            base.Copy(obj);
            if (obj is Icon icon)
            {
                Name = icon.Name;
                ImagePath = icon.ImagePath;
                Window = icon.Window;
            }
        }
        #endregion

        #region Serialization
        protected override void OnDeserialized()
        {
            m_ImagePath = m_ImagePath.StandardizeToEnvironement();
        }
        #endregion
    }
}