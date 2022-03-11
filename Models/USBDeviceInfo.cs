using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CG.LockUnlockTester
{
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

    }
}
