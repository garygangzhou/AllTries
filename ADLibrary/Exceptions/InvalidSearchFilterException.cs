using System;

namespace GZ.ActiveDirectoryLibrary.Exceptions
{
    public class InvalidSearchFilterException : ActiveDirectoryException
    {
        public InvalidSearchFilterException(Exception ex) : base("Search filter is invalid.", ErrorCode.SearchFilterError, ex)
        {
        }
    }
}