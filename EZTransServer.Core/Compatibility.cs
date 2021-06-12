using System.Runtime.InteropServices;

namespace EZTransServer.Core
{
    public static class Compatibility
    {
        public static bool IsCompatible()
        {
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            return isWindows;
        }
    }
}
