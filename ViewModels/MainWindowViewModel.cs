﻿using CG.LockUnlock;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using LockUnlock;
using Microsoft.Win32;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

// https://stackoverflow.com/questions/3331043/get-list-of-connected-usb-devices

// https://dev.to/iamthecarisma/enabling-and-disabling-usb-disk-drive-using-c-4dg5

// https://helpdeskgeek.com/how-to/how-to-setup-windows-10-as-a-kiosk/

namespace CG.LockUnlockTester
{
    public class MainWindowViewModel : ViewModelBase
    {
        [DllImport("User32.dll")]
        internal static extern bool SetupDiEnumDeviceInfo(SafeDeviceInfoSetHandle deviceInfoSet, int memberIndex, ref DeviceInfoData deviceInfoData);
        [DllImport("User32.dll")]
        internal static extern bool SetupDiCallClassInstaller(DiFunction installFunction, SafeDeviceInfoSetHandle deviceInfoSet, [In()] ref DeviceInfoData deviceInfoData);
        [DllImport("User32.dll")]
        internal static extern SafeDeviceInfoSetHandle SetupDiGetClassDevs([In()] ref Guid classGuid, [MarshalAs(UnmanagedType.LPWStr)] string enumerator, IntPtr hwndParent, SetupDiGetClassDevsFlags flags);
        [DllImport("User32.dll")]
        internal static extern bool SetupDiGetDeviceInstanceId(IntPtr DeviceInfoSet, ref DeviceInfoData did, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder DeviceInstanceId, int DeviceInstanceIdSize, out int RequiredSize);
        [DllImport("User32.dll")]
        internal static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);
        [DllImport("User32.dll")]
        internal static extern bool SetupDiSetClassInstallParams(SafeDeviceInfoSetHandle deviceInfoSet, [In()] ref DeviceInfoData deviceInfoData, [In()] ref PropertyChangeParameters classInstallParams, int classInstallParamsSize);

        /// <summary>
        /// Logger
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private List<USBDeviceInfo> usbDevices = new List<USBDeviceInfo>();

        public List<USBDeviceInfo> USBDevices { get => usbDevices; set { usbDevices = value; base.RaisePropertyChanged(nameof(USBDevices)); } }

        public RelayCommand DisableUSBCommand { get; set; }

        public RelayCommand EnableUSBCommand { get; set; }
        public RelayCommand RefreshUSBListCommand { get; set; }

        private bool isAdmin;
        public bool IsAdmin { get => isAdmin; set { isAdmin = value; base.RaisePropertyChanged(nameof(IsAdmin)); } }

        public string userName;
        public string UserName { get => userName; set { userName = value; base.RaisePropertyChanged(nameof(UserName)); } }

        public string userMessage;
        public string UserMessage { get => userMessage; set { userMessage = value; base.RaisePropertyChanged(nameof(UserMessage)); } }

        

        public bool isDisableUSBCommandEnable;
        public bool IsDisableUSBCommandEnable { get => isDisableUSBCommandEnable; set { isDisableUSBCommandEnable = value; base.RaisePropertyChanged(nameof(IsDisableUSBCommandEnable)); } }

        public bool isEnableUSBCommand;
        public bool IsEnableUSBCommand { get => isEnableUSBCommand; set { isEnableUSBCommand = value; base.RaisePropertyChanged(nameof(IsEnableUSBCommand)); } }



        private USBStatus currentUsbStatus = USBStatus.NotSet;

        public USBStatus CurrentUsbStatus
        {
            get => currentUsbStatus;
            set
            {
                currentUsbStatus = value;
                base.RaisePropertyChanged(nameof(CurrentUsbStatus));
            }
        }

        public string usbStatusDescription;
        public string USBStatusDescription { get => usbStatusDescription; set { usbStatusDescription = value; base.RaisePropertyChanged(nameof(USBStatusDescription)); } }


        private void ReadCurrentUsbStatus()
        {

            this.CurrentUsbStatus = ReadUSBStatus();
            USBStatusDescription = this.CurrentUsbStatus.ToString();

            IsDisableUSBCommandEnable = CurrentUsbStatus == USBStatus.Enable;
            IsEnableUSBCommand = CurrentUsbStatus == USBStatus.Disable;

            RefreshUSBListExecute();
        }


        public MainWindowViewModel()
        {

            // Logger
            Logger.Info("Start the app");

            this.USBDevices = GetUSBDevices();

            // Buttons
            DisableUSBCommand = new RelayCommand(DisableUSBStorageExecute);
            EnableUSBCommand = new RelayCommand(EnableUSBStorageExecute);
            RefreshUSBListCommand = new RelayCommand(RefreshUSBListExecute);

            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                IsAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
                UserName = principal.Identity.Name;
            }

            ReadCurrentUsbStatus();


            //ReadRegisterValue();

            //DriveInfo[] allDrives = DriveInfo.GetDrives();

            //foreach (DriveInfo d in allDrives)
            //{
            //    Console.WriteLine("Drive {0}", d.Name);
            //    Console.WriteLine("  Drive type: {0}", d.DriveType);
            //    if (d.IsReady == true)
            //    {
            //        Console.WriteLine("  Volume label: {0}", d.VolumeLabel);
            //        Console.WriteLine("  File system: {0}", d.DriveFormat);
            //        Console.WriteLine(
            //            "  Available space to current user:{0, 15} bytes",
            //            d.AvailableFreeSpace);

            //        Console.WriteLine(
            //            "  Total available space:          {0, 15} bytes",
            //            d.TotalFreeSpace);

            //        Console.WriteLine(
            //            "  Total size of drive:            {0, 15} bytes ",
            //            d.TotalSize);
            //    }
            //}


            //string[] keys = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Sofware").GetSubKeyNames();
            //foreach (string key in keys)
            //{
            //    if (Key == "MySoftware")
            //    {
            //        Console.Write("Found");
            //    }
            //}

            //var usbDriver = new List<USBDeviceInfo>();

            //var usbDevices = GetLogicalDevices();
            //foreach (var usbDevice in usbDevices)
            //{
            //    if (usbDevice.GetPropertyValue("DeviceID").ToString().Contains("USB") && usbDevice.GetPropertyValue("Status").ToString().Contains("OK"))
            //    {
            //        Guid mouseGuid = new Guid(usbDevice.GetPropertyValue("ClassGuid").ToString());
            //        string instancePath = usbDevice.GetPropertyValue("DeviceID").ToString();

            //        Console.WriteLine("mouseGuid={0}, instancePath={1}", mouseGuid, instancePath);

            //        //DeviceHelper.SetDeviceEnabled(mouseGuid, instancePath, true);
            //    }
            //}


            //foreach (var usbDevice in usbDriver)
            //{

            //}



        }


        public USBStatus ReadUSBStatus()
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
                return USBStatus.NotSet;

            }



        }

        private void ReadRegisterValue()
        {
            // HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\USBSTOR
            RegistryKey rk = Registry.LocalMachine;
            var t = rk.OpenSubKey("SYSTEM").OpenSubKey("CurrentControlSet").OpenSubKey("Services").OpenSubKey("USBSTOR");
            var t1 = t.GetValue("Start");
            // Print out the keys. 
            PrintKeys(t);
        }

        static void PrintKeys(RegistryKey rkey)
        {

            // Retrieve all the subkeys for the specified key. 
            String[] names = rkey.GetValueNames();


            int icount = 0;

            Console.WriteLine("Subkeys of " + rkey.Name);
            Console.WriteLine("-----------------------------------------------");

            // Print the contents of the array to the console. 
            foreach (String s in names)
            {
                Console.WriteLine(s);

                // The following code puts a limit on the number 
                // of keys displayed.  Comment it out to print the 
                // complete list. 
                icount++;
                if (icount >= 10)
                    break;
            }

        }

        static List<ManagementBaseObject> GetLogicalDevices()
        {
            List<ManagementBaseObject> devices = new List<ManagementBaseObject>();
            ManagementObjectCollection collection;
            using (var searcher = new ManagementObjectSearcher("root\\CIMV2",
                                  @"Select * From CIM_LogicalDevice"))
                collection = searcher.Get();
            foreach (var device in collection)
            {
                devices.Add(device);
            }
            collection.Dispose();
            return devices;
        }


        private void RefreshUSBListExecute()
        {
            this.USBDevices = GetUSBDevices();
        }

        private void DisableUSBStorageExecute()
        {
            if (!LockUnlockHelper.DisableUSBStorage())
            {
                UserMessage = "Fail to disable the usb! You must run the app as admin";
            }
            ReadCurrentUsbStatus();

        }

        private void EnableUSBStorageExecute()
        {
            if (!LockUnlockHelper.EnableUSBStorage())
            {
                UserMessage = "Fail to enable the usb! You must run the app as admin";
            }
            ReadCurrentUsbStatus();
        }

        static List<USBDeviceInfo> GetUSBDevices()
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

            foreach (ManagementObject queryObj in collection)
            {

                Console.WriteLine("-----------------------------------");
                Console.WriteLine("Win32_USBHub instance");
                Console.WriteLine("-----------------------------------");
                Console.WriteLine("Availability: {0}", queryObj["Availability"]);
                Console.WriteLine("Caption: {0}", queryObj["Caption"]);
                Console.WriteLine("CurrentConfigValue: {0}", queryObj["CurrentConfigValue"]);
                Console.WriteLine("NumberOfPorts: {0}", queryObj["NumberOfPorts"]);
                Console.WriteLine("NumberOfConfigs: {0}", queryObj["NumberOfConfigs"]);
                Console.WriteLine("CurrentConfigValue: {0}", queryObj["CurrentConfigValue"]);
                Console.WriteLine("Status: {0}", queryObj["Status"]);
                Console.WriteLine("StatusInfo: {0}", queryObj["StatusInfo"]);
                Console.WriteLine("USBVersion: {0}", queryObj["USBVersion"]);
                Console.WriteLine("PNPDeviceID: {0}", queryObj["PNPDeviceID"]);

            }

            collection.Dispose();
            return devices;
        }

    }
}
