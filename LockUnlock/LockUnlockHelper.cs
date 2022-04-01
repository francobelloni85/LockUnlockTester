using Microsoft.Win32;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        #region USB Devcon

        /// <summary>
        /// Output file
        /// </summary>
        public static string devconFileOutput = "devcon_output.txt";
        public static string devconAllHardwareOutput = "hwids.txt";

        /// <summary>
        /// Get the device list read by de the devcon tool
        /// The result is save in the devconFileOutput file
        /// </summary>
        /// <returns></returns>
        public static List<DevconUSB> GetDevConUSBList()
        {
            var result = new List<DevconUSB>();
            try
            {
                var startupPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                var pathFile_listusbtxt = Path.Combine(startupPath, devconFileOutput);

                ExecuteCommand("devcon status USB\\* > " + devconFileOutput);

                if (File.Exists(devconFileOutput))
                {
                    var fileText = File.ReadAllText(pathFile_listusbtxt);

                    var line = fileText.Split('\n');
                    if (line.Length < 3)
                    {
                        return new List<DevconUSB>();
                    }

                    var count = 1;
                    for (int i = 0; i < line.Length - 3; i += 3)
                    {
                        var path = line[i].Trim();
                        var name = line[i + 1].Trim().Replace("Name: ", "");
                        var driveIsRunning = line[i + 2].Trim();
                        result.Add(new DevconUSB(count, path, name, driveIsRunning));
                        count++;
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
        /// Get the device list read by de the devcon tool
        /// The result is save in the devconAllHardwareOutput file
        /// </summary>
        /// <returns></returns>
        public static List<DevConaHardwareDevice> GetDevConaAllHardware()
        {
            var result = new List<DevConaHardwareDevice>();
            try
            {
                var startupPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                var pathFile_listusbtxt = Path.Combine(startupPath, devconAllHardwareOutput);

                ExecuteCommand("devcon hwids * > " + devconAllHardwareOutput);

                if (File.Exists(devconAllHardwareOutput))
                {
                    var fileText = File.ReadAllText(devconAllHardwareOutput);

                    var lines = fileText.Split('\n');
                    if (lines.Length < 3)
                    {
                        return new List<DevConaHardwareDevice>();
                    }

                    // Trasforma in lista di componenti in base alla formattazione
                    DevConaHardwareDevice lastComponent = null;
                    DevConType currentType = DevConType.notSet;

                    foreach (var line in lines)
                    {
                        if (line.Length == 0)
                            continue;

                        var firstChar = line[0];

                        if (firstChar != ' ')
                        {
                            if (lastComponent != null) {
                                result.Add(lastComponent);
                            }                            
                            lastComponent = new DevConaHardwareDevice();
                            lastComponent.ID = line;
                            lastComponent.Count = result.Count;
                            currentType = DevConType.notSet;
                        }
                        else
                        {
                            var currentLine = line.Trim();

                            if (currentLine.StartsWith("Name:"))
                            {
                                lastComponent.Name = line.Replace("Name:","").Trim();
                                currentType = DevConType.notSet;
                            }
                            else
                            {
                                if (currentLine.StartsWith("Hardware IDs:"))
                                {
                                    currentType = DevConType.HardwareIDs;
                                    continue;
                                }
                                if (currentLine.StartsWith("Compatible IDs:"))
                                {
                                    currentType = DevConType.CompatibleIDs;
                                    continue;
                                }
                                switch (currentType)
                                {
                                    case DevConType.CompatibleIDs:
                                        lastComponent.CompatibleIDs.Add(currentLine);
                                        break;
                                    case DevConType.HardwareIDs:
                                        lastComponent.HardwareIDs.Add(currentLine);
                                        break;
                                }
                            }
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
        /// Return the description from the list by the id
        /// The device id start with USB but the meaninfull description is in the HID\deviceID         
        /// </summary>
        /// <param name="list"></param>
        /// <param name="deviceID"></param>
        /// <param name="defaultName"></param>
        /// <returns></returns>
        public static string GetDeviceNameByID(List<DevConaHardwareDevice> list, string deviceID, string defaultName)
        {
            var result = defaultName;
            try
            {
                var arrNameToFind = deviceID.Split('\\');
                if (arrNameToFind.Length < 1)
                    return deviceID;
                // arrNameToFind[0] 
                var nameToFind = "HID"+ "\\" +  arrNameToFind[1];
                var index = list.FindIndex(t => t.ID.ToLower().Contains(nameToFind.ToLower()));
                if (index != -1) {
                    return list[index].Name;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return result;

        }

        /// <summary>
        /// Enable or disable the USB device with DevCon
        /// </summary>
        /// <param name="HardwareID"></param>
        /// <param name="enable"></param>
        public static void EnableDeviceDevCon(string HardwareID, bool enable)
        {

            string command = "devcon enable \"@" + HardwareID + "\" > " + devconFileOutput;
            if (enable == false)
            {
                command = "devcon disable \"@" + HardwareID + "\" > " + devconFileOutput;
            }
            ExecuteCommand(command);

        }

        /// <summary>
        /// Open the shell to execute a command
        /// </summary>
        /// <param name="command"></param>
        private static void ExecuteCommand(string command)
        {
            int exitCode;
            ProcessStartInfo processInfo;
            Process process;

            processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            // *** Redirect the output ***
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardOutput = true;

            process = Process.Start(processInfo);
            process.WaitForExit();

            // *** Read the streams ***
            // Warning: This approach can lead to deadlocks, see Edit #2
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            exitCode = process.ExitCode;

            if (Debugger.IsAttached)
            {
                Console.WriteLine("output>>" + (String.IsNullOrEmpty(output) ? "(none)" : output));
                Console.WriteLine("error>>" + (String.IsNullOrEmpty(error) ? "(none)" : error));
                Console.WriteLine("ExitCode: " + exitCode.ToString(), "ExecuteCommand");
            }

            process.Close();
        }


        #endregion

        #region Virtual keyboard by process

        private static ManagementEventWatcher startWatch;

        public static bool IsKeyboardOnScreenAllowed() { return (startWatch != null); }

        /// <summary>
        /// Try to disable the keyboarad by killing the process when its arrived
        /// </summary>
        public static bool EnableKeyboardOnScreen(bool status)
        {
            try
            {
                if (status)
                {
                    startWatch = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
                    startWatch.EventArrived += new EventArrivedEventHandler(startWatch_EventArrived);
                    startWatch.Start();
                }
                else
                {
                    if (startWatch == null)
                    {
                        Logger.Error("Tried to disable an object that was never initialized (startWatch)");
                        return false;
                    }
                    else
                    {
                        startWatch.Stop();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }

            return true;
        }

        static void startWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            try
            {
                Console.WriteLine("Process started: {0}", e.NewEvent.Properties["ProcessName"].Value);
                Process[] processes = Process.GetProcesses();
                foreach (Process process in processes)
                {
                    Console.WriteLine(">>>: {0}", process.ProcessName);

                    if (process.ProcessName == "osk")
                    {
                        if (!process.HasExited)
                        {
                            Process.GetProcessById(process.Id).Kill();
                        }
                    }

                    if (process.ProcessName.ToLower() == "taskmgr")
                    {
                        if (!process.HasExited)
                        {
                            Process.GetProcessById(process.Id).Kill();
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }


        }

        #endregion

        #region Virtual keyboard register [NON SEMBRA FUNZIONARE]


        /// <summary>
        /// Try to disable the keyboarad by writing in windows register
        /// </summary>
        public static bool SetKeyboardByRegistry(bool status)
        {
            // Non sembra funzionare al 100%... da testare 
            throw  new Exception("TO TEST");

            var result = false;

            int statusToSet = status == true ? 1 : 0;

            try
            {
                //disable USB storage...
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Authentication\LogonUI", "ShowTabletKeyboard", (int)statusToSet, RegistryValueKind.DWord);
                result = true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return result;

        }

        public static bool ReadKeyboardByRegistry()
        {
            // Non sembra funzionare al 100%... da testare 
            throw new Exception("TO TEST");

            var status = false;
            try
            {
                RegistryKey rk = Registry.LocalMachine;
                var temp = rk.OpenSubKey("Software").OpenSubKey("Microsoft").OpenSubKey("Windows").OpenSubKey("CurrentVersion").OpenSubKey("Authentication").OpenSubKey("LogonUI");
                var value = temp.GetValue("ShowTabletKeyboard");
                return status;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return status;
        }


        #endregion

        #region Virtual keyboard [NON SEMBRA FUNZIONARE]

        /// <summary>
        /// Try to enable the virtual keyboard
        /// </summary>
        public static void EnableVirtualKeyboard()
        {
            // Non sembra funzionare al 100%... da testare 
            throw new Exception("TO TEST");

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
            // Non sembra funzionare al 100%... da testare 
            throw new Exception("TO TEST");

            var command = "sc config \"TabletInputService\" start= auto";
            Process.Start("cmd.exe", "/C " + command);
            command = "sc start \"TabletInputService\"";
            Process.Start("cmd.exe", "/C " + command);
        }


        #endregion

        #region TaskManager

        /// <summary>
        /// DEPRECATO
        /// </summary>
        /// <param name="enable"></param>
        public static void SetTaskManagerByRegistry(bool enable)
        {
            throw new Exception("Need to restart the pc, use SetTaskManagerByCmd");
            //// OLD Windows
            ////Microsoft.Win32.RegistryKey objRegistryKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System");

            //// windows 10 
            //Microsoft.Win32.RegistryKey objRegistryKey = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");

            //if (enable && objRegistryKey.GetValue("DisableTaskMgr") != null)
            //    objRegistryKey.DeleteValue("DisableTaskMgr");
            //else
            //    objRegistryKey.SetValue("DisableTaskMgr", "1");
            //objRegistryKey.Close();
        }

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

        public static void ShowTaskbar(bool value) {
            if (value)
            {
                Taskbar.Show();
            }
            else {
                Taskbar.Hide();
            }            
        }


        ///// <summary>
        ///// Try to hide/show the task bar in windows register
        ///// Nasconde e basta
        ///// </summary>
        //public static void HideTaskBarByCmd(bool enable)
        //{
        //    // Disable
        //    var value = 3;
        //    if (enable)
        //    {
        //        value = 2;
        //    }
        //    var command = "powershell -command \"&{$p='HKCU:SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StuckRects3';$v=(Get-ItemProperty -Path $p).Settings;$v[8]=" + value + ";&Set-ItemProperty -Path $p -Name Settings -Value $v;&Stop-Process -f -ProcessName explorer}\"";
        //    Process.Start("cmd.exe", "/C " + command);
        //}

        #endregion

        #region Task view

        /// <summary>
        /// Try to hide/show the task view in windows register
        /// </summary>        
        public static bool SetTaskView(bool enable)
        {
            var result = false;

            int statusToSet = (enable == true) ? 1 : 0;

            try
            {
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\EdgeUI", "AllowEdgeSwipe", (int)statusToSet, RegistryValueKind.DWord);
                result = true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return result;
        }

        public static bool ReadTaskView()
        {
            try
            {
                RegistryKey rk = Registry.LocalMachine;
                var temp = rk.OpenSubKey("Software").OpenSubKey("Policies").OpenSubKey("Microsoft").OpenSubKey("Windows").OpenSubKey("EdgeUI");
                var value = (int)temp.GetValue("AllowEdgeSwipe");
                if (value == 0)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return false;
        }

        #endregion

        public static bool GetKeyboardPresent()
        {
            bool keyboardPresent = false;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * from Win32_Keyboard");

            foreach (ManagementObject keyboard in searcher.Get())
            {
                foreach (PropertyData prop in keyboard.Properties)
                {
                    if (Convert.ToString(prop.Value).Contains("USB"))
                    {
                        keyboardPresent = true;
                        break;
                    }
                }
            }

            return keyboardPresent;
        }

    }
}