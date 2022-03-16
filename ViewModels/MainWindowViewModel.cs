using CG.LockUnlock;
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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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


        // USB  -----------------------------------------


        private List<USBDeviceInfo> usbDevices = new List<USBDeviceInfo>();

        public List<USBDeviceInfo> USBDevices { get => usbDevices; set { usbDevices = value; base.RaisePropertyChanged(nameof(USBDevices)); } }


        private List<GenericDeviceInfo> allDevices = new List<GenericDeviceInfo>();

        public List<GenericDeviceInfo> AllDevices { get => allDevices; set { allDevices = value; base.RaisePropertyChanged(nameof(AllDevices)); } }

        public RelayCommand DisableUSBCommand { get; set; }
        public RelayCommand EnableUSBCommand { get; set; }
        public RelayCommand RefreshUSBListCommand { get; set; }

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


        // ALL DEVICE ---------------------------------

        public GenericDeviceInfo deviceSelected;
        public GenericDeviceInfo DeviceSelected
        {
            get => deviceSelected;
            set
            {
                if (value != null)
                {
                    deviceSelected = value;
                    DeviceSelectedDescription = value.DeviceID;
                }
                base.RaisePropertyChanged(nameof(DeviceSelected));
            }
        }


        public RelayCommand DisableDeviceCommand { get; set; }

        public RelayCommand EnableDeviceCommand { get; set; }
        public RelayCommand RefreshAllDeviceListCommand { get; set; }


        public bool IsDisableDeviceCommandEnable { get; set; } = true;
        public bool IsEnableDeviceCommandEnable { get; set; } = true;


        public string deviceSelectedDescription;
        public string DeviceSelectedDescription { get => deviceSelectedDescription; set { deviceSelectedDescription = value; base.RaisePropertyChanged(nameof(DeviceSelectedDescription)); } }




        // USER ---------------------------------


        private bool isAdmin;
        public bool IsAdmin { get => isAdmin; set { isAdmin = value; base.RaisePropertyChanged(nameof(IsAdmin)); } }

        public string userName;
        public string UserName { get => userName; set { userName = value; base.RaisePropertyChanged(nameof(UserName)); } }

        public string userMessage;
        public string UserMessage { get => userMessage; set { userMessage = value; base.RaisePropertyChanged(nameof(UserMessage)); } }











        public MainWindowViewModel()
        {

            // Logger
            Logger.Info("Start the app");

            this.USBDevices = GetUSBDevices();

            // BUTTONS ------------------------------

            // usb
            DisableUSBCommand = new RelayCommand(DisableUSBStorageExecute);
            EnableUSBCommand = new RelayCommand(EnableUSBStorageExecute);
            RefreshUSBListCommand = new RelayCommand(RefreshUSBListExecute);

            // all device
            DisableDeviceCommand = new RelayCommand(DisableDeviceCommandExecute);
            EnableDeviceCommand = new RelayCommand(EnableDeviceCommandExecute);
            RefreshAllDeviceListCommand = new RelayCommand(RefreshAllDeviceListExecute);

            // USER ------------------------------
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                IsAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
                UserName = principal.Identity.Name;
            }

            // READ DEVICES ------------------------------


            ReadCurrentUsbStatus();
            LoadPCAllDevices();

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






            //foreach (var usbDevice in usbDriver)
            //{

            //}



        }


        private void LoadPCAllDevices()
        {

            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = Cursors.Wait;
            });

            AllDevices.Clear();

            var allPcDevices = GetLogicalDevices();
            Console.WriteLine($"There are {allPcDevices.Count} devices.");

            var countDevice = 1;

            foreach (var usbDevice in allPcDevices)
            {
                if (usbDevice.GetPropertyValue("DeviceID").ToString().Contains("USB")
                    && usbDevice.GetPropertyValue("Status").ToString().Contains("OK"))
                {
                    try
                    {
                        Guid mouseGuid = new Guid(usbDevice.GetPropertyValue("ClassGuid").ToString());
                        string instancePath = usbDevice.GetPropertyValue("DeviceID").ToString();
                        string desciption = usbDevice.GetPropertyValue("DeviceID").ToString();
                        var description = (string)usbDevice.GetPropertyValue("Description");
                        var status = (string)usbDevice.GetPropertyValue("Status");

                        AllDevices.Add(new GenericDeviceInfo(countDevice, instancePath, mouseGuid, description, true));
                        //Console.WriteLine("mouseGuid={0}, instancePath={1}", mouseGuid, instancePath);
                        countDevice++;
                    }
                    catch (Exception ex)
                    {
                        Logger.Info(ex);
                    }
                    //DeviceHelper.SetDeviceEnabled(mouseGuid, instancePath, true);
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = null;
            });

        }

        private void ReadCurrentUsbStatus()
        {

            this.CurrentUsbStatus = ReadUSBStatus();
            USBStatusDescription = this.CurrentUsbStatus.ToString();

            IsDisableUSBCommandEnable = CurrentUsbStatus == USBStatus.Enable;
            IsEnableUSBCommand = CurrentUsbStatus == USBStatus.Disable;

            RefreshUSBListExecute();
        }

        void ShowHideDetails(object sender, RoutedEventArgs e)
        {
            for (var vis = sender as Visual; vis != null; vis = VisualTreeHelper.GetParent(vis) as Visual)
                if (vis is DataGridRow)
                {
                    var row = (DataGridRow)vis;
                    row.DetailsVisibility = row.DetailsVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                    break;
                }
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
            using (var searcher = new ManagementObjectSearcher("root\\CIMV2", @"Select * From CIM_LogicalDevice"))
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


        #region ALL DEVICES

        private void RefreshAllDeviceListExecute()
        {
            LoadPCAllDevices();
        }

        private void EnableDeviceCommandExecute()
        {
            try
            {
                DeviceHelper.SetDeviceEnabled(DeviceSelected.ClassGuid, deviceSelected.DeviceID, true);
            }
            catch (Exception ex)
            {
                UserMessage = $"Fail to enable the {deviceSelected.Description}!";
            }            
        }

        private void DisableDeviceCommandExecute()
        {
            try
            {
                DeviceHelper.SetDeviceEnabled(DeviceSelected.ClassGuid, deviceSelected.DeviceID, false);
            }
            catch (Exception ex) {
                UserMessage = $"Fail to disable the {deviceSelected.Description}!";
            }
            
        }


        #endregion


    }
}
