using System;

namespace GZ.ActiveDirectoryLibrary.Exceptions
{
    public class PasswordInvalidException : ActiveDirectoryException
    {
        public PasswordInvalidException(Exception ex) : base("The specified network password is not correct.", ErrorCode.PasswordInvalid, ex)
        {
        }
    }
}