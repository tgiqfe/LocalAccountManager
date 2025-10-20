using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Management;

namespace Test424.LocalAccount
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
                    SetParameter(wmi, entry, principal);
                }
            }
        }

        public LocalGroup(ManagementObject wmi, DirectoryEntry entry, GroupPrincipal principal)
        {
            SetParameter(wmi, entry, principal);
        }

        /// <summary>
        /// Initialize object properties.
        /// </summary>
        /// <param name="wmi"></param>
        /// <param name="entry"></param>
        /// <param name="principal"></param>
        public void SetParameter(ManagementObject wmi, DirectoryEntry entry, GroupPrincipal principal)
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
                    SetParameter(wmi, entry, principal);
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
            return this.Members.
                Any(m => string.Equals(m, userName, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        public static LocalGroup GetGroup(string name)
        {
            using (var directoryEntry = new DirectoryEntry($"WinNT://{Environment.MachineName},computer"))
            {
                return LocalGroup.Exists(name, directoryEntry) ? new LocalGroup(name) : null;
            }
        }

        public void SetGroup(ModifyParam param)
        {
            if (_isDeleted) return;

            string name = this.Name;
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Group WHERE LocalAccount=True"))
            using (var directoryEntry = new DirectoryEntry($"WinNT://{Environment.MachineName},computer"))
            using (var context = new PrincipalContext(ContextType.Machine, Environment.MachineName))
            {
                using (var wmi = searcher.Get().
                    OfType<ManagementObject>().
                    FirstOrDefault(g => string.Equals(g["Name"]?.ToString(), name, StringComparison.OrdinalIgnoreCase)))
                using (var entry = directoryEntry.Children.
                    OfType<DirectoryEntry>().
                    FirstOrDefault(e => e.SchemaClassName == "Group" && e.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                using (var principal = GroupPrincipal.FindByIdentity(context, name))
                {
                    bool isChangeEntry = false;
                    bool isChangePrincipal = false;
                    if (param.Description != null && param.Description != this.Description)
                    {
                        entry.Properties["Description"].Value = param.Description;
                        this.Description = param.Description;
                        isChangeEntry = true;
                    }
                    if (isChangeEntry) entry.CommitChanges();
                    if (isChangePrincipal) principal.Save();
                }
            }
        }

        public void RenameGroup(string newName)
        {
            if (_isDeleted) return;
            using (var directoryEntry = new DirectoryEntry($"WinNT://{Environment.MachineName},computer"))
            using (var entry = directoryEntry.Children.Find(this.Name, "Group"))
            {
                if (entry != null)
                {
                    entry.Rename(newName);
                    entry.CommitChanges();
                    this.Name = newName;
                }
            }
        }

        public static void AddGroup(string name)
        {
            using (var directoryEntry = new DirectoryEntry($"WinNT://{Environment.MachineName},computer"))
            {
                if (!LocalGroup.Exists(name, directoryEntry))
                {
                    directoryEntry.Children.Add(name, "Group").CommitChanges();
                }
            }
        }

        public void RemoveGroup()
        {
            if (_isDeleted) return;
            using (var directoryEntry = new DirectoryEntry($"WinNT://{Environment.MachineName},computer"))
            {
                var entry = directoryEntry.Children.Find(this.Name, "Group");
                directoryEntry.Children.Remove(entry);
                _isDeleted = true;
            }
        }
    }
}