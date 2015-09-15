using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using log4net;
using Microsoft.Win32.SafeHandles;


namespace GeniusX.AXACS.WindowsConsole.Facade
{
    public class ImpersonateUser
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(ImpersonateUser));
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LogonUser(String lpszUsername, String lpszDomain, String lpszPassword,int logonType, int logonProvider, out SafeTokenHandle token);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool CloseHandle(IntPtr handle);

        public WindowsImpersonationContext SetImpersonatedUser(string userName, string domainName, string password)
        {
            WindowsImpersonationContext impersonatedUser = null;
            const int LOGON32_PROVIDER_DEFAULT = 0;
            ////This parameter causes LogonUser to create a primary token.
            const int LOGON32_LOGON_INTERACTIVE = 2;

            try
            {
                SafeTokenHandle safeTokenHandle;
                //// Call LogonUser to obtain a handle to an access token.
                bool returnValue = LogonUser(userName, domainName, password,LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT,out safeTokenHandle);

                using (safeTokenHandle)
                {
                    //// Check the identity.
                    Console.WriteLine("Before impersonation: "
                        + WindowsIdentity.GetCurrent().Name);
                    //// Use the token handle returned by LogonUser.
                    WindowsIdentity newId = new WindowsIdentity(safeTokenHandle.DangerousGetHandle());
                    impersonatedUser = newId.Impersonate();
                }

                return impersonatedUser;
            }
            catch (Exception ex)
            {
                logger.Error(string.Format("SetImpersonatedUser - {0}", ex.Message));
                return impersonatedUser;
            }
        }

        public sealed class SafeTokenHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private SafeTokenHandle()
                : base(true)
            {
            }

            [DllImport("kernel32.dll")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [SuppressUnmanagedCodeSecurity]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool CloseHandle(IntPtr handle);

            protected override bool ReleaseHandle()
            {
                return CloseHandle(handle);
            }
        }
    }
}
