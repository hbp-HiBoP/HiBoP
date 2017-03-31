namespace HBP.Data.Anatomy
{
    /**
    * \class PlotID
    * \author Adrien Gannerie
    * \version 1.0
    * \date 05 janvier 2017
    * \brief Electrode Plot.
    * 
    * \details Class which define a electrode plot  which contains:
    *   - \a Name.
    *   - \a Patient.
    */
    public class PlotID
    {
        #region Properties
        /** Name of the plot. */
        public string Name { get; set; }
        /** Patient  */
        public Patient Patient { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new Plot ID instance.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="patient"></param>
        public PlotID(string name,Patient patient)
        {
            Name = name;
            Patient = patient;
        }
        /// <summary>
        /// Create a new Plot ID with default values.
        /// </summary>
        public PlotID(): this("new plot",null)
        {
        }
        #endregion
    }
}