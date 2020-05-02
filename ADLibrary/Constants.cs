namespace GZ.ActiveDirectoryLibrary
{
    internal class Constants
    {
        internal const char MULTI_VALUED_SEPARATOR = '|';
        internal const int ACCOUNT_OPTION_PWD_NEVER_EXPIRES = 65536; // &H10000
        internal const int ACCOUNT_OPTION_ACCOUNT_DISABLED = 2; // &H2
        internal const int ACCOUNT_OPTION_ACCOUNT_LOCKOUT = 16;

        // Encrypted text pwd allowed
        internal const int ACCOUNT_OPTION_PWD_REVERSIBLE_ENCRYPTION = 128; // &H0080
        internal const string CHANGE_PASSWORD_GUID = "{AB721A53-1E2F-11D0-9819-00AA0040529B}";
        internal const int ADS_ACETYPE_ACCESS_DENIED_OBJECT = 0x6;
        internal const int ADS_ACETYPE_ACCESS_ALLOWED_OBJECT = 0x5;
        internal const string LOGON_ESCAPE_CHRS = "#";
        internal const string DOMAIN_USER_GROUP = "Domain Users";
        internal const string LOGON_FAILURE_MSG = "Logon failure";
    }
}