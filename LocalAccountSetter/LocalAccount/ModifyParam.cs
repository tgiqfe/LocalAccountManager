namespace Test424.LocalAccount
{
    internal class ModifyParam
    {
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
    }
}
