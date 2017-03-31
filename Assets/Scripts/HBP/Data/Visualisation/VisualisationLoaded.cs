/**
* \class VisualisationLoaded
* \author Adrien Gannerie
* \version 1.0
* \date 12 janvier 2017
* \brief Static class which contains all the informations about what is loaded in the visualisation.
* 
* \details Static class which contains all the informations about what is loaded in the visualisation:
*   - Single patient visualisation.
*   - Single patient visualisation data.
*   - Single patient columns mask.
*   
*   - Multi patients visualisation.
*   - Multi patients visualisation data.
*   - Multi patients columns mask.
*/
public static class VisualisationLoaded
{
    /// <summary>
    /// Single patient visualisation loaded.
    /// </summary>
    public static HBP.Data.Visualisation.SinglePatientVisualisation SP_Visualisation;
    /// <summary>
    /// Single patient visualisation data loaded.
    /// </summary>
    public static HBP.Data.Visualisation.SinglePatientVisualisationData SP_VisualisationData;
    /// <summary>
    /// Single patient columns mask.
    /// </summary>
    public static bool[] SP_Columns;
    /// <summary>
    /// Multi patients visualisation loaded.
    /// </summary>
    public static HBP.Data.Visualisation.MultiPatientsVisualisation MP_Visualisation;
    /// <summary>
    /// Multi patients visualisation data loaded.
    /// </summary>
    public static HBP.Data.Visualisation.MultiPatientsVisualisationData MP_VisualisationData;
    /// <summary>
    /// Multi patients columns mask.
    /// </summary>
    public static bool[] MP_Columns;
}