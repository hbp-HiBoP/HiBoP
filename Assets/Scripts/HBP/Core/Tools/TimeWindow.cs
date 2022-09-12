using System;
using System.Runtime.Serialization;
using UnityEngine;

namespace HBP.Core.Tools
{
    [DataContract]
    public struct TimeWindow
    {
        #region Properties
        [DataMember] public int Start { get; set; }
        [DataMember] public int End { get; set; }
        [IgnoreDataMember] public int Length
        {
            get
            {
                return End - Start;
            }
        }
        #endregion

        #region Constructors
        public TimeWindow(Vector2Int position)
        {
            Start = position.x;
            End = position.y;
        }
        public TimeWindow(int start,int end)
        {
            Start = start;
            End = end;
        }
        #endregion

        #region Public Methods
        public override string ToString()
        {
            return String.Format("({0},{1})", Start, End);
        }
        public override bool Equals(object obj)
        {
            if (obj is TimeWindow)
            {
                TimeWindow window = (TimeWindow)obj;
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
        public static bool operator ==(TimeWindow window1, TimeWindow window2)
        {
            return window1.Equals(window2);
        }
        public static bool operator !=(TimeWindow window1, TimeWindow window2)
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

