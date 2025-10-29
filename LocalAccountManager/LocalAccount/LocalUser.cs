using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Management;

namespace LocalAccountManager.LocalAccount
{
    internal class LocalUser
    {
        #region public parameter

        public string Name { get; private set; }
        public string FullName { get; private set; }
        public string Description { get; set; }
        public bool UserMustChangePasswordAtNextLogon { get; private set; }
        public bool UserCannotChangePassword { get; private set; }
        public bool PasswordNeverExpires { get; private set; }
        public bool AccountIsDisabled { get; private set; }
        public bool AccountIsLockedOut { get; private set; }
        public string[] JoinedGroup { get; private set; }
        public string ProfilePath { get; private set; }
        public string LogonScript { get; private set; }
        public string HomeDirectory { get; private set; }
        public string HomeDrive { get; private set; }
        public string SID { get; private set; }
        public DateTime LastLogonTime { get; private set; }

        #endregion

        #region private parameter

        private bool _isDeleted = false;
        const string _log_target = "local user";

        #endregion

        #region Constructor / Initializer

        public LocalUser() { }

        public LocalUser(string name)
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_UserAccount WHERE LocalAccount=True"))
            using (var directoryEntry = new DirectoryEntry($"WinNT://{Environment.MachineName},computer"))
            using (var context = new PrincipalContext(ContextType.Machine, Environment.MachineName))
            {
                using (var wmi = searcher.Get().
                    OfType<ManagementObject>().
                    FirstOrDefault(u => string.Equals(u["Name"]?.ToString(), name, StringComparison.OrdinalIgnoreCase)))
                using (var entry = directoryEntry.Children.Find(name, "User"))
                using (var principal = UserPrincipal.FindByIdentity(context, name))
                {
                    Initialize(wmi, entry, principal);
                }
            }
        }

        public LocalUser(ManagementObject wmi, DirectoryEntry entry, UserPrincipal principal)
        {
            Initialize(wmi, entry, principal);
        }

        /// <summary>
        /// Initialize object properties.
        /// </summary>
        /// <param name="wmi"></param>
        /// <param name="entry"></param>
        /// <param name="principal"></param>
        private void Initialize(ManagementObject wmi, DirectoryEntry entry, UserPrincipal principal)
        {
            if (wmi == null || entry == null || principal == null) return;

            this.Name = wmi["Name"]?.ToString();
            this.FullName = entry.Properties["FullName"]?.Value?.ToString();
            this.Description = entry.Properties["Description"]?.Value?.ToString();
            this.UserMustChangePasswordAtNextLogon = entry.Properties["PasswordExpired"].Value?.ToString() == "1";
            this.UserCannotChangePassword = principal.UserCannotChangePassword;
            this.PasswordNeverExpires = principal.PasswordNeverExpires;
            this.AccountIsDisabled = principal.Enabled.HasValue && !principal.Enabled.Value;
            this.AccountIsLockedOut = principal.IsAccountLockedOut();
            this.JoinedGroup = principal.GetGroups().Select(g => g.Name).ToArray();
            this.ProfilePath = entry.Properties["Profile"].Value?.ToString() ?? string.Empty;
            this.LogonScript = entry.Properties["LoginScript"].Value?.ToString() ?? string.Empty;
            this.HomeDirectory = entry.Properties["HomeDirectory"].Value?.ToString() ?? string.Empty;
            this.HomeDrive = entry.Properties["HomeDirDrive"].Value?.ToString() ?? string.Empty;
            this.SID = principal.Sid.ToString();
            this.LastLogonTime = principal.LastLogon.HasValue ?
                principal.LastLogon.Value.ToLocalTime() :
                DateTime.MinValue;
        }

        /// <summary>
        /// Reflesh parameters.
        /// </summary>
        public void Reflesh()
        {
            string name = this.Name;
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_UserAccount WHERE LocalAccount=True"))
            using (var directoryEntry = new DirectoryEntry($"WinNT://{Environment.MachineName},computer"))
            using (var context = new PrincipalContext(ContextType.Machine, Environment.MachineName))
            {
                using (var wmi = searcher.Get().
                    OfType<ManagementObject>().
                    FirstOrDefault(u => string.Equals(u["Name"]?.ToString(), name, StringComparison.OrdinalIgnoreCase)))
                using (var entry = directoryEntry.Children.Find(name, "User"))
                using (var principal = UserPrincipal.FindByIdentity(context, name))
                {
                    Initialize(wmi, entry, principal);
                }
            }
        }

        /// <summary>
        /// Create LocalUser array of all local users.
        /// </summary>
        /// <returns></returns>
        public static LocalUser[] Load()
        {
            List<LocalUser> list = new();
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_UserAccount WHERE LocalAccount=True"))
            using (var directoryEntry = new DirectoryEntry($"WinNT://{Environment.MachineName},computer"))
            using (var context = new PrincipalContext(ContextType.Machine, Environment.MachineName))
            {
                using (var users_wmi = searcher.Get())
                {
                    foreach (ManagementObject wmi in users_wmi)
                    {
                        string name = wmi["Name"]?.ToString();
                        using (var entry = directoryEntry.Children.Find(name, "User"))
                        using (var principal = UserPrincipal.FindByIdentity(context, name))
                        {
                            if (entry != null && principal != null)
                                list.Add(new LocalUser(wmi, entry, principal));
                        }
                    }
                }
            }
            return list.ToArray();
        }

        #endregion

        #region Checking methods

        /// <summary>
        /// User exists check.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="directoryEntry"></param>
        /// <returns></returns>
        public static bool Exists(string name, DirectoryEntry directoryEntry = null)
        {
            Logger.WriteLine("Info", $"Checking existence of {_log_target}. name: {name}");
            if (directoryEntry == null)
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_UserAccount WHERE LocalAccount=True"))
                using (var users_wmi = searcher.Get())
                {
                    return users_wmi.
                        OfType<ManagementObject>().
                        Any(u => string.Equals(u["Name"]?.ToString(), name, StringComparison.OrdinalIgnoreCase));
                }
            }
            else
            {
                return directoryEntry.Children.
                    OfType<DirectoryEntry>().
                    Any(e => e.SchemaClassName == "User" && e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// User is in group check. (static)
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public static bool IsMemberOf(string userName, string groupName)
        {
            Logger.WriteLine("Info", $"Checking membership of {_log_target}. user: {userName}, group: {groupName}");
            using (var context = new PrincipalContext(ContextType.Machine, Environment.MachineName))
            using (var user = UserPrincipal.FindByIdentity(context, userName))
            using (var group = GroupPrincipal.FindByIdentity(context, groupName))
            {
                if (user == null || group == null) return false;
                return user.IsMemberOf(group);
            }
        }

        /// <summary>
        /// User is in group check. (instance)
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public bool IsMemberOf(string groupName)
        {
            Logger.WriteLine("Info", $"Checking membership of {_log_target}. user: {this.Name}, group: {groupName}");
            return this.JoinedGroup != null &&
                this.JoinedGroup.Any(g => string.Equals(g, groupName, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        /// <summary>
        /// Get local user parameter.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static LocalUser GetParam(string name)
        {
            Logger.WriteLine("Info", $"Getting parameter of {_log_target}. name: {name}");
            using (var directoryEntry = new DirectoryEntry($"WinNT://{Environment.MachineName},computer"))
            {
                return LocalUser.Exists(name, directoryEntry) ? new LocalUser(name) : null;
            }
        }

        /// <summary>
        /// Set local user parameter. need ModifyParam object.
        /// </summary>
        /// <param name="fullName"></param>
        /// <param name="description"></param>
        /// <param name="userMustChangePasswordAtNextLogon"></param>
        /// <param name="userCannotChangePassword"></param>
        /// <param name="passwordNeverExpires"></param>
        /// <param name="accountIsDisabled"></param>
        /// <param name="profilePath"></param>
        /// <param name="logonScript"></param>
        /// <param name="homeDirectory"></param>
        /// <param name="homeDrive"></param>
        /// <returns></returns>
        public bool SetParam(
            string fullName,
            string description,
            bool? userMustChangePasswordAtNextLogon,
            bool? userCannotChangePassword,
            bool? passwordNeverExpires,
            bool? accountIsDisabled,
            string profilePath,
            string logonScript,
            string homeDirectory,
            string homeDrive)
        {
            Logger.WriteLine("Info", $"Setting parameter of {_log_target}. name: {this.Name}");
            if (_isDeleted)
            {
                Logger.WriteLine("Warning", $"Cannot set parameter of already deleted {_log_target}.");
                return false;
            }

            bool isMemberOfAdministrators = this.IsMemberOf("Administrators");
            if (isMemberOfAdministrators)
            {
                Logger.WriteLine("Info", $"{_log_target} is member of Administrators group. Temporarily leaving the group to modify parameters.");
                LeaveGroup("Administrators");
            }

            string name = this.Name;
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_UserAccount WHERE LocalAccount=True"))
            using (var directoryEntry = new DirectoryEntry($"WinNT://{Environment.MachineName},computer"))
            using (var context = new PrincipalContext(ContextType.Machine, Environment.MachineName))
            {
                try
                {
                    using (var wmi = searcher.Get().
                        OfType<ManagementObject>().
                        FirstOrDefault(u => string.Equals(u["Name"]?.ToString(), name, StringComparison.OrdinalIgnoreCase)))
                    using (var entry = directoryEntry.Children.
                        OfType<DirectoryEntry>().
                        FirstOrDefault(e => e.SchemaClassName == "User" && e.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    using (var principal = UserPrincipal.FindByIdentity(context, name))
                    {
                        bool isChangeEntry = false;
                        bool isChangePrincipal = false;
                        if (fullName != null && fullName != this.FullName)
                        {
                            Logger.WriteLine("Info", $"Changing FullName to '{fullName}'.");
                            entry.Properties["FullName"].Value = fullName;
                            this.FullName = fullName;
                            isChangeEntry = true;
                        }
                        if (description != null && description != this.Description)
                        {
                            Logger.WriteLine("Info", $"Changing Description to '{description}'.");
                            entry.Properties["Description"].Value = description;
                            this.Description = description;
                            isChangeEntry = true;
                        }
                        if (userMustChangePasswordAtNextLogon.HasValue &&
                            userMustChangePasswordAtNextLogon.Value != this.UserMustChangePasswordAtNextLogon)
                        {
                            Logger.WriteLine("Info", $"Changing UserMustChangePasswordAtNextLogon to '{userMustChangePasswordAtNextLogon.Value}'.");
                            entry.Properties["PasswordExpired"].Value = userMustChangePasswordAtNextLogon.Value ? "1" : "0";
                            this.UserMustChangePasswordAtNextLogon = userMustChangePasswordAtNextLogon.Value;
                            isChangeEntry = true;
                        }
                        if (userCannotChangePassword.HasValue &&
                            userCannotChangePassword.Value != this.UserCannotChangePassword)
                        {
                            Logger.WriteLine("Info", $"Changing UserCannotChangePassword to '{userCannotChangePassword.Value}'.");
                            principal.UserCannotChangePassword = userCannotChangePassword.Value;
                            this.UserCannotChangePassword = userCannotChangePassword.Value;
                            isChangePrincipal = true;
                        }
                        if (passwordNeverExpires.HasValue &&
                            passwordNeverExpires.Value != this.PasswordNeverExpires)
                        {
                            Logger.WriteLine("Info", $"Changing PasswordNeverExpires to '{passwordNeverExpires.Value}'.");
                            principal.PasswordNeverExpires = passwordNeverExpires.Value;
                            this.PasswordNeverExpires = passwordNeverExpires.Value;
                            isChangePrincipal = true;
                        }
                        if (accountIsDisabled.HasValue &&
                            accountIsDisabled.Value != this.AccountIsDisabled)
                        {
                            Logger.WriteLine("Info", $"Changing AccountIsDisabled to '{accountIsDisabled.Value}'.");
                            principal.Enabled = !accountIsDisabled.Value;
                            this.AccountIsDisabled = accountIsDisabled.Value;
                            isChangePrincipal = true;
                        }
                        if (profilePath != null && profilePath != this.ProfilePath)
                        {
                            Logger.WriteLine("Info", $"Changing ProfilePath to '{profilePath}'.");
                            entry.Properties["Profile"].Value = profilePath;
                            this.ProfilePath = profilePath;
                            isChangeEntry = true;
                        }
                        if (logonScript != null && logonScript != this.LogonScript)
                        {
                            Logger.WriteLine("Info", $"Changing LogonScript to '{logonScript}'.");
                            entry.Properties["LoginScript"].Value = logonScript;
                            this.LogonScript = logonScript;
                            isChangeEntry = true;
                        }
                        if (homeDirectory != null && homeDirectory != this.HomeDirectory)
                        {
                            Logger.WriteLine("Info", $"Changing HomeDirectory to '{homeDirectory}'.");
                            entry.Properties["HomeDirectory"].Value = homeDirectory;
                            this.HomeDirectory = homeDirectory;
                            isChangeEntry = true;
                        }
                        if (homeDrive != null && homeDrive != this.HomeDrive)
                        {
                            Logger.WriteLine("Info", $"Changing HomeDrive to '{homeDrive}'.");
                            entry.Properties["HomeDirDrive"].Value = homeDrive;
                            this.HomeDrive = homeDrive;
                            isChangeEntry = true;
                        }
                        if (isChangeEntry) entry.CommitChanges();
                        if (isChangePrincipal) principal.Save();
                        Logger.WriteLine("Info", $"Successfully set parameter of {_log_target}.");
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Logger.WriteLine("Error", $"Failed to set parameter of {_log_target}. Exception: {e.ToString()}");
                    Logger.WriteRaw(e.Message);
                }

                if (isMemberOfAdministrators)
                {
                    Logger.WriteLine("Info", $"{_log_target} was member of Administrators group. Re-joining the group after modifying parameters.");
                    JoinGroup("Administrators");
                }
                return false;
            }
        }

        /// <summary>
        /// Rename local user name.
        /// </summary>
        /// <param name="newName"></param>
        public bool Rename(string newName)
        {
            Logger.WriteLine("Info", $"Renaming {_log_target}. old name: {this.Name}, new name: {newName}");
            if (_isDeleted)
            {
                Logger.WriteLine("Warning", $"Cannot rename already deleted {_log_target}.");
                return false;
            }
            using (var directoryEntry = new DirectoryEntry($"WinNT://{Environment.MachineName},computer"))
            using (var entry = directoryEntry.Children.Find(this.Name, "User"))
            {
                try
                {
                    if (entry != null)
                    {
                        entry.Rename(newName);
                        entry.CommitChanges();
                        this.Name = newName;
                        Logger.WriteLine("Info", $"Successfully renamed {_log_target}.");
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Logger.WriteLine("Error", $"Failed to rename {_log_target}. Exception: {e.ToString()}");
                    Logger.WriteRaw(e.Message);
                }
            }
            return false;
        }

        /// <summary>
        /// Create new local user.
        /// </summary>
        /// <param name="name"></param>
        public static bool New(string name)
        {
            Logger.WriteLine("Info", $"Creating new {_log_target}. name: {name}");
            using (var directoryEntry = new DirectoryEntry($"WinNT://{Environment.MachineName},computer"))
            {
                try
                {
                    if (!LocalUser.Exists(name, directoryEntry))
                    {
                        directoryEntry.Children.Add(name, "User").CommitChanges();
                        Logger.WriteLine("Info", $"Successfully created new {_log_target}.");
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Logger.WriteLine("Error", $"Failed to create new {_log_target}. Exception: {e.ToString()}");
                    Logger.WriteRaw(e.Message);
                }
            }
            return false;
        }

        /// <summary>
        /// Create new local user. (alias of New method)
        /// </summary>
        /// <param name="name"></param>
        public static bool Add(string name)
        {
            return LocalUser.Add(name);
        }

        /// <summary>
        /// Remove local user.
        /// </summary>
        public bool Remove()
        {
            Logger.WriteLine("Info", $"Deleting {_log_target}. name: {this.Name}");
            if (_isDeleted)
            {
                Logger.WriteLine("Warning", $"Cannot delete already deleted {_log_target}.");
                return false;
            }
            using (var directoryEntry = new DirectoryEntry($"WinNT://{Environment.MachineName},computer"))
            {
                try
                {
                    var entry = directoryEntry.Children.Find(this.Name, "User");
                    directoryEntry.Children.Remove(entry);
                    _isDeleted = true;
                    Logger.WriteLine("Info", $"Successfully deleted {_log_target}.");
                    return true;
                }
                catch (Exception e)
                {
                    Logger.WriteLine("Error", $"Failed to delete {_log_target}. Exception: {e.ToString()}");
                    Logger.WriteRaw(e.Message);
                }
            }
            return false;
        }

        /// <summary>
        /// Remove local user. (alias of Remove method)
        /// </summary>
        public bool Delete()
        {
            return this.Remove();
        }

        /// <summary>
        /// Change local user password.
        /// </summary>
        /// <param name="newPassword"></param>
        public bool ChangePassword(string newPassword)
        {
            Logger.WriteLine("Info", $"Changing password of {_log_target}. name: {this.Name}");
            if (_isDeleted)
            {
                Logger.WriteLine("Warning", $"Cannot change password already deleted {_log_target}.");
                return false;
            }
            if (string.IsNullOrEmpty(newPassword))
            {
                Logger.WriteLine("Warning", $"Cannot set empty password to {_log_target}.");
                return false;
            }

            using (var directoryEntry = new DirectoryEntry($"WinNT://{Environment.MachineName},computer"))
            using (var entry = directoryEntry.Children.Find(this.Name, "User"))
            {
                try
                {
                    entry.Invoke("SetPassword", new object[] { newPassword });
                    entry.CommitChanges();
                    Logger.WriteLine("Info", $"Successfully changed password of {_log_target}.");
                    return true;
                }
                catch (Exception e)
                {
                    Logger.WriteLine("Error", $"Failed to change password of {_log_target}. Exception: {e.ToString()}");
                    Logger.WriteRaw(e.Message);
                }
            }
            return false;
        }

        /// <summary>
        /// Unlock local user account.
        /// </summary>
        public bool UnlockAccount()
        {
            Logger.WriteLine("Info", $"Unlocking account of {_log_target}. name: {this.Name}");
            if (_isDeleted)
            {
                Logger.WriteLine("Warning", $"Cannot unlock account of already deleted {_log_target}.");
                return false;
            }
            string name = this.Name;
            using (var context = new PrincipalContext(ContextType.Machine, Environment.MachineName))
            using (var principal = UserPrincipal.FindByIdentity(context, name))
            {
                try
                {
                    if (principal != null && principal.IsAccountLockedOut())
                    {
                        principal.UnlockAccount();
                        principal.Save();
                        this.AccountIsLockedOut = false;
                        Logger.WriteLine("Info", $"Successfully unlocked account of {_log_target}.");
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Logger.WriteLine("Error", $"Failed to unlock account of {_log_target}. Exception: {e.ToString()}");
                    Logger.WriteRaw(e.Message);
                }
            }
            return false;
        }

        /// <summary>
        /// Join local user to local group.
        /// </summary>
        /// <param name="groupName"></param>
        public bool JoinGroup(string groupName)
        {
            Logger.WriteLine("Info", $"Joining {_log_target} to group. user: {this.Name}, group: {groupName}");
            if (_isDeleted)
            {
                Logger.WriteLine("Warning", $"Cannot join to group already deleted {_log_target}.");
                return false;
            }
            if (IsMemberOf(groupName))
            {
                Logger.WriteLine("Info", $"{_log_target} is already a member of the group.");
                return true;
            }

            string name = this.Name;
            using (var directoryEntry = new DirectoryEntry($"WinNT://{Environment.MachineName},computer"))
            using (var entry_user = directoryEntry.Children.Find(name, "User"))
            using (var entry_group = directoryEntry.Children.Find(groupName, "Group"))
            {
                try
                {
                    entry_group.Invoke("Add", new object[] { entry_user.Path });
                    entry_group.CommitChanges();
                    var list = JoinedGroup.ToList();
                    list.Add(groupName);
                    this.JoinedGroup = list.ToArray();
                    Logger.WriteLine("Info", $"Successfully joined {_log_target} to group.");
                    return true;
                }
                catch (Exception e)
                {
                    Logger.WriteLine("Error", $"Failed to join {_log_target} to group. Exception: {e.ToString()}");
                    Logger.WriteRaw(e.Message);
                }
            }
            return false;
        }

        /// <summary>
        /// Leave local user from local group.
        /// </summary>
        /// <param name="groupName"></param>
        public bool LeaveGroup(string groupName)
        {
            Logger.WriteLine("Info", $"Leaving {_log_target} from group. user: {this.Name}, group: {groupName}");
            if (_isDeleted)
            {
                Logger.WriteLine("Warning", $"Cannot leave from group already deleted {_log_target}.");
                return false;
            }
            if (!IsMemberOf(groupName))
            {
                Logger.WriteLine("Info", $"{_log_target} is not a member of the group.");
                return true;
            }

            string name = this.Name;
            using (var directoryEntry = new DirectoryEntry($"WinNT://{Environment.MachineName},computer"))
            using (var entry_user = directoryEntry.Children.Find(name, "User"))
            using (var entry_group = directoryEntry.Children.Find(groupName, "Group"))
            {
                try
                {
                    entry_group.Invoke("Remove", new object[] { entry_user.Path });
                    entry_group.CommitChanges();
                    var list = JoinedGroup.ToList();
                    list.RemoveAll(g => string.Equals(g, groupName, StringComparison.OrdinalIgnoreCase));
                    this.JoinedGroup = list.ToArray();
                    Logger.WriteLine("Info", $"Successfully leaved {_log_target} from group.");
                    return true;
                }
                catch (Exception e)
                {
                    Logger.WriteLine("Error", $"Failed to leave {_log_target} from group. Exception: {e.ToString()}");
                    Logger.WriteRaw(e.Message);
                }
            }
            return false;
        }
    }
}
