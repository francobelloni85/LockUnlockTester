using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LockUnlock
{
    public static class LockUnlockHelper
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static bool DisableUSBStorage()
        {
            var result = false;
            try
            {
                //disable USB storage...
                Microsoft.Win32.Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\USBSTOR", "Start", 4, Microsoft.Win32.RegistryValueKind.DWord);
                result = true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return result;

        }



        public static bool EnableUSBStorage()
        {
            var result = false;
            try
            {
                //enable USB storage...
                Microsoft.Win32.Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\USBSTOR", "Start", 3, Microsoft.Win32.RegistryValueKind.DWord);
                result = true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return result;
        }

    }
}
