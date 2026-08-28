using DatabaseLibrary.Entities.Client;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DatabaseLibrary.WrapperClasses
{
    public static class UserDevice
    {
        public static void saveIdUsertoFile(Guid userId)
        {
            string path = "LastUser.txt";
            using (StreamWriter writer = new StreamWriter(path));
            using( StreamWriter writer = new StreamWriter(path))
            {
                writer.Write(userId.ToString());
            }
        }

        public static Guid readIdUserFromFile()
        {
            string path = "LastUser.txt"; // where the file is located ??
            if (File.Exists(path))
            {
                using (StreamReader reader = new StreamReader(path))
                {
                    string content = reader.ReadToEnd();
                    if (Guid.TryParse(content, out Guid userId))
                    {
                        return userId;
                    }
                }
            }
            return Guid.Empty; // Return Guid.Empty if the file does not exist or the content is invalid
        }

        public static Guid LocalUser { get; set; }

        // SignalR connection id of this application instance.
        // Sent with every update so the server can exclude only this connection,
        // not every connection belonging to the same user.
        public static string? HubConnectionId { get; set; }

        public static Guid SetLocalUser(Guid user)
        {
            LocalUser = user;
            return LocalUser;
        }

        public static void Logout()
        {
            LocalUser = Guid.Empty;
        }
    }
}
