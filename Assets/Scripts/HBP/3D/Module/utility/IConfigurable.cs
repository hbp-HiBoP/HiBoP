namespace HBP.Module3D
{
    public interface IConfigurable
    {
        /// <summary>
        /// Load the visualization configuration from the loaded visualization
        /// </summary>
        void LoadConfiguration();
        /// <summary>
        /// Save the current settings of this scene to the configuration of the linked visualization
        /// </summary>
        void SaveConfiguration();
        /// <summary>
        /// Reset the settings of the loaded scene
        /// </summary>
        void ResetConfiguration();
    }
}