using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Management;
using System.Security.Principal;

// https://stackoverflow.com/questions/3331043/get-list-of-connected-usb-devices

// https://dev.to/iamthecarisma/enabling-and-disabling-usb-disk-drive-using-c-4dg5

// https://helpdeskgeek.com/how-to/how-to-setup-windows-10-as-a-kiosk/

namespace CG.LockUnlockTester
{
    public class MainWindowViewModel : ViewModelBase
    {

        /// <summary>
        /// Logger
        /// </summary>
        //private Logger log;


        private List<USBDeviceInfo> usbDevices = new List<USBDeviceInfo>();
        public List<USBDeviceInfo> USBDevices { get => usbDevices; set { usbDevices = value; base.RaisePropertyChanged(nameof(USBDevices)); } }


        public RelayCommand DisableUSBCommand { get; set; }

        public RelayCommand EnableUSBCommand { get; set; }
        public RelayCommand RefreshUSBListCommand { get; set; }

        private bool isAdmin;
        public bool IsAdmin { get => isAdmin; set { isAdmin = value; base.RaisePropertyChanged(nameof(IsAdmin)); } }

        public string userName;
        public string UserName { get => userName; set { userName = value; base.RaisePropertyChanged(nameof(UserName)); } }

        public MainWindowViewModel()
        {

            //// Logger
            //this.log = new Logger();
            //this.log.LoggerIdxTrace = this.log.NewLogger("tracelogger", true);
            //this.log.LoggerIdxError = this.log.NewLogger("errorlogger", true);
            //this.log.LoggerIdxDebug = this.log.NewLogger("debuglogger", true);
            //this.logFilePaht = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LoggerError.log");
            //if (!File.Exists(logFilePaht))
            //{
            //    MessageRunResult = "File LoggerError.log not found!";
            //}

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

        }

        private void RefreshUSBListExecute()
        {
            this.USBDevices = GetUSBDevices();
        }

        private void DisableUSBStorageExecute()
        {
            //disable USB storage...
            Microsoft.Win32.Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\USBSTOR", "Start", 4, Microsoft.Win32.RegistryValueKind.DWord);
        }

        private void EnableUSBStorageExecute()
        {
            //enable USB storage...
            Microsoft.Win32.Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\USBSTOR", "Start", 3, Microsoft.Win32.RegistryValueKind.DWord);
        }

        static List<USBDeviceInfo> GetUSBDevices()
        {
            List<USBDeviceInfo> devices = new List<USBDeviceInfo>();

            ManagementObjectCollection collection;

            using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_USBHub"))
                collection = searcher.Get();

            foreach (var device in collection)
            {
                devices.Add(new USBDeviceInfo(
                (string)device.GetPropertyValue("DeviceID"),
                (string)device.GetPropertyValue("PNPDeviceID"),
                (string)device.GetPropertyValue("Description")
                ));
            }

            collection.Dispose();
            return devices;
        }

    }
}
