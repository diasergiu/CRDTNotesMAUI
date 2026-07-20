using DatabaseLibrary.Entities;

namespace Server.RequestBodies
{
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string DeviceId { get; set; }
        public List<ISyncQueue> ChangesMade { get; set; }
    }
}
