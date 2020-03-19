using System;

namespace GZ.ActiveDirectoryLibrary.Exceptions
{
    public class PasswordPolicyException : ActiveDirectoryException
    {
        public PasswordPolicyException(Exception ex) : base("The password does not meet the password policy requirements. Check the minimum password length, password complexity and password history requirements.", ErrorCode.PasswordPolicyUnmet, ex)
        {
        }
    }
}