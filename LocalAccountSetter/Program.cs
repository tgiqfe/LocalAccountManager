
using LocalAccountSetter;

var ap = new ArgsParam(args);
switch (ap.SubOption)
{
    case SubOption.ListUser:
        LocalAccountManager.ListUsers();
        break;
    case SubOption.ListGroup:
        LocalAccountManager.ListGroups();
        break;
    case SubOption.Get:
        LocalAccountManager.GetUser(ap.UserName);
        break;
    case SubOption.Set:
        LocalAccountManager.SetUser(ap.UserName, ap.ModifyParam);
        break;
    case SubOption.Add:
        LocalAccountManager.AddUser(ap.UserName);
        break;
    case SubOption.Remove:
        LocalAccountManager.RemoveUser(ap.UserName);
        break;
    case SubOption.Rename:
        LocalAccountManager.RenameUser(ap.UserName, ap.NewName);
        break;
    case SubOption.ChangePassword:
        LocalAccountManager.ChangePassword(ap.UserName, ap.Password);
        break;
    case SubOption.Join:
        LocalAccountManager.AddUserToGroup(ap.UserName, ap.GroupName);
        break;
    case SubOption.Leave:
        LocalAccountManager.RemoveUserFromGroup(ap.UserName, ap.GroupName);
        break;
    default:
        Console.WriteLine("Invalid command.");
        break;
}

Console.ReadLine();
