using Microsoft.Win32;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;

namespace LockUnlock
{
    public static class LockUnlockHelper
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #region USB Mode Registry

        /// <summary>
        /// Read all the USB device found in Win32_USBHub
        /// </summary>
        /// <returns></returns>
        public static List<USBDeviceInfo> GetUSBDevices()
        {
            List<USBDeviceInfo> devices = new List<USBDeviceInfo>();

            ManagementObjectCollection collection;

            using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_USBHub"))
                collection = searcher.Get();

            var count = 1;
            foreach (var device in collection)
            {
                var deviceID = (string)device.GetPropertyValue("DeviceID");
                var PNPDeviceID = (string)device.GetPropertyValue("PNPDeviceID");
                var Description = (string)device.GetPropertyValue("Description");
                var Status = (string)device.GetPropertyValue("Status");
                devices.Add(new USBDeviceInfo(count, deviceID, PNPDeviceID, Description, Status, Status == "OK"));
                count++;
            }

            //foreach (ManagementObject queryObj in collection)
            //{
            //    Console.WriteLine("-----------------------------------");
            //    Console.WriteLine("Win32_USBHub instance");
            //    Console.WriteLine("-----------------------------------");
            //    Console.WriteLine("Availability: {0}", queryObj["Availability"]);
            //    Console.WriteLine("Caption: {0}", queryObj["Caption"]);
            //    Console.WriteLine("CurrentConfigValue: {0}", queryObj["CurrentConfigValue"]);
            //    Console.WriteLine("NumberOfPorts: {0}", queryObj["NumberOfPorts"]);
            //    Console.WriteLine("NumberOfConfigs: {0}", queryObj["NumberOfConfigs"]);
            //    Console.WriteLine("CurrentConfigValue: {0}", queryObj["CurrentConfigValue"]);
            //    Console.WriteLine("Status: {0}", queryObj["Status"]);
            //    Console.WriteLine("StatusInfo: {0}", queryObj["StatusInfo"]);
            //    Console.WriteLine("USBVersion: {0}", queryObj["USBVersion"]);
            //    Console.WriteLine("PNPDeviceID: {0}", queryObj["PNPDeviceID"]);
            //}

            collection.Dispose();
            return devices;
        }

        /// <summary>
        /// Try to read the USB status by reading in windows register
        /// </summary>
        /// <returns></returns>
        public static USBStatus ReadUSBStatusInRegistry()
        {
            try
            {
                RegistryKey rk = Registry.LocalMachine;
                var temp = rk.OpenSubKey("SYSTEM").OpenSubKey("CurrentControlSet").OpenSubKey("Services").OpenSubKey("USBSTOR");
                var value = (int)temp.GetValue("Start");
                return (USBStatus)value;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return USBStatus.NotSet;
        }


        /// <summary>
        /// Try to disable the USB by writing in windows register
        /// </summary>
        public static bool SetUSBbyRegistry(USBStatus status)
        {
            var result = false;

            try
            {
                //disable USB storage...
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\USBSTOR", "Start", (int)status, RegistryValueKind.DWord);
                result = true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return result;

        }

        #endregion

        #region USB Mode API

        /// <summary>
        /// Read all the USB device found in CIM_LogicalDevice
        /// </summary>
        public static List<ManagementBaseObject> GetLogicalDevices()
        {
            List<ManagementBaseObject> devices = new List<ManagementBaseObject>();
            ManagementObjectCollection collection;
            using (var searcher = new ManagementObjectSearcher("root\\CIMV2", @"Select * From CIM_LogicalDevice"))
                collection = searcher.Get();
            foreach (var device in collection)
            {
                devices.Add(device);
            }
            collection.Dispose();
            return devices;
        }

        /// <summary>
        /// Try to enable the all the USB by API
        /// </summary>
        public static bool SetUSBbyAPI(bool enable)
        {
            var result = false;
            try
            {
                var allPcDevices = LockUnlockHelper.GetLogicalDevices();

                foreach (var usbDevice in allPcDevices)
                {
                    if (usbDevice.GetPropertyValue("DeviceID").ToString().Contains("USB") && usbDevice.GetPropertyValue("Status").ToString().Contains("OK"))
                    {

                        // https://powershell.one/wmi/root/cimv2/cim_logicaldevice

                        try
                        {
                            Guid mouseGuid = new Guid(usbDevice.GetPropertyValue("ClassGuid").ToString());
                            string DeviceID = usbDevice.GetPropertyValue("DeviceID").ToString();
                            SetDeviceEnabled(mouseGuid, DeviceID, enable);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return result;

        }

        /// <summary>
        /// Try to enable a single the USB by API
        /// </summary>
        public static void SetDeviceEnabled(Guid mouseGuid, string DeviceID, bool enable)
        {
            DeviceHelper.SetDeviceEnabled(mouseGuid, DeviceID, enable);
        }

        #endregion


        #region Virtual keyboard

        /// <summary>
        /// Try to enable the virtual keyboard
        /// </summary>
        public static void EnableVirtualKeyboard()
        {
            var command = "sc config \"TabletInputService\" start= disabled";
            Process.Start("cmd.exe", "/C " + command);
            command = "sc stop \"TabletInputService\"";
            Process.Start("cmd.exe", "/C " + command);
        }

        /// <summary>
        /// Try to disable the virtual keyboard
        /// </summary>
        public static void DisableVirtualKeyboard()
        {
            var command = "sc config \"TabletInputService\" start= auto";
            Process.Start("cmd.exe", "/C " + command);
            command = "sc start \"TabletInputService\"";
            Process.Start("cmd.exe", "/C " + command);
        }


        #endregion


        #region TaskManager

        ///// <summary>
        ///// DEPRECATO
        ///// </summary>
        ///// <param name="enable"></param>
        //public static void SetTaskManagerByRegistry(bool enable)
        //{
        //    throw new Exception("Need to restart the pc, use SetTaskManagerByCmd");
        //    //// OLD Windows
        //    ////Microsoft.Win32.RegistryKey objRegistryKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System");

        //    //// windows 10 
        //    //Microsoft.Win32.RegistryKey objRegistryKey = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");

        //    //if (enable && objRegistryKey.GetValue("DisableTaskMgr") != null)
        //    //    objRegistryKey.DeleteValue("DisableTaskMgr");
        //    //else
        //    //    objRegistryKey.SetValue("DisableTaskMgr", "1");
        //    //objRegistryKey.Close();
        //}


        /// <summary>
        /// Try to disable the task manager in windows register
        /// </summary>
        public static void SetTaskManagerByCmd(bool enable)
        {
            // https://www.digitalcitizen.life/easily-enable-or-disable-task-manager-using-taskmgred/

            // Disable
            var command = "reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System /v DisableTaskMgr /t REG_DWORD /d 1 /f";

            if (enable)
            {
                // Enable
                command = "reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System /v DisableTaskMgr /t REG_DWORD /d 0 /f";
            }

            Process.Start("cmd.exe", "/C " + command);
        }

        #endregion


        #region TaskBar


        /// <summary>
        /// Try to hide/show the task bar in windows register
        /// </summary>
        public static void SetTaskBarByCmd(bool enable)
        {
            // Disable
            var value = 3;
            if (enable)
            {
                value = 2;
            }
            var command = "powershell -command \"&{$p='HKCU:SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StuckRects3';$v=(Get-ItemProperty -Path $p).Settings;$v[8]="+ value + ";&Set-ItemProperty -Path $p -Name Settings -Value $v;&Stop-Process -f -ProcessName explorer}\"";
            Process.Start("cmd.exe", "/C " + command);
        }

        #endregion



    }
}