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
        //public static string PathToFile = "LastUser.txt"; // not implemented yet
        //public static UserClient UserClient { get; set; }
        //public static void SaveLastUserToFile(UserClient userClient)
        //{
        //    using (StreamWriter writer = new StreamWriter(PathToFile))
        //    {
        //        StringBuilder builder = new StringBuilder();
        //        builder.Append(userClient.IdUser).Append(",")
        //               .Append(userClient.Name).Append(",")
        //               .Append(userClient.Username).Append(",")
        //               .Append(userClient.Password);
        //        writer.Write(builder.ToString());
        //    }
        //}

        //public static void UpdateUserDetails(UserClient user)
        //{
        //    UserClient = user;
        //    SaveLastUserToFile(user);
        //}

        //public static UserClient GetLastUserDetails()
        //{
        //    if (UserClient != null)
        //    {
        //        return UserClient;
        //    }
        //    else
        //    {
        //        // If UserClient is null, try to read from the file
        //        if (System.IO.File.Exists(PathToFile))
        //        {
        //            using (StreamReader reader = new StreamReader(PathToFile))
        //            {
        //                string content = reader.ReadToEnd();
        //                string[] parts = content.Split(',');
        //                UserClient user = new UserClient
        //                {
        //                    IdUser = int.Parse(parts[0]),
        //                    Name = parts[1],
        //                    Username = parts[2],
        //                    Password = parts[3]
        //                };
        //                UserClient = user;
        //                return user;
        //            }
        //        }
        //        else
        //        {
        //            return null; // No user details available
        //        }
        //    }
        //}

        public static void saveIdUsertoFile(int userId)
        {
            string path = "LastUser.txt";
            using (StreamWriter writer = new StreamWriter(path));
            using( StreamWriter writer = new StreamWriter(path))
            {
                writer.Write(userId.ToString());
            }
        }

        public static int readIdUserFromFile()
        {
            string path = "LastUser.txt"; // where the file is located ??
            if (File.Exists(path))
            {
                using (StreamReader reader = new StreamReader(path))
                {
                    string content = reader.ReadToEnd();
                    if (int.TryParse(content, out int userId))
                    {
                        return userId;
                    }
                }
            }
            return -1; // Return -1 if the file does not exist or the content is invalid
        }

        public static int LocalUser { get; set; }

        public static int localUser(int user)
        {
            LocalUser = user;
            return LocalUser;
        }
    }
}
