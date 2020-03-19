using System;

namespace GZ.ActiveDirectoryLibrary.Exceptions
{
    public class InvalidPathException : ActiveDirectoryException
    {
        public InvalidPathException(Exception ex) : base("AD Path is invalid.", ErrorCode.InvalidPathError, ex)
        {
        }
    }
}