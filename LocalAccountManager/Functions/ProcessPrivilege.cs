using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace LocalAccountManager.Functions
{
    internal class ProcessPrivilege
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public LUID_AND_ATTRIBUTES[] Privileges;
        }

        private const uint SE_PRIVILEGE_ENABLED = 0x00000002u;

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(nint ProcessHandle, TokenAccessLevels DesiredAccess, out nint TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(nint TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint BufferLength, nint PreviousState, nint ReturnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(nint handle);

        [Flags]
        private enum TokenAccessLevels : uint
        {
            AssignPrimary = 0x00000001,
            Duplicate = 0x00000002,
            Impersonate = 0x00000004,
            Query = 0x00000008,
            QuerySource = 0x00000010,
            AdjustPrivileges = 0x00000020,
            AdjustGroups = 0x00000040,
            AdjustDefault = 0x00000080,
            AdjustSessionId = 0x00000100,

            AllAccess = (AssignPrimary |
                Duplicate |
                Impersonate |
                Query |
                QuerySource |
                AdjustPrivileges |
                AdjustGroups |
                AdjustDefault |
                AdjustSessionId),

            Read = (Query | QuerySource),
            Write = (AdjustPrivileges |
                AdjustGroups |
                AdjustDefault),
        }

        public static void AdjustToken(string privilegeName)
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                nint token;
                if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TokenAccessLevels.AdjustPrivileges | TokenAccessLevels.Query, out token))
                    throw new Exception("Failed to obtain process token.");
                try
                {
                    LUID luid;
                    if (!LookupPrivilegeValue(null, privilegeName, out luid))
                        throw new Exception("Failed to obtain privilege ID for " + privilegeName + ".");

                    var tokenPrivs = new TOKEN_PRIVILEGES();
                    tokenPrivs.PrivilegeCount = 1;
                    tokenPrivs.Privileges = new LUID_AND_ATTRIBUTES[1];
                    tokenPrivs.Privileges[0].Luid = luid;
                    tokenPrivs.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;

                    if (!AdjustTokenPrivileges(token, false, ref tokenPrivs, 0, nint.Zero, nint.Zero))
                        throw new Exception("Failed to enable privilege: " + privilegeName);
                }
                finally
                {
                    CloseHandle(token);
                }
            }
        }
    }

    //  参考
    //  https://learn.microsoft.com/ja-jp/windows/win32/secauthz/privilege-constants
    internal class Privilege
    {
        public const string SeCreateTokenPrivilege = "SeCreateTokenPrivilege";
        public const string SeAssignPrimaryTokenPrivilege = "SeAssignPrimaryTokenPrivilege";
        public const string SeLockMemoryPrivilege = "SeLockMemoryPrivilege";
        public const string SeIncreaseQuotaPrivilege = "SeIncreaseQuotaPrivilege";
        public const string SeUnsolicitedInputPrivilege = "SeUnsolicitedInputPrivilege";
        public const string SeMachineAccountPrivilege = "SeMachineAccountPrivilege";
        public const string SeTcbPrivilege = "SeTcbPrivilege";
        public const string SeSecurityPrivilege = "SeSecurityPrivilege";
        public const string SeTakeOwnershipPrivilege = "SeTakeOwnershipPrivilege";
        public const string SeLoadDriverPrivilege = "SeLoadDriverPrivilege";
        public const string SeSystemProfilePrivilege = "SeSystemProfilePrivilege";
        public const string SeSystemtimePrivilege = "SeSystemtimePrivilege";
        public const string SeProfileSingleProcessPrivilege = "SeProfileSingleProcessPrivilege";
        public const string SeIncreaseBasePriorityPrivilege = "SeIncreaseBasePriorityPrivilege";
        public const string SeCreatePagefilePrivilege = "SeCreatePagefilePrivilege";
        public const string SeCreatePermanentPrivilege = "SeCreatePermanentPrivilege";
        public const string SeBackupPrivilege = "SeBackupPrivilege";
        public const string SeRestorePrivilege = "SeRestorePrivilege";
        public const string SeShutdownPrivilege = "SeShutdownPrivilege";
        public const string SeDebugPrivilege = "SeDebugPrivilege";
        public const string SeAuditPrivilege = "SeAuditPrivilege";
        public const string SeSystemEnvironmentPrivilege = "SeSystemEnvironmentPrivilege";
        public const string SeChangeNotifyPrivilege = "SeChangeNotifyPrivilege";
        public const string SeRemoteShutdownPrivilege = "SeRemoteShutdownPrivilege";
        public const string SeUndockPrivilege = "SeUndockPrivilege";
        public const string SeSyncAgentPrivilege = "SeSyncAgentPrivilege";
        public const string SeEnableDelegationPrivilege = "SeEnableDelegationPrivilege";
        public const string SeManageVolumePrivilege = "SeManageVolumePrivilege";
        public const string SeImpersonatePrivilege = "SeImpersonatePrivilege";
        public const string SeCreateGlobalPrivilege = "SeCreateGlobalPrivilege";
        public const string SeTrustedCredManAccessPrivilege = "SeTrustedCredManAccessPrivilege";
        public const string SeRelabelPrivilege = "SeRelabelPrivilege";
        public const string SeIncreaseWorkingSetPrivilege = "SeIncreaseWorkingSetPrivilege";
        public const string SeTimeZonePrivilege = "SeTimeZonePrivilege";
        public const string SeCreateSymbolicLinkPrivilege = "SeCreateSymbolicLinkPrivilege";
    }
}
