using Microsoft.Maui.Storage;

namespace MAUIClientUI.Services
{
    // (s)not a service not one that communicates with the server
    public class DeviceIdentityService
    {
        private const string DeviceIdKey = "device_id";

        /// <summary>
        /// Get or create a unique device ID for this client
        /// </summary>
        /// // this should change to string and the Id device should be string
        public static int GetDeviceId()
        {
            var deviceId = Preferences.Get(DeviceIdKey, string.Empty);

            if (string.IsNullOrEmpty(deviceId))
            {
                // Generate new device ID on first run
                deviceId = Guid.NewGuid().ToString();
                Preferences.Set(DeviceIdKey, deviceId);
            }

            //return int.Parse(deviceId);
            return 1;
        }

        /// <summary>
        /// Get current user ID from login session
        /// </summary>
        public static int GetCurrentUserId()
        {
            return Preferences.Get("user_id", -1);
        }

        /// <summary>
        /// Store user ID after login
        /// </summary>
        public static void SetCurrentUserId(int userId)
        {
            Preferences.Set("user_id", userId);
        }

        /// <summary>
        /// Clear session on logout
        /// </summary>
        public static void ClearSession()
        {
            Preferences.Remove("user_id");
        }
    }
}