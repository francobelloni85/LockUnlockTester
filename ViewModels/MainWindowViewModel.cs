using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using LockUnlock;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;



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
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #region USB DISABLE

        // USB by Registry -----------------------------------------

        private List<USBDeviceInfo> usbDevices = new List<USBDeviceInfo>();

        /// <summary>
        /// Lista di tutti i device trovati con il metodo Registry
        /// </summary>
        public List<USBDeviceInfo> USBDevices { get => usbDevices; set { usbDevices = value; base.RaisePropertyChanged(nameof(USBDevices)); } }


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

        private List<GenericDeviceInfo> allDevices = new List<GenericDeviceInfo>();

        /// <summary>
        /// LIsta di tutti i device trovati con il metodo API
        /// </summary>
        public List<GenericDeviceInfo> AllDevices { get => allDevices; set { allDevices = value; base.RaisePropertyChanged(nameof(AllDevices)); } }


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



        // DEVCOM

        /// <summary>
        /// Lista di tutti i device letti con il metodo 3 DevCon
        /// </summary>
        public ObservableCollection<DevconUSB> USBDevconList { get; set; } = new ObservableCollection<DevconUSB>();


        private DevconUSB usbDevconSelected;

        /// <summary>
        /// L'usb che è al momento selezionata
        /// </summary>
        public DevconUSB USBDevconSelected
        {
            get => usbDevconSelected;
            set
            {
                usbDevconSelected = value;
                base.RaisePropertyChanged(nameof(USBDevconSelected));
            }
        }

        /// <summary>
        /// Deprecated
        /// </summary>  
        public RelayCommand DisableDEVCOMCommand { get; set; }

        /// <summary>
        /// Deprecated
        /// </summary>  
        public RelayCommand EnableDEVCOMCommand { get; set; }

        /// <summary>
        /// Aggiorna la lista
        /// </summary>
        public RelayCommand RefreshDEVCOM { get; set; }

        private bool isEnableDEVCOMCommand;

        /// <summary>
        /// abilita tutto - deprecato
        /// </summary>
        public bool IsEnableDEVCOMCommand { get => isEnableDEVCOMCommand; set { isEnableDEVCOMCommand = value; base.RaisePropertyChanged(nameof(IsEnableDEVCOMCommand)); } }

        private bool isDisableDEVCOMCommandEnable = true;

        /// <summary>
        /// deprecato - disabilita tutto 
        /// </summary>
        public bool IsDisableDEVCOMCommandEnable { get => isDisableDEVCOMCommandEnable; set { isDisableDEVCOMCommandEnable = value; base.RaisePropertyChanged(nameof(IsDisableDEVCOMCommandEnable)); } }

        private string isDEVCOMFound;
        /// <summary>
        /// Mi indica se ho trovato il file devcon.exe
        /// </summary>
        public string IsDEVCOMFound { get => isDEVCOMFound; set { isDEVCOMFound = value; base.RaisePropertyChanged(nameof(IsDEVCOMFound)); } }

        public string devcomOutput;
        /// <summary>
        /// Output del comando devcom.exe
        /// </summary>  
        public string DEVCOMOutput { get => devcomOutput; set { devcomOutput = value; base.RaisePropertyChanged(nameof(DEVCOMOutput)); } }


        // Single disable

        public bool IsDisableDeviceDevconEnable { get; set; } = true;
        public bool IsEnableDeviceDevconEnable { get; set; } = true;

        /// <summary>
        /// Abilita l'usb selezionata
        /// </summary> 
        public RelayCommand DisableDeviceDevconCommand { get; set; }

        /// <summary>
        /// Disabilita l'usb selezionata
        /// </summary>  
        public RelayCommand EnableDeviceDevconCommand { get; set; }

        #endregion

        // USER ---------------------------------

        #region USER



        private bool isAdmin;
        /// <summary>
        /// Mi indica se l'utente è admin
        /// </summary>
        public bool IsAdmin { get => isAdmin; set { isAdmin = value; base.RaisePropertyChanged(nameof(IsAdmin)); } }

        public string userName;

        /// <summary>
        /// IL nome dell'utente che sta usando windows
        /// </summary>
        public string UserName { get => userName; set { userName = value; base.RaisePropertyChanged(nameof(UserName)); } }

        public string messageForTheUser;

        /// <summary>
        /// Messaggio da far comparere all'utente
        /// </summary>
        public string MessageForTheUser { get => messageForTheUser; set { messageForTheUser = value; base.RaisePropertyChanged(nameof(MessageForTheUser)); } }

        #endregion


        // KEYBOARD LOGGER ---------------------------------

        #region KEYBOARD LOGGER

        /// <summary>
        /// Comando per attivare il keylogger
        /// </summary>
        public RelayCommand CreateKeyLoggerCommand { get; set; }

        /// <summary>
        /// Comando per disabilitare il keylogger
        /// </summary>
        public RelayCommand DisposeKeyLoggerCommand { get; set; }

        private bool isCreateKeyLoggerEnable = true;
        public bool IsCreateKeyLoggerEnable { get => isCreateKeyLoggerEnable; set { isCreateKeyLoggerEnable = value; base.RaisePropertyChanged(nameof(IsCreateKeyLoggerEnable)); } }

        private bool isKeyLoggerAlive;
        public bool IsKeyLoggerAlive { get => isKeyLoggerAlive; set { isKeyLoggerAlive = value; base.RaisePropertyChanged(nameof(IsKeyLoggerAlive)); } }

        /// <summary>
        /// Classe che per gestire il key logger
        /// </summary>
        KeyboardListener keyboardListener;

        /// <summary>
        /// Questa call back è solo per far vedere che il keylogger è attivo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void KeyboardListener_KeyDown(object sender, RawKeyEventArgs args)
        {
            Console.WriteLine(args.Key.ToString());
            Console.WriteLine(args.ToString()); // Prints the text of pressed button, takes in account big and small letters. E.g. "Shift+a" => "A"
        }

        #endregion

        // VIRTUAL KEYBOARD ---------------------------------

        #region VIRTUAL KEYBOARD

        /// <summary>
        /// Abilita la tastiare virtuale
        /// </summary>
        public RelayCommand DisableVirtualKeyboardCommand { get; set; }

        /// <summary>
        /// Disabilita la tastiera virtuale
        /// </summary>
        public RelayCommand EnableVirtualKeyboardCommand { get; set; }


        private bool isVirtualKeyboardEnable = true;
        /// <summary>
        /// Mi dice se il punsate deve essere enable per la attivare il comando che abilita la tastiera
        /// </summary>

        public bool IsVirtualKeyboardEnable { get => isVirtualKeyboardEnable; set { isVirtualKeyboardEnable = value; base.RaisePropertyChanged(nameof(IsVirtualKeyboardEnable)); } }

        private bool isVirtualKeyboardDisable;

        /// <summary>
        /// Mi dice se il punsate deve essere enable per la attivare il comando che disabilita la tastiera
        /// </summary>
        public bool IsVirtualKeyboardDisable { get => isVirtualKeyboardDisable; set { isVirtualKeyboardDisable = value; base.RaisePropertyChanged(nameof(IsVirtualKeyboardDisable)); } }

        #endregion

        // KEYBOARD ---------------------------------

        #region KEYBOARD

        /// <summary>
        /// Abilita la tastiare 
        /// </summary>
        public RelayCommand DisableKeyboardCommand { get; set; }

        /// <summary>
        /// Disabilita la tastiera 
        /// </summary>
        public RelayCommand EnableKeyboardCommand { get; set; }


        private bool isKeyboardEnable;
        /// <summary>
        /// Mi dice se il punsate deve essere enable per la attivare il comando che abilita la tastiera
        /// </summary>

        public bool IsKeyboardEnable { get => isKeyboardEnable; set { isKeyboardEnable = value; base.RaisePropertyChanged(nameof(IsKeyboardEnable)); } }

        private bool isKeyboardDisable;

        /// <summary>
        /// Mi dice se il punsate deve essere enable per la attivare il comando che disabilita la tastiera
        /// </summary>
        public bool IsKeyboardDisable { get => isKeyboardDisable; set { isKeyboardDisable = value; base.RaisePropertyChanged(nameof(IsKeyboardDisable)); } }

        #endregion

        // TASK VIEW ---------------------------------

        #region TASK VIEW

        private string taskViewStatus;
        public string TaskViewStatus { get => taskViewStatus; set { taskViewStatus = value; base.RaisePropertyChanged(nameof(TaskViewStatus)); } }

        /// <summary>
        /// Abilita la task view
        /// </summary>
        public RelayCommand DisableTaskViewCommand { get; set; }

        /// <summary>
        /// Disabilita task view
        /// </summary>
        public RelayCommand EnableTaskViewCommand { get; set; }


        private bool isDisableTaskView = true;
        /// <summary>
        /// Mi dice se il punsate deve essere enable per la attivare il comando che abilita la task view
        /// </summary>

        public bool IsDisableTaskView { get => isDisableTaskView; set { isDisableTaskView = value; base.RaisePropertyChanged(nameof(IsDisableTaskView)); } }

        private bool isEnableTaskView;

        /// <summary>
        /// Mi dice se il punsate deve essere enable per la attivare il comando che disabilita la task view
        /// </summary>
        public bool IsEnableTaskView { get => isEnableTaskView; set { isEnableTaskView = value; base.RaisePropertyChanged(nameof(IsEnableTaskView)); } }



        #endregion


        // DISABLE SHORTCUT  ---------------------------------

        #region SHORTCUT

        /// <summary>
        /// Deprecated
        /// </summary>       
        public RelayCommand DisableShortcutCommand { get; set; }

        /// <summary>
        /// Deprecated
        /// </summary>
        public RelayCommand EnableShortcutCommand { get; set; }

        private bool isDisableShortcut;
        /// <summary>
        /// Deprecated
        /// </summary>
        public bool IsDisableShortcut { get => isDisableShortcut; set { isDisableShortcut = value; base.RaisePropertyChanged(nameof(IsDisableShortcut)); } }

        private bool isEnableShortcut;
        /// <summary>
        /// Deprecated
        /// </summary>
        public bool IsEnableShortcut { get => isEnableShortcut; set { isEnableShortcut = value; base.RaisePropertyChanged(nameof(IsEnableShortcut)); } }

        #endregion

        // DISABLE TASK MANAGER  ---------------------------------

        #region TASK MANAGER

        /// <summary>
        /// Cpmando per disabilitare il task manager
        /// </summary>
        public RelayCommand DisableTaskManagerCommand { get; set; }

        /// <summary>
        /// Comando per abilitare il task manager
        /// </summary>
        public RelayCommand EnableTaskManagerCommand { get; set; }

        private bool isTaskManagerDisable = true;

        /// <summary>
        /// Mi indica lo stato del pulsate
        /// </summary>
        public bool IsTaskManagerDisable { get => isTaskManagerDisable; set { isTaskManagerDisable = value; base.RaisePropertyChanged(nameof(IsTaskManagerDisable)); } }

        private bool isTaskManagerEnable;

        /// <summary>
        /// Mi indica lo stato del pulsate
        /// </summary>
        public bool IsTaskManagerEnable { get => isTaskManagerEnable; set { isTaskManagerEnable = value; base.RaisePropertyChanged(nameof(IsTaskManagerEnable)); } }

        #endregion

        // DISABLE TASK BAR  ---------------------------------

        #region TASK BAR

        /// <summary>
        /// Comando per abilitare
        /// </summary>
        public RelayCommand DisableTaskBarCommand { get; set; }

        /// <summary>
        /// Comando per disabilitare
        /// </summary>
        public RelayCommand EnableTaskBarCommand { get; set; }

        private bool isTaskBarDisable = true;

        /// <summary>
        /// Mi indica lo stato del pulsate
        /// </summary>
        public bool IsTaskBarDisable { get => isTaskBarDisable; set { isTaskBarDisable = value; base.RaisePropertyChanged(nameof(IsTaskBarDisable)); } }

        private bool isTaskbarEnable;

        /// <summary>
        /// Mi indica lo stato del pulsate
        /// </summary>
        public bool IsTaskbarEnable { get => isTaskbarEnable; set { isTaskbarEnable = value; base.RaisePropertyChanged(nameof(IsTaskbarEnable)); } }

        #endregion

        // DISABLE MOUSE LIMIT AREA ---------------------------------

        #region MOUSE LIMIT AREA

        private DispatcherTimer dispatcherTimerMousePosition;

        private int MouseYLimit = -1;

        /// <summary>
        /// Comando per abilitare
        /// </summary>
        public RelayCommand DisableLimitAreaMouseCommand { get; set; }

        /// <summary>
        /// Comando per disabilitare
        /// </summary>
        public RelayCommand EnableLimitAreaMouseCommand { get; set; }

        private bool isLimitAreaMouseDisable;

        /// <summary>
        /// Mi indica lo stato del pulsate
        /// </summary>
        public bool IsLimitAreaMouseDisable { get => isLimitAreaMouseDisable; set { isLimitAreaMouseDisable = value; base.RaisePropertyChanged(nameof(IsLimitAreaMouseDisable)); } }


        private bool isLimitAreaMouseEnable = true;

        /// <summary>
        /// Mi indica lo stato del pulsate
        /// </summary>
        public bool IsLimitAreaMouseEnable { get => isLimitAreaMouseEnable; set { isLimitAreaMouseEnable = value; base.RaisePropertyChanged(nameof(IsLimitAreaMouseEnable)); } }

        public static System.Windows.Point GetMousePositionWindowsForms()
        {
            var point = Control.MousePosition;
            return new System.Windows.Point(point.X, point.Y);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out Point pPoint);

        /// <summary>
        /// Mi rappresenta le coordinate
        /// </summary>
        public struct Point
        {
            public int X;
            public int Y;

            public Point(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        #endregion

        // COSTRUTTORE ------------------------------

        /// <summary>
        /// CST
        /// </summary>
        /// <param name="shortcutCallback"></param>
        public MainWindowViewModel()
        {
            try
            {
                

                // Logger
                Logger.Info("Start the app");

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
                EnableAllDeviceCommand = new RelayCommand(EnableAllDeviceExecute);
                DisableAllDeviceCommand = new RelayCommand(DisableAllDeviceExecute);

                // DEVCOM
                DisableDEVCOMCommand = new RelayCommand(DisableDEVCOMCommandExecute);
                EnableDEVCOMCommand = new RelayCommand(EnableDEVCOMCommandExecute);
                RefreshDEVCOM = new RelayCommand(RefreshDEVCOMExecute);
                DisableDeviceDevconCommand = new RelayCommand(DisableDeviceDevconExecute);
                EnableDeviceDevconCommand = new RelayCommand(EnableDeviceDevconExecute);

                // Key logger
                CreateKeyLoggerCommand = new RelayCommand(CreateKeyLoggerExecute);
                DisposeKeyLoggerCommand = new RelayCommand(DisposeKeyLoggerExecute);

                // Disable keyboard
                DisableKeyboardCommand = new RelayCommand(DisableKeyboardExecute);
                EnableKeyboardCommand = new RelayCommand(EnableKeyboardExecute);

                // Vitual keyboard
                DisableVirtualKeyboardCommand = new RelayCommand(DisableVirtualKeyboardExecute);
                EnableVirtualKeyboardCommand = new RelayCommand(EnableVirtualKeyboardExecute);

                // Short cut
                DisableShortcutCommand = new RelayCommand(DisableShortcutExecute);
                EnableShortcutCommand = new RelayCommand(EnableShortcutExecute);

                // TaskManager
                DisableTaskManagerCommand = new RelayCommand(DisableTaskManagerExecute);
                EnableTaskManagerCommand = new RelayCommand(EnableTaskManagerExecute);

                // TaskBar
                DisableTaskBarCommand = new RelayCommand(DisableTaskBarExecute);
                EnableTaskBarCommand = new RelayCommand(EnableTaskBarExecute);

                // Mouse limit area
                DisableLimitAreaMouseCommand = new RelayCommand(DisableLimitAreaMouseExecute);
                EnableLimitAreaMouseCommand = new RelayCommand(EnableLimitAreaMouseExecute);

                // Task view
                DisableTaskViewCommand = new RelayCommand(DisableTaskViewExecute);
                EnableTaskViewCommand = new RelayCommand(EnableTaskViewExecute);
                TaskViewStatus = LockUnlockHelper.ReadTaskView().ToString();

                // USER ------------------------------
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    IsAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
                    UserName = principal.Identity.Name;
                }

                // READ USB DEVICES ------------------------------

                // Mode 1
                ReadCurrentUsbStatus();

                // Mode 2
                LoadPCAllDevices();

                // Mode 3
                try
                {
                    if (File.Exists("devcon.exe"))
                    {
                        ReadUSBListByDevCon();
                    }
                    else
                    {
                        MessageForTheUser = "devcon.exe not found!";
                        IsEnableDeviceDevconEnable = false;
                        IsDisableDeviceDevconEnable = false;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Info(ex);
                }

                // Mouse events
                MouseYLimit = Screen.PrimaryScreen.WorkingArea.Height;

                this.dispatcherTimerMousePosition = new DispatcherTimer();
                this.dispatcherTimerMousePosition.Tick += new EventHandler(TimerTickMousePosition);
                this.dispatcherTimerMousePosition.Interval = new TimeSpan(0, 0, 0, 0, 50);
                                
            }
            catch (Exception ex)
            {
                Logger.Info(ex);
            }

        }

        #region USB Registry

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
                MessageForTheUser = "Fail to disable the usb! You must run the app as admin";
            }
            ReadCurrentUsbStatus();
        }

        private void EnableUSBStorageByRegistryExecute()
        {
            if (!LockUnlockHelper.SetUSBbyRegistry(USBStatus.Enable))
            {
                MessageForTheUser = "Fail to enable the usb! You must run the app as admin";
            }
            ReadCurrentUsbStatus();
        }

        #endregion

        #region USB API

        private void LoadPCAllDevices()
        {

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            });

            AllDevices.Clear();

            var allPcDevices = LockUnlockHelper.GetLogicalDevices();
            Console.WriteLine($"There are {allPcDevices.Count} devices.");

            var countDevice = 1;
            int countStep = 1;

            foreach (ManagementBaseObject usbDevice in allPcDevices)
            {
                countStep = 1;

                string instancePath = GetProperty(usbDevice, "DeviceID");
                countStep++;

                string status = GetProperty(usbDevice, "Status");
                countStep++;

                if (instancePath.Contains("USB") && status.Contains("OK"))
                {
                    // https://powershell.one/wmi/root/cimv2/cim_logicaldevice

                    try
                    {

                        countStep++;
                        Guid mouseGuid = new Guid();
                        countStep++;
                        var mouseGuidValue = GetProperty(usbDevice, "ClassGuid");
                        countStep++;
                        if (mouseGuidValue != "NA")
                        {
                            countStep++;
                            mouseGuid = new Guid(mouseGuidValue);
                        }
                        countStep++;
                        string description = GetProperty(usbDevice, "Description");
                        countStep++;
                        string stringStatusInfo = GetProperty(usbDevice, "StatusInfo");
                        countStep++;
                        string name = GetProperty(usbDevice, "Name");
                        countStep++;
                        if (name != "NA")
                        {
                            countStep++;
                            var t = name.ToString();
                            countStep++;
                            if (t.ToLower().Trim() != description.ToLower().Trim())
                            {
                                countStep++;
                                description = description + " [" + t + "]";
                            }
                        }
                        countStep++;

                        AllDevices.Add(new GenericDeviceInfo(countDevice, instancePath, mouseGuid, description, true, stringStatusInfo));
                        countDevice++;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("countStep=" + countStep);
                        Logger.Error(ex);
                    }
                }
            }

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = null;
            });

        }

        private static string GetProperty(ManagementBaseObject obj, string property)
        {
            try
            {
                var value = (obj.GetPropertyValue(property) == null)
                            ? "NA"
                            : obj.GetPropertyValue(property).ToString();

                return value;
            }
            catch (Exception ex)
            {
                //Logger.Error("property=" + property);
                //Logger.Error(ex);
            }
            return "NA";

        }

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
                MessageForTheUser = $"Fail to enable the {deviceSelected.Description}! Check if the LockUnlock library is compile in 64bit";
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
                MessageForTheUser = $"Fail to disable the {deviceSelected.Description}!. Check if the LockUnlock library is compile in 64bit";
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
                MessageForTheUser = $"Fail to disable the {deviceSelected.Description}!. Check if the LockUnlock library is compile in 64bit";
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
                MessageForTheUser = $"Fail to disable the {deviceSelected.Description}!. Check if the LockUnlock library is compile in 64bit";
            }

            IsDisableDeviceAllEnable = true;
            IsEnableDeviceAllEnable = false;
        }

        #endregion

        #region DEVCOM
        

        private void ReadUSBListByDevCon()
        {
            var temp = LockUnlockHelper.GetDevConUSBList();
            List<DevConaHardwareDevice> allHardware = LockUnlockHelper.GetDevConaAllHardware();

            ObservableCollection<DevconUSB> tempList = new ObservableCollection<DevconUSB>();
            foreach (var item in temp)
            {
                tempList.Add(item);
            }

            if (tempList.Count == 0)
            {
                DEVCOMOutput = "devcon.exe not working!";
                IsDEVCOMFound = "not found";
            }

            var OrderUSBDevconList = tempList.OrderBy(t => t.Name);

            USBDevconList.Clear();
            foreach (var item in OrderUSBDevconList)
            {
                item.DisplayName = LockUnlockHelper.GetDeviceNameByID(allHardware, item.HardwareID, item.Name);
                USBDevconList.Add(item);
            }

        }

        private void EnableDeviceDevconExecute()
        {
            ManageDeviceByDevcon(true);
        }

        // https://docs.microsoft.com/en-us/windows-hardware/drivers/devtest/devcon-examples#example-31-disable-devices-by-device-instance-id
        private void DisableDeviceDevconExecute()
        {
            ManageDeviceByDevcon(false);
        }

        public void ManageDeviceByDevcon(bool enable)
        {
            //DisableKeyboard(enable);
            LockUnlockHelper.EnableDeviceDevCon(USBDevconSelected.HardwareID, enable);
            RefreshDEVCOMExecute();
        }

        //private void DisableKeyboard(bool disable) {

        //    var deviceID = "HID_DEVICE_SYSTEM_KEYBOARD";
        //    LockUnlockHelper.EnableDeviceDevCon(deviceID, disable);

        //}


        private void RefreshDEVCOMExecute()
        {
            ReadUSBListByDevCon();
        }

        private void EnableDEVCOMCommandExecute()
        {
            //if (isDEVCOMAvalable)
            //{
            //    ExecuteCommand("devcon enable USB* > listusb.txt");
            //    ReadDEVCOMCommandResult();
            //}
            //else
            //{
            //    DEVCOMOutput = "devcon.exe not working!";
            //    IsDEVCOMFound = "not found";
            //}
        }

        private void DisableDEVCOMCommandExecute()
        {

            //if (isDEVCOMAvalable)
            //{
            //    ExecuteCommand("devcon disable USB* > listusb.txt");
            //    ReadDEVCOMCommandResult();
            //}
            //else
            //{
            //    DEVCOMOutput = "devcon.exe not working!";
            //    IsDEVCOMFound = "not found";
            //}
        }



        #endregion

        #region Virtual keyboard

        // https://answers.microsoft.com/en-us/windows/forum/all/cannot-disablestop-touch-keyboard-and-handwriting/091c4403-098b-49ac-a18c-6af3d787b72a

        private void EnableVirtualKeyboardExecute()
        {
            try
            {
                LockUnlockHelper.EnableKeyboardOnScreen(true);
            }
            catch (Exception ex)
            {
                MessageForTheUser = "Fail to Enable Virtual Keyboard";
                Logger.Error(ex);
            }

            IsVirtualKeyboardDisable = true;
            IsVirtualKeyboardEnable = false;
        }
        
        private void DisableVirtualKeyboardExecute()
        {

            try
            {
                LockUnlockHelper.EnableKeyboardOnScreen(false);
            }
            catch (Exception ex)
            {
                MessageForTheUser = "Fail to Disable Virtual Keyboard";
                Logger.Error(ex);
            }

            IsVirtualKeyboardDisable = false;
            IsVirtualKeyboardEnable = true;

        }

        #endregion
        
        #region Key logger

        private void CreateKeyLoggerExecute()
        {
            keyboardListener = new KeyboardListener();

            IsKeyboardDisable = false;
            IsKeyboardEnable = true;

            IsEnableShortcut = true;
            IsDisableShortcut = false;

            IsKeyLoggerAlive = true;
        }

        private void DisposeKeyLoggerExecute()
        {
            keyboardListener.KeyDown -= KeyboardListener_KeyDown;
            keyboardListener.Dispose();

            IsCreateKeyLoggerEnable = true;
            IsKeyLoggerAlive = false;

            IsKeyboardDisable = false;
            IsKeyboardEnable = false;

            IsEnableShortcut = false;
            IsDisableShortcut = false;
        }

        #endregion

        #region Disable Keyboard

        private void EnableKeyboardExecute()
        {
            keyboardListener.DisableKeyboar(true);
            IsKeyboardDisable = true;
            IsKeyboardEnable = false;
        }

        private void DisableKeyboardExecute()
        {
            keyboardListener.DisableKeyboar(false);
            IsKeyboardDisable = false;
            IsKeyboardEnable = true;
        }

        #endregion

        #region Disable Shortcut

        /// <summary>
        /// 
        /// </summary>
        private void EnableShortcutExecute()
        {
            ManageShortcut(true);
            IsEnableShortcut = false;
            IsDisableShortcut = true;
        }

        /// <summary>
        /// 
        /// </summary>
        private void DisableShortcutExecute()
        {
            ManageShortcut(false);
            IsEnableShortcut = true;
            IsDisableShortcut = false;
        }

        /// <summary>
        /// 
        /// </summary>
        public void ManageShortcut(bool enable)
        {
            if (enable)
            {
                keyboardListener.EnableShortcut();
            }
            else
            {
                keyboardListener.DisableShortcut();
            }
        }


        #endregion

        #region TaskManager

        private void EnableTaskManagerExecute()
        {
            try
            {
                LockUnlockHelper.SetTaskManagerByCmd(true);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                MessageForTheUser = $"Fail to enable the TaskManager";
            }
            IsTaskManagerDisable = true;
            IsTaskManagerEnable = false;
        }

        private void DisableTaskManagerExecute()
        {
            try
            {
                LockUnlockHelper.SetTaskManagerByCmd(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                MessageForTheUser = $"Fail to disable the TaskManager";
            }

            IsTaskManagerDisable = false;
            IsTaskManagerEnable = true;
        }


        #endregion

        #region TaskBar

        private void EnableTaskBarExecute()
        {
            try
            {
                LockUnlockHelper.ShowTaskbar(true);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                MessageForTheUser = $"Fail to disable the Taskbar";
            }
            IsTaskBarDisable = true;
            IsTaskbarEnable = false;
        }

        private void DisableTaskBarExecute()
        {
            try
            {
                LockUnlockHelper.ShowTaskbar(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                MessageForTheUser = $"Fail to disable the Taskbar";
            }
            IsTaskBarDisable = false;
            IsTaskbarEnable = true;
        }

        #endregion

        #region TaskView

        private void EnableTaskViewExecute()
        {
            try
            {
                LockUnlockHelper.SetTaskView(true);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                MessageForTheUser = $"Fail to disable the Task view";
            }
            IsEnableTaskView = false;
            IsDisableTaskView = true;
            TaskViewStatus = LockUnlockHelper.ReadTaskView().ToString();
        }

        private void DisableTaskViewExecute()
        {
            try
            {
                LockUnlockHelper.SetTaskView(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                MessageForTheUser = $"Fail to disable the Task view";
            }
            IsEnableTaskView = true;
            IsDisableTaskView = false;
            TaskViewStatus = LockUnlockHelper.ReadTaskView().ToString();
        }

        #endregion

        #region LimitAreaMouse

        /* 
         * Un timer controlla che il mouse non sia oltre il limite (troppo vicino alla barra di windows)
         * Se lo è riporta il mouse in piu in alto. 
         */
                 
        private void EnableLimitAreaMouseExecute()
        {
            dispatcherTimerMousePosition.Start();
            IsLimitAreaMouseEnable = false;
            IsLimitAreaMouseDisable = true;
        }

        private void DisableLimitAreaMouseExecute()
        {
            dispatcherTimerMousePosition.Stop();
            IsLimitAreaMouseEnable = true;
            IsLimitAreaMouseDisable = false;
        }

        private void TimerTickMousePosition(object sender, EventArgs e)
        {
            if (!IsLimitAreaMouseEnable)
            {
                GetCursorPos(out Point pnt);
                Console.WriteLine("Mouse {0}.{1}", pnt.X, pnt.Y);
                if (pnt.Y > MouseYLimit)
                {
                    SetPosition(pnt.X, MouseYLimit - 2);
                }
            }

        }

        private void SetPosition(int a, int b)
        {
            SetCursorPos(a, b);
        }

        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        #endregion
        

        //private void MouseHook()
        //{

        //    using (var eventHookFactory = new EventHookFactory())
        //    {
        //        keyboardWatcher = eventHookFactory.GetKeyboardWatcher();
        //        keyboardWatcher.Start();
        //        keyboardWatcher.OnKeyInput += (s, e) =>
        //        {
        //            Console.WriteLine(string.Format("Key {0} event of key {1}", e.KeyData.EventType, e.KeyData.Keyname));
        //        };

        //        //mouseWatcher = eventHookFactory.GetMouseWatcher();
        //        //mouseWatcher.Start();
        //        //mouseWatcher.OnMouseInput += (s, e) =>
        //        //{
        //        //    Console.WriteLine(string.Format("Mouse event {0} at point {1},{2}", e.Message.ToString(), e.Point.x, e.Point.y));
        //        //};

        //        //var clipboardWatcher = eventHookFactory.GetClipboardWatcher();
        //        //clipboardWatcher.Start();
        //        //clipboardWatcher.OnClipboardModified += (s, e) =>
        //        //{
        //        //    Console.WriteLine(string.Format("Clipboard updated with data '{0}' of format {1}", e.Data, e.DataFormat.ToString()));
        //        //};


        //        //var applicationWatcher = eventHookFactory.GetApplicationWatcher();
        //        //applicationWatcher.Start();
        //        //applicationWatcher.OnApplicationWindowChange += (s, e) =>
        //        //{
        //        //    Console.WriteLine(string.Format("Application window of '{0}' with the title '{1}' was {2}", e.ApplicationData.AppName, e.ApplicationData.AppTitle, e.Event));
        //        //};

        //        //var printWatcher = eventHookFactory.GetPrintWatcher();
        //        //printWatcher.Start();
        //        //printWatcher.OnPrintEvent += (s, e) =>
        //        //{
        //        //    Console.WriteLine(string.Format("Printer '{0}' currently printing {1} pages.", e.EventData.PrinterName, e.EventData.Pages));
        //        //};

        //        //waiting here to keep this thread running           
        //        //Console.Read();

        //        //stop watching

        //        //clipboardWatcher.Stop();
        //        //applicationWatcher.Stop();
        //        //printWatcher.Stop();
        //    }


        //}

        /// ---------------------------------------------------------------------

        #region Detect new usb

        private void DetectUSB()
        {
            HwndSource hwndSource = HwndSource.FromHwnd(Process.GetCurrentProcess().MainWindowHandle);
            if (hwndSource != null)
            {
                IntPtr windowHandle = hwndSource.Handle;
                hwndSource.AddHook(UsbNotificationHandler);
                USBDetector.RegisterUsbDeviceNotification(windowHandle);
            }
        }

        private IntPtr UsbNotificationHandler(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            if (msg == USBDetector.UsbDevicechange)
            {
                switch ((int)wparam)
                {
                    case USBDetector.UsbDeviceRemoved:
                        MessageBox.Show("USB Removed");
                        break;
                    case USBDetector.NewUsbDeviceConnected:
                        MessageBox.Show("New USB Detected");
                        break;
                }
            }

            handled = false;
            return IntPtr.Zero;
        }

        #endregion

    }
}