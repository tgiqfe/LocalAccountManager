
using LocalAccountManager;
using LocalAccountManager.LocalAccount;


LocalAccountManager.LocalAccountManager.SetUser(
    "Administrator",
    null,
    null,
    false,
    null,
    true,
    null,
    null, null, null, null);


Console.ReadLine();

Environment.Exit(0);

var ap = new ArgsParam(args);
switch (ap.SubOption)
{
    case SubOption.ListUser:
        LocalAccountManager.LocalAccountManager.ListUsers();
        break;
    case SubOption.ListGroup:
        LocalAccountManager.LocalAccountManager.ListGroups();
        break;
    case SubOption.Get:
        LocalAccountManager.LocalAccountManager.GetUser(ap.UserName);
        break;
    case SubOption.Set:
        LocalAccountManager.LocalAccountManager.SetUser(ap.UserName,
            ap.FullName, ap.Description, ap.UserMustChangePasswordAtNextLogon,
            ap.UserCannotChangePassword, ap.PasswordNeverExpires, ap.AccountIsDisabled,
            ap.ProfilePath, ap.LogonScript, ap.HomeDirectory, ap.HomeDrive);
        break;
    case SubOption.Add:
        LocalAccountManager.LocalAccountManager.AddUser(ap.UserName);
        break;
    case SubOption.Remove:
        LocalAccountManager.LocalAccountManager.RemoveUser(ap.UserName);
        break;
    case SubOption.Rename:
        LocalAccountManager.LocalAccountManager.RenameUser(ap.UserName, ap.NewName);
        break;
    case SubOption.ChangePassword:
        LocalAccountManager.LocalAccountManager.ChangePassword(ap.UserName, ap.Password);
        break;
    case SubOption.Join:
        LocalAccountManager.LocalAccountManager.AddUserToGroup(ap.UserName, ap.GroupName);
        break;
    case SubOption.Leave:
        LocalAccountManager.LocalAccountManager.RemoveUserFromGroup(ap.UserName, ap.GroupName);
        break;
    default:
        Console.WriteLine("Invalid command.");
        break;
}

Console.ReadLine();
