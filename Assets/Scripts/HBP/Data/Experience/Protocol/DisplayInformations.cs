using UnityEngine;
using System;

namespace HBP.Data.Experience.Protocol
{
    /// <summary>
    /// Class which define the display informations of a bloc.
    ///     - name.
    ///     - Row.
    ///     - Column.
    ///     - IllustrationPath.
    ///     - Sort.
    ///     - Window.
    ///     - BaseLine.
    /// </summary>
    [Serializable]
    public class DisplayInformations : ICloneable
    {
        #region Properties
        [SerializeField]
        private string name;
        public string Name { get { return name; } set { name = value; } }

        [SerializeField]
        private int row;
        public int Row { get { return row; } set { row = value; } }

        [SerializeField]
        private int column;
        public int Column { get { return column; } set { column = value; } }

        [SerializeField]
        private string image;
        public string Image { get { return image; } set { image = value; } }

        [SerializeField]
        private string sort;
        public string Sort { get { return sort; } set { sort = value; } }

        [SerializeField]
        private Vector2 window;
        public Vector2 Window { get { return window; } set { window = value; } }

        [SerializeField]
        private Vector2 baseLine;
        public Vector2 BaseLine { get { return baseLine; } set { baseLine = value; } }
        #endregion

        #region Constructors
        public DisplayInformations(int row, int col, string name, string illustrationPath, string sort, Vector2 window, Vector2 baseLine)
        {
            Row = row;
            Column = col;
            Name = name;
            Image = illustrationPath;
            Sort = sort;
            Window = window;
            BaseLine = baseLine;
        }
        public DisplayInformations(int row, int col) : this(row, col, string.Empty, string.Empty, string.Empty, Vector2.zero, Vector2.zero)
        {
        }
        public DisplayInformations() : this(0,0,string.Empty,string.Empty,string.Empty,Vector2.zero,Vector2.zero)
		{
        }
        #endregion

        #region Operators
        public object Clone()
        {
            return new DisplayInformations(Row, Column, Name.Clone() as string, Image.Clone() as string, Sort.Clone() as string, new Vector2(Window.x,Window.y), new Vector2(BaseLine.x,BaseLine.y));
        }
        public override bool Equals(object obj)
        {
            DisplayInformations p = obj as DisplayInformations;
            if (p == null)
            {
                return false;
            }
            else
            {
                return Name == p.Name && Row == p.Row && Column == p.Column && Image == p.Image && Sort == p.Sort && Window == p.Window && BaseLine == p.BaseLine;
            }
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public static bool operator ==(DisplayInformations a, DisplayInformations b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            return a.Equals(b);
        }
        public static bool operator !=(DisplayInformations a, DisplayInformations b)
        {
            return !(a == b);
        }
        #endregion
    }
}