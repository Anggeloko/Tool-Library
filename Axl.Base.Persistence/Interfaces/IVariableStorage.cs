using System;

namespace Axl.Base.Persistence.Interfaces
{
    /// <summary>
    /// Interface for persistent storage of variables/configurations.
    /// </summary>
    public interface IVariableStorage
    {
        bool UseCompression { get; set; }

        /// <summary>
        /// Loads a variable of type T.
        /// </summary>
        /// <typeparam name="T">Type of the variable.</typeparam>
        /// <param name="key">The unique key/name of the variable.</param>
        /// <param name="subDir">Subdirectory within the base path (default: "Data").</param>
        /// <returns>The deserialized object or default(T) if not found or error.</returns>
        T Load<T>(string key, string subDir = "Data") where T : new();

        /// <summary>
        /// Saves a variable of type T.
        /// </summary>
        /// <typeparam name="T">Type of the variable.</typeparam>
        /// <param name="key">The unique key/name of the variable.</param>
        /// <param name="value">The object to save.</param>
        /// <param name="subDir">Subdirectory within the base path (default: "Data").</param>
        void Save<T>(string key, T value, string subDir = "Data");

        /// <summary>
        /// Deletes a variable.
        /// </summary>
        /// <param name="key">The unique key/name of the variable.</param>
        /// <param name="subDir">Subdirectory within the base path (default: "Data").</param>
        void Delete(string key, string subDir = "Data");
    }
}
