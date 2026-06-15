using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Text;

namespace CRDT_TestShering.Services
{
    public class BaseURLGetter
    {
        public static string getBaseURL()
        {
            return DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:5000/Login"  // Android emulator
                : "http://localhost:5000/Login"; // Windows/iOS/Mac
        }
    }
}
