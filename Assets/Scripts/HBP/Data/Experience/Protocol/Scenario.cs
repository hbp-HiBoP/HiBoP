using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace HBP.Data.Experience.Protocol
{
    /// <summary>
    /// Class which define a iconic scenario.
    ///     - Icons.
    /// </summary>
    [Serializable]
    public class Scenario : ICloneable
    {
        #region Properties
        [SerializeField]
        List<Icon> icons;
        public ReadOnlyCollection<Icon> Icons
        {
            get { return new ReadOnlyCollection<Icon>(icons); }
        }
        #endregion

        #region Constructors
        public Scenario(List<Icon> icons)
        {
            this.icons = icons;
        }
        public Scenario() : this(new List<Icon>())
        {
        }
        #endregion

        #region Public methods
        public void Set(Icon[] icons)
        {
            this.icons = new List<Icon>(icons);
        }
        public void Add(Icon icon)
        {
            if(!icons.Contains(icon)) icons.Add(icon);
        }
        public void Add(Icon[] icons)
        {
            foreach (Icon icon in icons) Add(icon);
        }
        public void Remove(Icon icon)
        {
            icons.Remove(icon);
        }
        public void Remove(Icon[] icons)
        {
            foreach (Icon icon in icons) Remove(icon);
        }
        public void Clear()
        {
            icons = new List<Icon>();
        }
        #endregion

        #region Operator
        public object Clone()
        {
            List<Icon> l_list = new List<Icon>(Icons.Count);
            for (int i = 0; i < Icons.Count; i++)
            {
                l_list.Add(Icons[i].Clone() as Icon);
            }
            return new Scenario(l_list);
        }
        public override bool Equals(object obj)
        {
            Scenario p = obj as Scenario;
            if (p == null)
            {
                return false;
            }
            else
            {
                return System.Linq.Enumerable.SequenceEqual(Icons,p.Icons);
            }
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public static bool operator ==(Scenario a, Scenario b)
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
        public static bool operator !=(Scenario a, Scenario b)
        {
            return !(a == b);
        }
        #endregion
    }
}
