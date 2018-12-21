using UnityEngine;
using System;
using Tools.CSharp;
using Tools.Unity;
using System.Runtime.Serialization;
using System.IO;

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
    public class Icon : ICloneable, ICopiable
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
                if(!m_Image)
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
        /// <summary>
        /// Create a new instance of icon.
        /// </summary>
        /// <param name="label">Label of the icon.</param>
        /// <param name="path">Path of the icon illustration.</param>
        /// <param name="window">Window of the icon.</param>
        public Icon(string label, string path, Vector2Int window)
        {
            Name = label;
            IllustrationPath = path;
            Window = new Window(window);
        }
        /// <summary>
        /// Create a new instance of icon with default value.
        /// </summary>
        public Icon() : this(string.Empty,string.Empty, new Vector2Int(-300,300))
        {
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone the icon instance.
        /// </summary>
        /// <returns>Icon clone.</returns>
        public object Clone()
        {
            return new Icon(Name.Clone() as string, IllustrationPath.Clone() as string, new Vector2Int(Window.Start, Window.End));
        }

        public void Copy(object copy)
        {
            Icon icon = copy as Icon;
            Name = icon.Name;
            IllustrationPath = icon.IllustrationPath;
            Window = icon.Window;
        }
        #endregion
    }
}