using LocalAccountManager.LocalAccount;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalAccountManager
{
    public enum SubOption
    {
        None,
        ListUser,
        ListGroup,
        Get,
        Set,
        Add,
        Remove,
        Rename,
        ChangePassword,
        Join,
        Leave,
    }

    internal class ArgsParam
    {
        public SubOption SubOption { get; set; }
        public string UserName { get; set; }
        public string GroupName { get; set; }
        public string NewName { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string Description { get; set; }
        public bool? UserMustChangePasswordAtNextLogon { get; set; }
        public bool? UserCannotChangePassword { get; set; }
        public bool? PasswordNeverExpires { get; set; }
        public bool? AccountIsDisabled { get; set; }
        public string ProfilePath { get; set; }
        public string LogonScript { get; set; }
        public string HomeDirectory { get; set; }
        public string HomeDrive { get; set; }

        private readonly string[] _enable_words = new string[]
            { "true", "false", "1", "yes", "on", "enable", "en" };

        public ArgsParam(string[] args)
        {
            if (args.Length > 0)
            {
                this.SubOption = args[0] switch
                {
                    "listuser" => SubOption.ListUser,
                    "listgroup" => SubOption.ListGroup,
                    "get" => SubOption.Get,
                    "set" => SubOption.Set,
                    "add" => SubOption.Add,
                    "remove" => SubOption.Remove,
                    "rename" => SubOption.Rename,
                    "changepassword" => SubOption.ChangePassword,
                    "join" => SubOption.Join,
                    "leave" => SubOption.Leave,
                    _ => SubOption.None,
                };
            }
            if (args.Length > 1)
            {
                for (int i = 1; i < args.Length; i++)
                {
                    switch (args[i].ToLower())
                    {
                        case "/u":
                        case "--user":
                        case "--username":
                            if (i + 1 < args.Length) this.UserName = args[++i];
                            break;
                        case "/g":
                        case "--group":
                        case "--groupname":
                            if (i + 1 < args.Length) this.GroupName = args[++i];
                            break;
                        case "/n":
                        case "--new":
                        case "--newname":
                            if (i + 1 < args.Length) this.NewName = args[++i];
                            break;
                        case "/p":
                        case "--pwd":
                        case "--password":
                            if (i + 1 < args.Length) this.Password = args[++i];
                            break;
                        case "/fn":
                        case "--fullname":
                            if (i + 1 < args.Length)
                            {
                                this.FullName = args[++i];
                            }
                            break;
                        case "/desc":
                        case "--description":
                            if (i + 1 < args.Length)
                            {
                                this.Description = args[++i];
                            }
                            break;
                        case "/must":
                        case "--mustchangepassword":
                            if (i + 1 < args.Length)
                            {
                                this.UserMustChangePasswordAtNextLogon =
                                    _enable_words.Any(x => x.Equals(args[++i], StringComparison.OrdinalIgnoreCase));
                            }
                            break;
                        case "/cant":
                        case "--cantchangepassword":
                            if (i + 1 < args.Length)
                            {
                                this.UserCannotChangePassword =
                                    _enable_words.Any(x => x.Equals(args[++i], StringComparison.OrdinalIgnoreCase));
                            }
                            break;
                        case "/never":
                        case "--neverexpirepassword":
                            if (i + 1 < args.Length)
                            {
                                this.PasswordNeverExpires =
                                    _enable_words.Any(x => x.Equals(args[++i], StringComparison.OrdinalIgnoreCase));
                            }
                            break;
                        case "/disable":
                        case "--disableaccount":
                            if (i + 1 < args.Length)
                            {
                                this.AccountIsDisabled =
                                    _enable_words.Any(x => x.Equals(args[++i], StringComparison.OrdinalIgnoreCase));
                            }
                            break;
                        case "/profile":
                        case "--profilepath":
                            if (i + 1 < args.Length)
                            {
                                this.ProfilePath = args[++i];
                            }
                            break;
                        case "/script":
                        case "--logonscript":
                            if (i + 1 < args.Length)
                            {
                                this.LogonScript = args[++i];
                            }
                            break;
                        case "/hdir":
                        case "--homedirectory":
                            if (i + 1 < args.Length)
                            {
                                this.HomeDirectory = args[++i];
                            }
                            break;
                        case "/hdrive":
                        case "--homedrive":
                            if (i + 1 < args.Length)
                            {
                                this.HomeDrive = args[++i];
                            }
                            break;
                    }
                }
            }
        }
    }
}
