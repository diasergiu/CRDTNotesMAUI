using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Text;

namespace MAUIClientUI.Services
{
    public class BaseURLGetter
    {
        public static string getBaseURL()
        {
            return DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:5266"  // Android emulator
                : "http://localhost:5266"; // Windows/iOS/Mac
        }
    }
}
