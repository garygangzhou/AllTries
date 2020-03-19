using System;

namespace GZ.ActiveDirectoryLibrary.Exceptions
{
    public class LoginFailureException : ActiveDirectoryException
    {
        public LoginFailureException(Exception ex) : base("Invalid username and/or password.", ErrorCode.LoginFailure, ex)
        {
        }
    }
}