using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BuildTask
{
    internal static class RegistryNative
    {
        [Flags]
        public enum RegSAM
        {
            AllAccess = 0x000f003f
            , CreateLink = 0x00000020
            , CreateSubKey = 0x00000004
            , EnumerateSubKeys = 0x00000008
            , Execute = 0x00020019
            , Notify = 0x00000010
            , QueryValue = 0x00000001
            , Read = 0x00020019
            , SetValue = 0x00000002
            , Wow64_32Key = 0x00000200
            , Wow64_64Key = 0x00000100
            , Write = 0x00020006
        }

        private const int REG_PROCESS_APPKEY = 0x00000001;

        // approximated from pinvoke.net's RegLoadKey and RegOpenKey
        // NOTE: changed return from long to int so we could do Win32Exception on it
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int RegLoadAppKey(String hiveFile, out int hKey, RegSAM samDesired, int options, int reserved);

        public static int RegLoadAppKey(String hiveFile)
        {
            int hKey;
            int rc = RegLoadAppKey(hiveFile, out hKey, RegSAM.Read, 0*REG_PROCESS_APPKEY, 0);

            if (rc != 0)
            {
                throw new Win32Exception(rc, "Failed during RegLoadAppKey of file " + hiveFile);
            }

            return hKey;
        }
    }
}
