using System;

namespace GZ.ActiveDirectoryLibrary.Exceptions
{
    public class InvalidAttributeValueException : ActiveDirectoryException
    {
        public InvalidAttributeValueException(Exception ex) : base("Invalid value for an attribute.", ErrorCode.InvalidAttributeValue, ex)
        {
        }
    }
}