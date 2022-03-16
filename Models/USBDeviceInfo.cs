using System;

namespace CG.LockUnlockTester
{
    /// <summary>
    /// Class representing a USB device
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


        public USBDeviceInfo(int count, string deviceID, string classGuid, bool isEnable)
        {
            this.Count = count;
            this.DeviceID = deviceID;
            this.ClassGuid = classGuid;
            this.IsEnable = isEnable;
        }


        public int Count { get; private set; }
        public string DeviceID { get; private set; }
        public string PnpDeviceID { get; private set; }
        public string Description { get; private set; }
        public string Status { get; set; }
        public bool IsEnable { get; set; }
        public string ClassGuid { get; set; }

    }


    public class GenericDeviceInfo
    {
        public GenericDeviceInfo(int count, string deviceID, Guid classGuid, string description, bool isEnable)
        {
            this.Count = count;
            this.DeviceID = deviceID;
            this.ClassGuid = classGuid;
            this.Description = description;
            this.IsEnable = isEnable;
        }

        public int Count { get; private set; }
        public string DeviceID { get; private set; }
        public string Description { get; private set; }
        public string Status { get; set; }
        public bool IsEnable { get; set; }
        public Guid ClassGuid { get; set; }
    }


}
