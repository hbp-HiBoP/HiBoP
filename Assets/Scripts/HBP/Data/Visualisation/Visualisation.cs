using System;
using System.Collections.Generic;
using HBP.Data.Experience.Dataset;
using System.Runtime.Serialization;

namespace HBP.Data.Visualisation
{
    /**
    * \class Visualisation
    * \author Adrien Gannerie
    * \version 1.0
    * \date 12 janvier 2017
    * \brief Visualisation is a class which define a brain visualisation.
    * 
    * \details Visualisation is a ckass which define a brain visualiation and contains:
    *   - \a Name of the visualisation.
    *   - \a Unique ID.
    *   - \a Columns of the visualisation.   
    */
    [DataContract]
    public abstract class Visualisation : ICloneable , ICopiable
    {
        #region Properties
        [DataMember(Order = 1)]     
        /// <summary>
        /// Unique ID.
        /// </summary>
        public string ID { get; private set; }
        [DataMember(Order = 2)]
        /// <summary>
        /// Name of the visualisation.
        /// </summary>
        public string Name { get; set; }
        [DataMember(Order = 4)]
        /// <summary>
        /// Columns of the visualisation.
        /// </summary>
        public List<Column> Columns { get; set; }
        public abstract bool IsVisualisable { get; }
        #endregion

        #region Constructor
        /// <summary>
        /// Create a new visualisation instance.
        /// </summary>
        /// <param name="name">Name of the visualisation.</param>
        /// <param name="columns">Columns of the visualisation.</param>
        /// <param name="id">Unique ID.</param>
        protected Visualisation(string name, List<Column> columns, string id)
        {
            ID = id;
            Name = name;
            Columns = columns;
        }
        /// <summary>
        /// Create a new visualisation instance.
        /// </summary>
        /// <param name="name">Name of the visualisation.</param>
        /// <param name="columns">Columns of the visualisation.</param>
        protected Visualisation(string name, List<Column> columns) : this(name,columns,Guid.NewGuid().ToString())
        {
        }
        /// <summary>
        /// Create a new visualisation instance with default value.
        /// </summary>
        protected Visualisation() : this("New visualisation",new List<Column>())
        {
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Swap two columns by index.
        /// </summary>
        /// <param name="index1">Index of the first column to swap.</param>
        /// <param name="index2">Index of the second column to swap.</param>
        public void SwapColumns(int index1,int index2)
        {
            Column tmp = Columns[index1];
            Columns[index1] = Columns[index2];
            Columns[index2] = tmp;
        }
        /// <summary>
        /// Get the DataInfo of the column.
        /// </summary>
        /// <param name="column">Column</param>
        /// <returns>DataInfo of the column.</returns>
        public abstract DataInfo[] GetDataInfo(Column column);
        /// <summary>
        /// Copy a Visualisation instance in this visualisation.
        /// </summary>
        /// <param name="copy">Instance to copy.</param>
        public virtual void Copy(object copy)
        {
            Visualisation visualisation = copy as Visualisation;
            Name = visualisation.Name;
            Columns = visualisation.Columns;
            ID = visualisation.ID;
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Clone of this instance.</returns>
        public abstract object Clone();
        #endregion
    }
}