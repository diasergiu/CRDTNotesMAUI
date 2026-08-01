using Microsoft.Maui.Storage;

namespace MAUIClientUI.Services
{
    // (s)not a service not one that communicates with the server
    public class DeviceIdentityService
    {
        private static string DeviceIdKey = "device_id";

        /// <summary>
        /// Get or create a unique device ID for this client
        /// </summary>
        /// // this should change to string and the Id device should be string
        public static Guid GetDeviceId()
        {
            string deviceIdString = Preferences.Get(DeviceIdKey, string.Empty);
            Guid deviceId = Guid.Empty;

            if (string.IsNullOrEmpty(deviceIdString))
            {
                // Generate new device ID on first run
                deviceId = Guid.NewGuid();     
                Preferences.Set(DeviceIdKey, deviceId.ToString());
            }
            else
            {
                Guid.TryParse(deviceIdString, out deviceId);
            }
            return deviceId;
        }

        /// <summary>
        /// Get current user ID from login session
        /// </summary>
        public static Guid GetCurrentUserId()
        {
            string userIdString = Preferences.Get("user_id", string.Empty);
            if (Guid.TryParse(userIdString, out Guid userId))
            {
                return userId;
            }
            return Guid.Empty;
        }

        /// <summary>
        /// Store user ID after login
        /// </summary>
        public static void SetCurrentUserId(Guid userId)
        {
            Preferences.Set("user_id", userId.ToString());
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