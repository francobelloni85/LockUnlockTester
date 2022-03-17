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

        public static void SetDeviceEnabled(Guid mouseGuid, string DeviceID, bool enable)
        {
            DeviceHelper.SetDeviceEnabled(mouseGuid, DeviceID, enable);
        }

        #endregion


        #region Virtual keyboard

        public static void EnableVirtualKeyboard()
        {
            var command = "sc config \"TabletInputService\" start= disabled";
            Process.Start("cmd.exe", "/C " + command);
            command = "sc stop \"TabletInputService\"";
            Process.Start("cmd.exe", "/C " + command);
        }

        public static void DisableVirtualKeyboard()
        {
            var command = "sc config \"TabletInputService\" start= auto";
            Process.Start("cmd.exe", "/C " + command);
            command = "sc start \"TabletInputService\"";
            Process.Start("cmd.exe", "/C " + command);
        }


        #endregion


        #region aaa



        #endregion

    }
}
