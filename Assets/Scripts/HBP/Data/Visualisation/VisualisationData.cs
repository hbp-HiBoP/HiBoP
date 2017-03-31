using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using HBP.Data.Anatomy;

namespace HBP.Data.Visualisation
{
    /**
    * \class VisualisationData
    * \author Adrien Gannerie
    * \version 1.0
    * \date 12 janvier 2017
    * \brief Class which contains the visualisation data.
    * 
    * \details visualisation data is a class which contains the data of a visualisation and contains :
    *   - \a Columns.
    *   - \a Plots unique ID.
    */
    public abstract class VisualisationData
    {
        #region Properties
        /// <summary>
        /// Columns of the visualisation.
        /// </summary>
        public List<ColumnData> Columns { get; set; }
        /// <summary>
        /// PlotsID of the visualisation.
        /// </summary>
        public List<PlotID> PlotsID { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new visualisation data instance.
        /// </summary>
        /// <param name="columns">Columns of the visualisation.</param>
        public VisualisationData(ColumnData[] columns)
        {
            Columns = columns.ToList();
        }
        #endregion

        #region  Public Methods
        /// <summary>
        /// Standardize the data of all the columns.
        /// </summary>
        public void StandardizeColumns()
        {
            int before = Mathf.Max((from c in Columns select c.TimeLine.MainEvent.Position).ToArray());
            int after = Mathf.Max((from c in Columns select (c.TimeLine.Lenght-c.TimeLine.MainEvent.Position)).ToArray());
            foreach (ColumnData column in Columns) column.Standardize(before, after);
        }
        #endregion
    }
}