namespace HBP.Core
{
    /// <summary>
    /// Interface for the configurable classes
    /// </summary>
    public interface IConfigurable
    {
        /// <summary>
        /// Load the configuration from a data class
        /// </summary>
        void LoadConfiguration(bool firstCall);
        /// <summary>
        /// Save the current configuration to a data class
        /// </summary>
        void SaveConfiguration();
        /// <summary>
        /// Reset the configuration
        /// </summary>
        void ResetConfiguration();
    }
}