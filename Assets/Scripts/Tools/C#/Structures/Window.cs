using UnityEngine;

namespace Tools.CSharp
{
    public struct Window
    {
        #region Properties
        public int Start { get; set; }
        public int End { get; set; }
        #endregion

        #region Constructors
        public Window(Vector2Int position)
        {
            Start = position.x;
            End = position.y;
        }
        public Window(int start,int end)
        {
            Start = start;
            End = end;
        }
        #endregion

        #region Public Methods
        public override bool Equals(object obj)
        {
            if (obj is Window)
            {
                Window window = (Window)obj;
                if (window.Start == Start && window.End == End)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        public override int GetHashCode()
        {
            return Start.GetHashCode() * End.GetHashCode();
        }
        public static bool operator ==(Window window1, Window window2)
        {
            return window1.Equals(window2);
        }
        public static bool operator !=(Window window1, Window window2)
        {
            return !window1.Equals(window2);
        }
        public Vector2 ToVector2()
        {
            return new Vector2(Start, End);
        }
        public Vector2Int ToVector2Int()
        {
            return new Vector2Int(Start, End);
        }
        #endregion
    }

}

