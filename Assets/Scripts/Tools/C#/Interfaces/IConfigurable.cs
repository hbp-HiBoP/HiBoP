namespace HBP.Module3D
{
    /// <summary>
    /// Interface for the configurable classes
    /// </summary>
    public interface IConfigurable
    {
        /// <summary>
        /// Load the configuration from the saved configuration file
        /// </summary>
        void LoadConfiguration(bool firstCall);
        /// <summary>
        /// Save the current configuration to a file
        /// </summary>
        void SaveConfiguration();
        /// <summary>
        /// Reset the configuration
        /// </summary>
        void ResetConfiguration();
    }
}