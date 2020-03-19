using System;

namespace GZ.ActiveDirectoryLibrary.Exceptions
{
    /// <summary>
    /// AccessDeniedException
    /// </summary>
    public class AccessDeniedException : ActiveDirectoryException
    {
        /// <summary>
        /// AccessDeniedException constructor
        /// </summary>
        /// <param name="ex"></param>
        public AccessDeniedException(Exception ex) : base("Access Denied.", ErrorCode.AccessDenied, ex)
        {
        }
    }
}