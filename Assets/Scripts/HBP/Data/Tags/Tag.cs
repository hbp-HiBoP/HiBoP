using System;

namespace HBP.Data.Tags
{
    public abstract class Tag : ICloneable, ICopiable
    {
        #region Properties
        public string Name { get; set; }
        #endregion

        #region Constructors
        public Tag() : this("")
        {
        }
        public Tag(string name)
        {
            Name = name;
        }
        #endregion

        #region Public Methods
        public abstract object Clone();
        public virtual void Copy(object copy)
        {
            Name = (copy as Tag).Name;
        }
        #endregion
    }
}