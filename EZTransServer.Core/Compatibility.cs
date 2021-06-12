using System.Runtime.InteropServices;

namespace EZTransServer.Core
{
    /// <summary>
    /// It provides compatibility check.
    /// </summary>
    public static class Compatibility
    {
        /// <summary>
        /// Checks whether library features are compatible with this platform.
        /// </summary>
        /// <returns>Result</returns>
        public static bool IsCompatible()
        {
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            return isWindows;
        }
    }
}
