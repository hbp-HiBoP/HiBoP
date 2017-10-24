using UnityEngine;

namespace Tools.CSharp
{
    public struct Window
    {
        public float Start { get; set; }
        public float End { get; set; }

        public Window(Vector2 position)
        {
            Start = position.x;
            End = position.y;
        }
        public Window(float start,float end)
        {
            Start = start;
            End = end;
        }
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
    }

}

