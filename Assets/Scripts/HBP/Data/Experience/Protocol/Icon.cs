using UnityEngine;
using System;

namespace HBP.Data.Experience.Protocol
{
    [Serializable]
    public class Icon : ICloneable
    {
        #region Properties
        [SerializeField]
        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [SerializeField]
        private string image;
        public string Image
        {
            get { return image; }
            set { image = value; }
        }

        [SerializeField]
        private Vector2 window;
        public Vector2 Window
        {
            get { return window; }
            set { window = value; }
        }
        #endregion

        #region Constructors
        public Icon(string label, string path, Vector2 window)
        {
            Name = label;
            Image = path;
            Window = window;
        }
        public Icon() : this(string.Empty,string.Empty,Vector2.zero)
        {
        }
        #endregion

        #region Operators
        public object Clone()
        {
            return new Icon(Name.Clone() as string, Image.Clone() as string, new Vector2(Window.x, Window.y));
        }
        #endregion
    }
}
