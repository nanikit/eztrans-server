using System.Runtime.InteropServices;
using System.Text;

namespace EztransServer.Core
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

        /// <summary>
        /// Registers code pages.
        /// </summary>
        public static void RegisterCodePages()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
    }
}
