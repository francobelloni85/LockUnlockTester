using System;

namespace LockUnlock
{

    /// <summary>
    /// Support class when devices are read with DevCon
    /// </summary>
    public class DevconUSB
    {
        public int Count { get; private set; }
        public string HardwareID { get; private set; }
        public string Name { get; private set; }
        public string IsRunning { get; private set; }

        public string Type { get; private set; }

        public DevconUSB(int count, string hardwareID, string name, string isRunning)
        {
            this.Count = count;
            this.HardwareID = hardwareID;
            this.Name = name;
            this.IsRunning = isRunning;
        }


    }

    /// <summary>
    /// Class representing a USB device Mode1
    /// </summary>
    public class USBDeviceInfo
    {
        public USBDeviceInfo(int count, string deviceID, string pnpDeviceID, string description, string status, bool isEnable)
        {
            this.Count = count;
            this.DeviceID = deviceID;
            this.PnpDeviceID = pnpDeviceID;
            this.Description = description;
            this.IsEnable = isEnable;
            this.Status = status;
        }

        public int Count { get; private set; }
        public string DeviceID { get; private set; }
        public string PnpDeviceID { get; private set; }
        public string Description { get; private set; }
        public string Status { get; set; }
        public bool IsEnable { get; set; }
        public string ClassGuid { get; set; }

    }

    /// <summary>
    /// Class representing a USB device Mode by API
    /// </summary>

    public class GenericDeviceInfo
    {
        public GenericDeviceInfo(int count, string deviceID, Guid classGuid, string description, bool isEnable, string statusInfo)
        {
            this.Count = count;
            this.DeviceID = deviceID;
            this.ClassGuid = classGuid;
            this.Description = description;
            this.IsEnable = isEnable;
            this.StatusInfo = statusInfo;
        }

        public int Count { get; private set; }
        public string DeviceID { get; private set; }
        public string Description { get; private set; }
        public string Status { get; set; }
        public bool IsEnable { get; set; }
        public string StatusInfo { get; set; }

        public Guid ClassGuid { get; set; }
    }


}
