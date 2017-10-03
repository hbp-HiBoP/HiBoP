using UnityEngine;
using System;
using Tools.CSharp;
using Tools.Unity;

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
    public class Icon : ICloneable, ICopiable
    {
        #region Properties
        /// <summary>
        /// Icon name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Icon illustration path.
        /// </summary>
        public string IllustrationPath { get; set; }

        Sprite m_Image;
        public Sprite Image
        {
            get
            {
                if(!m_Image)
                {
                    m_Image = SpriteExtension.Load(IllustrationPath);
                }
                return m_Image;
            }
        }

        /// <summary>
        /// Icon window.
        /// </summary>
        public Window Window { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of icon.
        /// </summary>
        /// <param name="label">Label of the icon.</param>
        /// <param name="path">Path of the icon illustration.</param>
        /// <param name="window">Window of the icon.</param>
        public Icon(string label, string path, Vector2 window)
        {
            Name = label;
            IllustrationPath = path;
            Window = new Window(window);
        }
        /// <summary>
        /// Create a new instance of icon with default value.
        /// </summary>
        public Icon() : this(string.Empty,string.Empty,Vector2.zero)
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
            return new Icon(Name.Clone() as string, IllustrationPath.Clone() as string, new Vector2(Window.Start, Window.End));
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