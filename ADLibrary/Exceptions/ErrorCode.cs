
namespace GZ.ActiveDirectoryLibrary.Exceptions
{
    public enum ErrorCode
    {
        None = 0,
        PasswordPolicyUnmet = 1000,
        AccessDenied = 1001,
        PasswordInvalid = 1002,
        InvalidAttributeValue = 1003,
        SearchFilterError = 1004,
        InvalidPathError = 1005,
        LoginFailure = 1006,
        UnexpectedError = -1,
        UnknownError = -2
    }
}