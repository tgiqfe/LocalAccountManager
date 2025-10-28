using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Security;

namespace LocalAccountManager.LocalAccount
{
    internal class LogonChecker
    {
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword,
            int dwLogonType, int dwLogonProvider, out SafeTokenHandle phToken);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private extern static bool CloseHandle(nint handle);

        private static bool GetImpersonationContext(string userName, string domainName, string password)
        {
            const int LOGON32_PROVIDER_DEFAULT = 0;
            const int LOGON32_LOGON_INTERACTIVE = 2;
            bool resultBool = false;
            try
            {
                SafeTokenHandle safeTokenHandle;
                bool returnValue = LogonUser(userName, domainName, password, LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT, out safeTokenHandle);
                if (returnValue) { resultBool = true; }
            }
            catch (Exception) { throw; }
            return resultBool;
        }

        private sealed class SafeTokenHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private SafeTokenHandle() : base(true) { }

            [DllImport("kernel32.dll")]
            //[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [SuppressUnmanagedCodeSecurity]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool CloseHandle(nint handle);

            protected override bool ReleaseHandle()
            {
                return CloseHandle(handle);
            }
        }

        public static bool IsEnabled(string userName, string password, string domainName = null)
        {
            if (domainName == null)
            {
                if (userName.Contains("\\"))
                {
                    domainName = userName.Substring(0, userName.IndexOf("\\"));
                    userName = userName.Substring(userName.IndexOf("\\") + 1);
                }
                else if (userName.Contains("@"))
                {
                    domainName = userName.Substring(userName.IndexOf("@") + 1);
                    userName = userName.Substring(0, userName.IndexOf("@"));
                }
                else
                {
                    domainName = "";
                }
                try
                {
                    return GetImpersonationContext(userName, domainName, password);
                }
                catch { }
            }
            return false;
        }
    }
}
