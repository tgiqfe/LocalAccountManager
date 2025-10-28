using LocalAccountManager.LocalAccount;
using System.Text.Json;

namespace LocalAccountManager
{
    internal class LocalAccountManager
    {
        public static void ListUsers()
        {
            var localUsers = LocalUser.Load();
            string json = JsonSerializer.Serialize(localUsers,
                new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true
                });
            Console.WriteLine(json);
        }

        public static void ListGroups()
        {
            var localGroups = LocalGroup.Load();
            string json = JsonSerializer.Serialize(localGroups,
                new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true
                });
            Console.WriteLine(json);
        }

        public static void GetUser(string userName)
        {
            var user = new LocalUser(userName);
            if (user != null)
            {
                string json = JsonSerializer.Serialize(user,
                    new JsonSerializerOptions
                    {
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                        WriteIndented = true
                    });
                Console.WriteLine(json);
            }
        }

        public static void SetUser(string userName, ModifyParam modifyParam)
        {
            var user = new LocalUser(userName);
            user.SetParam(modifyParam);
        }

        public static void AddUser(string userName)
        {
            LocalUser.Add(userName);
        }

        public static void RemoveUser(string userName)
        {
            var user = new LocalUser(userName);
            user.Remove();
        }

        public static void RenameUser(string userName, string newName)
        {
            var user = new LocalUser(userName);
            user.Rename(newName);
        }

        public static void ChangePassword(string userName, string password)
        {
            var user = new LocalUser(userName);
            user.ChangePassword(password);
        }

        public static void AddUserToGroup(string userName, string groupName)
        {
            var user = new LocalUser(userName);
            user.JoinGroup(groupName);
        }

        public static void RemoveUserFromGroup(string userName, string groupName)
        {
            var user = new LocalUser(userName);
            user.LeaveGroup(groupName);
        }
    }
}
