using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BluetoothMonitor.Core
{
    public record DeviceBatteryLevel(string DeviceId, int BatteryLevel);
}
