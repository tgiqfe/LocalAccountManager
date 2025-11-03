using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Management;

namespace LocalAccountManager.LocalAccount
{
    internal class LocalGroup
    {
        #region public parameter

        public string Name { get; private set; }
        public string Description { get; private set; }
        public string[] Members { get; private set; }
        public string SID { get; private set; }

        #endregion

        #region private parameter

        private bool _isDeleted = false;
        const string _log_target = "local group";

        #endregion

        #region Constructor / Initializer

        public LocalGroup() { }

        public LocalGroup(string name)
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Group WHERE LocalAccount=True"))
            using (var directoryEntry = new DirectoryEntry($"WinNT://{Environment.MachineName},computer"))
            using (var context = new PrincipalContext(ContextType.Machine, Environment.MachineName))
            {
                using (var wmi = searcher.Get().
                    OfType<ManagementObject>().
                    FirstOrDefault(g => string.Equals(g["Name"]?.ToString(), name, StringComparison.OrdinalIgnoreCase)))
                using (var entry = directoryEntry.Children.Find(name, "Group"))
                using (var principal = GroupPrincipal.FindByIdentity(context, name))
                {
                    Initialize(wmi, entry, principal);
                }
            }
        }

        public LocalGroup(ManagementObject wmi, DirectoryEntry entry, GroupPrincipal principal)
        {
            Initialize(wmi, entry, principal);
        }

        /// <summary>
        /// Initialize object properties.
        /// </summary>
        /// <param name="wmi"></param>
        /// <param name="entry"></param>
        /// <param name="principal"></param>
        public void Initialize(ManagementObject wmi, DirectoryEntry entry, GroupPrincipal principal)
        {
            if (wmi == null || entry == null || principal == null) return;

            this.Name = wmi["Name"]?.ToString();
            this.Description = entry.Properties["Description"]?.Value?.ToString();
            this.Members = principal.GetMembers().Select(m => m.Name).ToArray();
            this.SID = principal.Sid.ToString();
        }

        /// <summary>
        /// Reflesh parameters.
        /// </summary>
        public void Reflesh()
        {
            string name = this.Name;
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Group WHERE LocalAccount=True"))
            using (var directoryEntry = new DirectoryEntry($"WinNT://{Environment.MachineName},computer"))
            using (var context = new PrincipalContext(ContextType.Machine, Environment.MachineName))
            {
                using (var wmi = searcher.Get().
                    OfType<ManagementObject>().
                    FirstOrDefault(g => string.Equals(g["Name"]?.ToString(), name, StringComparison.OrdinalIgnoreCase)))
                using (var entry = directoryEntry.Children.Find(name, "Group"))
                using (var principal = GroupPrincipal.FindByIdentity(context, name))
                {
                    Initialize(wmi, entry, principal);
                }
            }
        }

        /// <summary>
        /// Create LocalGroup array from local machine.
        /// </summary>
        /// <returns></returns>
        public static LocalGroup[] Load()
        {
            List<LocalGroup> list = new();
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Group WHERE LocalAccount=True"))
            using (var directoryEntry = new DirectoryEntry($"WinNT://{Environment.MachineName},computer"))
            using (var context = new PrincipalContext(ContextType.Machine, Environment.MachineName))
            {
                using (var groups_wmi = searcher.Get())
                {
                    foreach (ManagementObject wmi in groups_wmi)
                    {
                        string name = wmi["Name"]?.ToString();
                        using (var entry = directoryEntry.Children.Find(name, "Group"))
                        using (var principal = GroupPrincipal.FindByIdentity(context, name))
                        {
                            if (entry != null && principal != null)
                                list.Add(new LocalGroup(wmi, entry, principal));
                        }
                    }
                }
            }
            return list.ToArray();
        }

        #endregion

        #region Checking methods

        /// <summary>
        /// Group exists check.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="directoryEntry"></param>
        /// <returns></returns>
        public static bool Exists(string name, DirectoryEntry directoryEntry = null)
        {
            Logger.WriteLine("Info", $"Checking existence of {_log_target}. name: {name}");
            if (directoryEntry == null)
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Group WHERE LocalAccount=True"))
                using (var groups_wmi = searcher.Get())
                {
                    return groups_wmi.
                        OfType<ManagementObject>().
                        Any(u => string.Equals(u["Name"]?.ToString(), name, StringComparison.OrdinalIgnoreCase));
                }
            }
            else
            {
                return directoryEntry.Children.
                    OfType<DirectoryEntry>().
                    Any(e => e.SchemaClassName == "Group" && e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Group has member check. (static)
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public static bool HasMember(string userName, string groupName)
        {
            Logger.WriteLine("Info", $"Checking membership of {_log_target}. group: {groupName}, user: {userName}");
            using (var context = new PrincipalContext(ContextType.Machine, Environment.MachineName))
            using (var group = GroupPrincipal.FindByIdentity(context, groupName))
            {
                if (group == null) return false;
                return group.GetMembers().
                    OfType<Principal>().
                    Any(m => string.Equals(m.Name, userName, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Group has member check. (instance)
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public bool HasMember(string userName)
        {
            Logger.WriteLine("Info", $"Checking membership of {_log_target}. group: {this.Name}, user: {userName}");
            return this.Members.
                Any(m => string.Equals(m, userName, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        /// <summary>
        /// Get local group parameter.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static LocalGroup GetParam(string name)
        {
            Logger.WriteLine("Info", $"Getting parameter of {_log_target}. name: {name}");
            using (var directoryEntry = new DirectoryEntry($"WinNT://{Environment.MachineName},computer"))
            {
                return LocalGroup.Exists(name, directoryEntry) ? new LocalGroup(name) : null;
            }
        }

        public bool SetParam(string description)
        {
            Logger.WriteLine("Info", $"Setting parameter of {_log_target}. name: {this.Name}");
            if (_isDeleted)
            {
                Logger.WriteLine("Warning", $"Cannot set parameter of already deleted {_log_target}.");
                return false;
            }
            if (string.IsNullOrEmpty(description))
            {
                Logger.WriteLine("Warning", $"Skip set parameter for {_log_target}.");
                return false;
            }

            string name = this.Name;
            using (var directoryEntry = new DirectoryEntry($"WinNT://{Environment.MachineName},computer"))
            {
                try
                {
                    using (var entry = directoryEntry.Children.
                        OfType<DirectoryEntry>().
                        FirstOrDefault(e => e.SchemaClassName == "Group" && e.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    {
                        bool isChangeEntry = false;
                        if (description != null && description != this.Description)
                        {
                            Logger.WriteLine("Info", $"Changing Description to '{description}'.");
                            entry.Properties["Description"].Value = description;
                            this.Description = description;
                            isChangeEntry = true;
                        }
                        if (isChangeEntry) entry.CommitChanges();
                        Logger.WriteLine("Info", $"Successfully set parameter of {_log_target}.");
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Logger.WriteLine("Error", $"Failed to set parameter of {_log_target}.");
                    Logger.WriteRaw(e.ToString());
                }
            }
            return false;
        }

        /// <summary>
        /// Rename local group name.
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
            using (var entry = directoryEntry.Children.Find(this.Name, "Group"))
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
                    Logger.WriteLine("Error", $"Failed to rename {_log_target}.");
                    Logger.WriteRaw(e.ToString());
                }
            }
            return false;
        }

        /// <summary>
        /// Create new local group.
        /// </summary>
        /// <param name="name"></param>
        public static bool New(string name)
        {
            Logger.WriteLine("Info", $"Creating new {_log_target}. name: {name}");
            using (var directoryEntry = new DirectoryEntry($"WinNT://{Environment.MachineName},computer"))
            {
                try
                {
                    if (!LocalGroup.Exists(name, directoryEntry))
                    {
                        directoryEntry.Children.Add(name, "Group").CommitChanges();
                        Logger.WriteLine("Info", $"Successfully created new {_log_target}.");
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Logger.WriteLine("Error", $"Failed to create new {_log_target}.");
                    Logger.WriteRaw(e.ToString());
                }
            }
            return false;
        }

        /// <summary>
        /// Create new local group. (alias of New)
        /// </summary>
        /// <param name="name"></param>
        public static bool Add(string name)
        {
            return LocalGroup.New(name);
        }

        /// <summary>
        /// Remove local group.
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
                    var entry = directoryEntry.Children.Find(this.Name, "Group");
                    directoryEntry.Children.Remove(entry);
                    _isDeleted = true;
                    Logger.WriteLine("Info", $"Successfully deleted {_log_target}.");
                    return true;
                }
                catch (Exception e)
                {
                    Logger.WriteLine("Error", $"Failed to delete {_log_target}.");
                    Logger.WriteRaw(e.ToString());
                }
            }
            return false;
        }

        /// <summary>
        /// Remove local group. (alias of Remove)
        /// </summary>
        public bool Delete()
        {
            return this.Remove();
        }
    }
}