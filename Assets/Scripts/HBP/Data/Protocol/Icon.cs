using UnityEngine;
using Tools.CSharp;
using Tools.Unity;
using System.Runtime.Serialization;

namespace HBP.Data.Experience.Protocol
{
    /**
    * \class Icon
    * \author Adrien Gannerie
    * \version 1.0
    * \date 09 janvier 2017
    * \brief Icon of the iconic Scenario.
    * 
    * \details Class which define a Icon of the iconic Scenario which contains :
    *     - \a Label.
    *     - \a Illustration \a path.
    *     - \a Window.
    */
    [DataContract]
    public class Icon : BaseData, INameable
    {
        #region Properties
        /// <summary>
        /// Icon name.
        /// </summary>
        [DataMember]
        public string Name { get; set; }
        [DataMember(Name = "IllustrationPath")]
        private string m_IllustrationPath = "";
        /// <summary>
        /// Icon illustration path.
        /// </summary>
        [IgnoreDataMember]
        public string IllustrationPath
        {
            get
            {
                return m_IllustrationPath.ConvertToFullPath();
            }
            set
            {
                m_IllustrationPath = value.ConvertToShortPath();
            }
        }

        Sprite m_Image;
        public Sprite Image
        {
            get
            {
                if (!m_Image)
                {
                    Sprite sprite;
                    if (SpriteExtension.LoadSpriteFromFile(out sprite, IllustrationPath)) m_Image = sprite;
                }
                return m_Image;
            }
        }

        /// <summary>
        /// Icon window.
        /// </summary>
        [DataMember]
        public Window Window { get; set; }
        #endregion

        #region Constructors
        public Icon(string name, string path, Window window) : base()
        {
            Name = name;
            IllustrationPath = path;
            Window = window;
        }
        public Icon(string name, string path, Window window, string ID) : base(ID)
        {
            Name = name;
            IllustrationPath = path;
            Window = window;
        }
        public Icon(string name, string path, Vector2Int window, string ID) : this(name, path, new Window(window), ID)
        {
        }
        /// <summary>
        /// Create a new instance of icon.
        /// </summary>
        /// <param name="name">Label of the icon.</param>
        /// <param name="path">Path of the icon illustration.</param>
        /// <param name="window">Window of the icon.</param>
        public Icon(string name, string path, Vector2Int window) : this(name, path, new Window(window))
        {
        }
        /// <summary>
        /// Create a new instance of icon with default value.
        /// </summary>
        public Icon() : this("New Icon", string.Empty, new Vector2Int(-300, 300))
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
            return new Icon(Name, IllustrationPath, Window, ID);
        }
        public override void Copy(object obj)
        {
            base.Copy(obj);
            if (obj is Icon icon)
            {
                Name = icon.Name;
                IllustrationPath = icon.IllustrationPath;
                Window = icon.Window;
            }
        }
        #endregion

        #region Serialization
        [OnDeserialized()]
        public void OnDeserialized(StreamingContext context)
        {
            m_IllustrationPath = m_IllustrationPath.ToPath();
        }
        #endregion
    }
}