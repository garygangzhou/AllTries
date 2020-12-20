
namespace GZ.ActiveDirectoryLibrary.User
{    
    public struct SearchAccountCriteria
    {
        public string FirstName;
        public string LastName;
        public string LogonName;
        public string GUID;
        public string OU;
        public string Site;
        public string Role;
        public bool DisabledAccount;
        public bool PasswordExpired;
        public bool AccountExistsInAD;
    }
}