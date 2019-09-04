using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HBP.Data.Visualization
{
    /**
    * \class Column
    * \author Adrien Gannerie
    * \version 1.0
    * \date 21 septembre 2018
    * \brief Base class of visualization column.
    * 
    * \detail Visualization column is a class which contains the base information of the visualization column.
    */
    [DataContract]
    public abstract class Column : ICloneable
    {
        #region Properties
        /// <summary>
        /// Name of the column.
        /// </summary>
        [DataMember] public virtual string Name { get; set; }
        /// <summary>
        /// Base Configuration of the column.
        /// </summary>
        [DataMember] public virtual BaseConfiguration BaseConfiguration { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new column.
        /// </summary>
        /// <param name="name">Name of the column.</param>
        /// <param name="baseConfiguration">Base configuration of the column.</param>
        public Column(string name, BaseConfiguration baseConfiguration)
        {
            Name = name;
            BaseConfiguration = baseConfiguration;
        }
        /// <summary>
        /// Create a new Column with default values.
        /// </summary>
        public Column():this("New column", new BaseConfiguration())
        {
        }
        #endregion

        #region Public Methods
        public abstract object Clone();
        public abstract bool IsCompatible(IEnumerable<Patient> patients);
        public abstract void Unload();
        #endregion
    }
}