using System;

namespace GZ.ActiveDirectoryLibrary.Exceptions
{
    public class UnknownErrorException : ActiveDirectoryException
    {
        public UnknownErrorException(Exception ex) : base("Unknown exception: " + ex.Message, ErrorCode.UnknownError, ex)
        {
        }
    }
}