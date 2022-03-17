using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using LockUnlock;
using NLog;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Input;


// https://stackoverflow.com/questions/3331043/get-list-of-connected-usb-devices

// https://dev.to/iamthecarisma/enabling-and-disabling-usb-disk-drive-using-c-4dg5

// https://helpdeskgeek.com/how-to/how-to-setup-windows-10-as-a-kiosk/

namespace CG.LockUnlockTester
{
    public class MainWindowViewModel : ViewModelBase
    {

        #region DISABLE USB 

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

        #endregion



        /// <summary>
        /// Logger
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();


        // USB by Registry -----------------------------------------

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


        // USB by API ---------------------------------

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

        public RelayCommand EnableAllDeviceCommand { get; set; }
        public RelayCommand DisableAllDeviceCommand { get; set; }

        private bool isDisableDeviceAllEnable;
        public bool IsDisableDeviceAllEnable { get => isDisableDeviceAllEnable; set { isDisableDeviceAllEnable = value; base.RaisePropertyChanged(nameof(IsDisableDeviceAllEnable)); } }

        private bool isEnableDeviceAllEnable = true;
        public bool IsEnableDeviceAllEnable { get => isEnableDeviceAllEnable; set { isEnableDeviceAllEnable = value; base.RaisePropertyChanged(nameof(IsEnableDeviceAllEnable)); } }



        // USER ---------------------------------

        private bool isAdmin;
        public bool IsAdmin { get => isAdmin; set { isAdmin = value; base.RaisePropertyChanged(nameof(IsAdmin)); } }

        public string userName;
        public string UserName { get => userName; set { userName = value; base.RaisePropertyChanged(nameof(UserName)); } }

        public string userMessage;
        public string UserMessage { get => userMessage; set { userMessage = value; base.RaisePropertyChanged(nameof(UserMessage)); } }



        // VIRTUAL KEYBOARD ---------------------------------

        public RelayCommand DisableVirtualKeyboardCommand { get; set; }

        public RelayCommand EnableVirtualKeyboardCommand { get; set; }

        private bool isVirtualKeyboardEnable;
        public bool IsVirtualKeyboardEnable { get => isVirtualKeyboardEnable; set { isVirtualKeyboardEnable = value; base.RaisePropertyChanged(nameof(IsVirtualKeyboardEnable)); } }

        private bool isVirtualKeyboardDisable = true;
        public bool IsVirtualKeyboardDisable { get => isVirtualKeyboardDisable; set { isVirtualKeyboardDisable = value; base.RaisePropertyChanged(nameof(IsVirtualKeyboardDisable)); } }


        // DISABLE SHORTCUT  ---------------------------------

        public RelayCommand DisableShortcutCommand { get; set; }

        public RelayCommand EnableShortcutCommand { get; set; }

        private bool isDisableShortcut;
        public bool IsDisableShortcut { get => isDisableShortcut; set { isDisableShortcut = value; base.RaisePropertyChanged(nameof(IsDisableShortcut)); } }

        private bool isEnableShortcut = true;
        public bool IsEnableShortcut { get => isEnableShortcut; set { isEnableShortcut = value; base.RaisePropertyChanged(nameof(IsEnableShortcut)); } }

        ManageShortcutCallback shortcutCallback;

        /// <summary>
        /// CST
        /// </summary>
        /// <param name="shortcutCallback"></param>
        public MainWindowViewModel(ManageShortcutCallback shortcutCallback)
        {

            // Logger
            Logger.Info("Start the app");

            this.shortcutCallback = shortcutCallback;

            this.USBDevices = LockUnlockHelper.GetUSBDevices();

            // BUTTONS ------------------------------

            // usb
            DisableUSBCommand = new RelayCommand(DisableUSBStorageByRegistryExecute);
            EnableUSBCommand = new RelayCommand(EnableUSBStorageByRegistryExecute);
            RefreshUSBListCommand = new RelayCommand(RefreshUSBListExecute);

            // all device
            DisableDeviceCommand = new RelayCommand(DisableDeviceCommandExecute);
            EnableDeviceCommand = new RelayCommand(EnableDeviceCommandExecute);
            RefreshAllDeviceListCommand = new RelayCommand(RefreshAllDeviceListExecute);
            EnableAllDeviceCommand  = new RelayCommand(EnableAllDeviceExecute);
            DisableAllDeviceCommand = new RelayCommand(DisableAllDeviceExecute);

            // Virtual keyboard
            DisableVirtualKeyboardCommand = new RelayCommand(DisableVirtualKeyboardExecute);
            EnableVirtualKeyboardCommand = new RelayCommand(EnableVirtualKeyboardExecute);

            // Short cut
            DisableShortcutCommand = new RelayCommand(DisableShortcutExecute);
            EnableShortcutCommand = new RelayCommand(EnableShortcutExecute); ;


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

        }



        private void LoadPCAllDevices()
        {

            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = Cursors.Wait;
            });

            AllDevices.Clear();

            var allPcDevices = LockUnlockHelper.GetLogicalDevices();
            Console.WriteLine($"There are {allPcDevices.Count} devices.");

            var countDevice = 1;

            foreach (var usbDevice in allPcDevices)
            {
                if (usbDevice.GetPropertyValue("DeviceID").ToString().Contains("USB")
                    && usbDevice.GetPropertyValue("Status").ToString().Contains("OK"))
                {

                    // https://powershell.one/wmi/root/cimv2/cim_logicaldevice

                    try
                    {
                        Guid mouseGuid = new Guid(usbDevice.GetPropertyValue("ClassGuid").ToString());
                        string instancePath = usbDevice.GetPropertyValue("DeviceID").ToString();

                        var description = (string)usbDevice.GetPropertyValue("Description");

                        var name = usbDevice.GetPropertyValue("Name");
                        if (name != null)
                        {
                            var t = name.ToString();
                            if (t.ToLower().Trim() != description.ToLower().Trim())
                            {
                                description = description + " [" + t + "]";
                            }
                        }
                        var stringStatusInfo = "not-set";
                        var statusInfo = usbDevice.GetPropertyValue("StatusInfo");
                        if (statusInfo != null)
                        {
                            stringStatusInfo = statusInfo.ToString();
                        }
                        AllDevices.Add(new GenericDeviceInfo(countDevice, instancePath, mouseGuid, description, true, stringStatusInfo));
                        countDevice++;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = null;
            });

        }

        private void ReadCurrentUsbStatus()
        {
            this.CurrentUsbStatus = LockUnlockHelper.ReadUSBStatusInRegistry();
            USBStatusDescription = this.CurrentUsbStatus.ToString();
            IsDisableUSBCommandEnable = CurrentUsbStatus == USBStatus.Enable;
            IsEnableUSBCommand = CurrentUsbStatus == USBStatus.Disable;
            RefreshUSBListExecute();
        }

        private void RefreshUSBListExecute()
        {
            this.USBDevices = LockUnlockHelper.GetUSBDevices();
        }

        private void DisableUSBStorageByRegistryExecute()
        {
            if (!LockUnlockHelper.SetUSBbyRegistry(USBStatus.Disable))
            {
                UserMessage = "Fail to disable the usb! You must run the app as admin";
            }
            ReadCurrentUsbStatus();
        }

        private void EnableUSBStorageByRegistryExecute()
        {
            if (!LockUnlockHelper.SetUSBbyRegistry(USBStatus.Enable))
            {
                UserMessage = "Fail to enable the usb! You must run the app as admin";
            }
            ReadCurrentUsbStatus();
        }




        #region USB API

        private void RefreshAllDeviceListExecute()
        {
            LoadPCAllDevices();
        }

        private void EnableDeviceCommandExecute()
        {
            try
            {
                LockUnlockHelper.SetDeviceEnabled(DeviceSelected.ClassGuid, deviceSelected.DeviceID, true);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                UserMessage = $"Fail to enable the {deviceSelected.Description}! Check if the LockUnlock library is compile in 64bit";
            }
        }

        private void DisableDeviceCommandExecute()
        {
            try
            {
                LockUnlockHelper.SetDeviceEnabled(DeviceSelected.ClassGuid, deviceSelected.DeviceID, false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                UserMessage = $"Fail to disable the {deviceSelected.Description}!. Check if the LockUnlock library is compile in 64bit";
            }

        }

        private void DisableAllDeviceExecute()
        {
            try
            {
                LockUnlockHelper.SetUSBbyAPI(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                UserMessage = $"Fail to disable the {deviceSelected.Description}!. Check if the LockUnlock library is compile in 64bit";
            }

            IsDisableDeviceAllEnable = false;
            IsEnableDeviceAllEnable = true;
        }

        private void EnableAllDeviceExecute()
        {
            try
            {
                LockUnlockHelper.SetUSBbyAPI(true);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                UserMessage = $"Fail to disable the {deviceSelected.Description}!. Check if the LockUnlock library is compile in 64bit";
            }

            IsDisableDeviceAllEnable = true;
            IsEnableDeviceAllEnable = false;
        }

        #endregion


        #region Virtual keyboard

        // https://answers.microsoft.com/en-us/windows/forum/all/cannot-disablestop-touch-keyboard-and-handwriting/091c4403-098b-49ac-a18c-6af3d787b72a

        private void EnableVirtualKeyboardExecute()
        {
            try
            {
                LockUnlockHelper.EnableVirtualKeyboard();
            }
            catch (Exception ex)
            {
                UserMessage = "Fail to EnableVirtualKeyboardExecute";
                Logger.Error(ex);
            }
            IsVirtualKeyboardDisable = true;
            IsVirtualKeyboardEnable = false;
        }

        private void DisableVirtualKeyboardExecute()
        {

            try
            {
                LockUnlockHelper.DisableVirtualKeyboard();
            }
            catch (Exception ex)
            {
                UserMessage = "Fail to DisableVirtualKeyboard";
                Logger.Error(ex);
            }

            IsVirtualKeyboardDisable = false;
            IsVirtualKeyboardEnable = true;

        }

        #endregion


        #region Disable Shortcut

        private void EnableShortcutExecute()
        {
            shortcutCallback.Invoke(true);
            IsEnableShortcut = false;
            IsDisableShortcut = true;
        }

        private void DisableShortcutExecute()
        {
            shortcutCallback.Invoke(false);
            IsEnableShortcut = true;
            IsDisableShortcut = false;
        }


        #endregion


    }
}
